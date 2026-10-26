using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SceneSentry.Editor;
using UnityEngine;

namespace SceneSentry.Tests
{
    [TestFixture]
    public class SceneAnalyzerTests
    {
        private GameObject _testGo;
        private TagDatabase _database;
        private SceneSentryRuleConfig _config;

        [SetUp]
        public void SetUp()
        {
            _testGo = new GameObject("AnalyzerTestTarget");
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
        public void DuplicateTagsRule_DetectsDuplicateTagAssignment()
        {
            var tag = _database.CreateTag("DuplicateTarget");
            var tagsComp = _testGo.AddComponent<SceneSentryTags>();

            // Force duplicate in list for testing
            tagsComp.AddTag(tag.Id);
            typeof(SceneSentryTags)
                .GetField("_tagIds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(tagsComp, new List<string> { tag.Id, tag.Id });

            var rule = new DuplicateTagsRule();
            var context = new SceneValidationContext(new[] { _testGo }, _database, _config);

            var findings = rule.Analyze(context).ToList();

            Assert.That(findings.Count, Is.EqualTo(1));
            Assert.That(findings[0].Severity, Is.EqualTo(ValidationSeverity.Warning));
            Assert.That(findings[0].IsFixable, Is.True);
        }

        [Test]
        public void InvalidTagReferencesRule_DetectsNonExistentTagId()
        {
            var tagsComp = _testGo.AddComponent<SceneSentryTags>();
            tagsComp.AddTag("ghost-tag-id-9999");

            var rule = new InvalidTagReferencesRule();
            var context = new SceneValidationContext(new[] { _testGo }, _database, _config);

            var findings = rule.Analyze(context).ToList();

            Assert.That(findings.Count, Is.EqualTo(1));
            Assert.That(findings[0].Severity, Is.EqualTo(ValidationSeverity.Warning));
            Assert.That(findings[0].IsFixable, Is.True);
        }

        [Test]
        public void TagRequiresComponentRule_FlagsMissingComponent()
        {
            var tag = _database.CreateTag("Interactive");
            var tagsComp = _testGo.AddComponent<SceneSentryTags>();
            tagsComp.AddTag(tag.Id);

            _config.AddTagComponentRequirement(tag.Id, "Interactive", "Camera");

            var rule = new TagRequiresComponentRule();
            var context = new SceneValidationContext(new[] { _testGo }, _database, _config);

            var findings = rule.Analyze(context).ToList();

            Assert.That(findings.Count, Is.EqualTo(1));
            Assert.That(findings[0].Severity, Is.EqualTo(ValidationSeverity.Error));
            Assert.That(findings[0].IsFixable, Is.True);
        }

        [Test]
        public void DisabledRule_GeneratesZeroFindingsInEngine()
        {
            var tag = _database.CreateTag("DuplicateTarget");
            var tagsComp = _testGo.AddComponent<SceneSentryTags>();
            tagsComp.AddTag(tag.Id);
            typeof(SceneSentryTags)
                .GetField("_tagIds", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(tagsComp, new List<string> { tag.Id, tag.Id });

            var dupRule = new DuplicateTagsRule();
            _config.SetRuleEnabled(dupRule.RuleId, false);

            SceneAnalyzerEngine.SetRulesForTesting(new List<ISceneValidationRule> { dupRule });
            var findings = SceneAnalyzerEngine.RunAnalysis(_database, _config, showProgress: false);

            Assert.That(findings.Count, Is.EqualTo(0));
        }

        [Test]
        public void EmptyScene_AnalyzesWithoutErrors()
        {
            var rule = new DuplicateTagsRule();
            var context = new SceneValidationContext(new GameObject[0], _database, _config);

            var findings = rule.Analyze(context).ToList();

            Assert.That(findings.Count, Is.EqualTo(0));
        }
    }
}
