using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SceneSentry.Editor;
using UnityEngine;

namespace SceneSentry.Tests
{
    [TestFixture]
    public class SafeFixTests
    {
        private GameObject _testGo;
        private TagDatabase _database;
        private SceneSentryRuleConfig _config;

        [SetUp]
        public void SetUp()
        {
            _testGo = new GameObject("SafeFixTestTarget");
            _database = ScriptableObject.CreateInstance<TagDatabase>();
            _config = ScriptableObject.CreateInstance<SceneSentryRuleConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGo != null) UnityEngine.Object.DestroyImmediate(_testGo);
            if (_database != null) UnityEngine.Object.DestroyImmediate(_database);
            if (_config != null) UnityEngine.Object.DestroyImmediate(_config);
        }

        [Test]
        public void SafeFix_DuplicateTags_RemovesDuplicatesSuccessfully()
        {
            var tag = _database.CreateTag("Item");
            var tagsComp = _testGo.AddComponent<SceneSentryTags>();

            // Artificially inject duplicates
            typeof(SceneSentryTags)
                .GetField("_tagIds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(tagsComp, new List<string> { tag.Id, tag.Id });

            var rule = new DuplicateTagsRule();
            var context = new SceneValidationContext(new[] { _testGo }, _database, _config);

            var finding = rule.Analyze(context).FirstOrDefault();
            Assert.That(finding, Is.Not.Null);
            Assert.That(finding.IsFixable, Is.True);

            var result = SafeFixManager.ApplySingleFix(finding);

            Assert.That(result.Success, Is.True);
            Assert.That(tagsComp.TagCount, Is.EqualTo(1));
            Assert.That(tagsComp.HasTagId(tag.Id), Is.True);
        }

        [Test]
        public void SafeFix_InvalidTags_PurgesOrphanedIdsSuccessfully()
        {
            var validTag = _database.CreateTag("ValidTag");
            var tagsComp = _testGo.AddComponent<SceneSentryTags>();
            tagsComp.AddTag(validTag.Id);
            tagsComp.AddTag("ghost-id-1234");

            var rule = new InvalidTagReferencesRule();
            var context = new SceneValidationContext(new[] { _testGo }, _database, _config);

            var finding = rule.Analyze(context).FirstOrDefault();
            Assert.That(finding, Is.Not.Null);

            var result = SafeFixManager.ApplySingleFix(finding);

            Assert.That(result.Success, Is.True);
            Assert.That(tagsComp.TagCount, Is.EqualTo(1));
            Assert.That(tagsComp.HasTagId(validTag.Id), Is.True);
            Assert.That(tagsComp.HasTagId("ghost-id-1234"), Is.False);
        }

        [Test]
        public void SafeFix_NonFixableFinding_ReturnsFailure()
        {
            var finding = new ValidationFinding(
                ruleId: "manual-only",
                ruleName: "Manual Rule",
                title: "Manual Issue",
                explanation: "Must be resolved manually.",
                targetGameObject: _testGo,
                targetComponent: null,
                reason: "Cannot be automated safely.",
                suggestedCorrection: "Manual review required.",
                severity: ValidationSeverity.Warning);

            var result = SafeFixManager.ApplySingleFix(finding);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Contains.Substring("does not support an automated safe fix"));
        }
    }
}
