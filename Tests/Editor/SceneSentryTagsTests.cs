using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace SceneSentry.Tests
{
    [TestFixture]
    public class SceneSentryTagsTests
    {
        private GameObject _testGo;
        private SceneSentryTags _tagsComp;
        private TagDatabase _database;

        [SetUp]
        public void SetUp()
        {
            SceneSentryRegistry.Clear();
            _testGo = new GameObject("TestTarget");
            _tagsComp = _testGo.AddComponent<SceneSentryTags>();
            _database = ScriptableObject.CreateInstance<TagDatabase>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testGo != null)
            {
                UnityEngine.Object.DestroyImmediate(_testGo);
            }
            if (_database != null)
            {
                UnityEngine.Object.DestroyImmediate(_database);
            }
            SceneSentryRegistry.Clear();
        }

        [Test]
        public void AddTag_SingleAndMultiple_StoresTagsCorrectly()
        {
            var tag1 = _database.CreateTag("TagA");
            var tag2 = _database.CreateTag("TagB");

            bool added1 = _tagsComp.AddTag(tag1.Id);
            bool added2 = _tagsComp.AddTag(tag2.Id);

            Assert.That(added1, Is.True);
            Assert.That(added2, Is.True);
            Assert.That(_tagsComp.TagCount, Is.EqualTo(2));
            Assert.That(_tagsComp.HasTagId(tag1.Id), Is.True);
            Assert.That(_tagsComp.HasTagId(tag2.Id), Is.True);
        }

        [Test]
        public void AddTag_DuplicateTag_ReturnsFalseAndAvoidsDuplicates()
        {
            var tag = _database.CreateTag("UniqueTag");

            bool firstAdd = _tagsComp.AddTag(tag.Id);
            bool secondAdd = _tagsComp.AddTag(tag.Id);

            Assert.That(firstAdd, Is.True);
            Assert.That(secondAdd, Is.False);
            Assert.That(_tagsComp.TagCount, Is.EqualTo(1));
        }

        [Test]
        public void RemoveTag_ExistingTag_RemovesSuccessfully()
        {
            var tag = _database.CreateTag("RemoveMe");
            _tagsComp.AddTag(tag.Id);

            bool removed = _tagsComp.RemoveTag(tag.Id);

            Assert.That(removed, Is.True);
            Assert.That(_tagsComp.TagCount, Is.EqualTo(0));
            Assert.That(_tagsComp.HasTagId(tag.Id), Is.False);
        }

        [Test]
        public void HasAllTagIds_MatchesOnlyWhenAllPresent()
        {
            var tagA = _database.CreateTag("Alpha");
            var tagB = _database.CreateTag("Beta");
            var tagC = _database.CreateTag("Gamma");

            _tagsComp.AddTag(tagA.Id);
            _tagsComp.AddTag(tagB.Id);

            Assert.That(_tagsComp.HasAllTagIds(new[] { tagA.Id, tagB.Id }), Is.True);
            Assert.That(_tagsComp.HasAllTagIds(new[] { tagA.Id, tagC.Id }), Is.False);
        }

        [Test]
        public void HasAnyTagId_MatchesWhenAtLeastOnePresent()
        {
            var tagA = _database.CreateTag("Alpha");
            var tagB = _database.CreateTag("Beta");

            _tagsComp.AddTag(tagA.Id);

            Assert.That(_tagsComp.HasAnyTagId(new[] { tagA.Id, tagB.Id }), Is.True);
            Assert.That(_tagsComp.HasAnyTagId(new[] { "NonExistentId" }), Is.False);
        }

        [Test]
        public void Registry_Lookups_ReturnMatchingObjects()
        {
            var tag = _database.CreateTag("RegistryTest");
            _tagsComp.AddTag(tag.Id);

            var found = SceneSentryRegistry.FindObjectsWithTag(tag.Id);

            Assert.That(found.Count, Is.EqualTo(1));
            Assert.That(found, Contains.Item(_tagsComp));
        }

        [Test]
        public void CleanInvalidTags_PurgesOrphanedIds()
        {
            var validTag = _database.CreateTag("Valid");
            _tagsComp.AddTag(validTag.Id);
            _tagsComp.AddTag("orphaned-id-12345");

            Assert.That(_tagsComp.TagCount, Is.EqualTo(2));

            int cleaned = _tagsComp.CleanInvalidTags(_database);

            Assert.That(cleaned, Is.EqualTo(1));
            Assert.That(_tagsComp.TagCount, Is.EqualTo(1));
            Assert.That(_tagsComp.HasTagId(validTag.Id), Is.True);
        }
    }
}
