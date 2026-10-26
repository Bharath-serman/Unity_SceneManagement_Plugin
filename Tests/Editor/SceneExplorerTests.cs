using System.Collections.Generic;
using NUnit.Framework;
using SceneSentry.Editor;
using UnityEngine;

namespace SceneSentry.Tests
{
    [TestFixture]
    public class SceneExplorerTests
    {
        private GameObject _go1;
        private GameObject _go2;
        private TagDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = ScriptableObject.CreateInstance<TagDatabase>();
            _go1 = new GameObject("PlayerCharacter");
            _go2 = new GameObject("EnemyGoblin");
        }

        [TearDown]
        public void TearDown()
        {
            if (_go1 != null) UnityEngine.Object.DestroyImmediate(_go1);
            if (_go2 != null) UnityEngine.Object.DestroyImmediate(_go2);
            if (_database != null) UnityEngine.Object.DestroyImmediate(_database);
        }

        [Test]
        public void Filter_ByName_MatchesTargetObject()
        {
            var filter = new SceneExplorerFilter
            {
                SearchText = "Goblin"
            };

            Assert.That(filter.Matches(_go1, _database), Is.False);
            Assert.That(filter.Matches(_go2, _database), Is.True);
        }

        [Test]
        public void Filter_ByCustomTag_MatchesTaggedObject()
        {
            var tag = _database.CreateTag("PlayerTag");
            var tagsComp = _go1.AddComponent<SceneSentryTags>();
            tagsComp.AddTag(tag.Id);

            var filter = new SceneExplorerFilter();
            filter.CustomTagIds.Add(tag.Id);

            Assert.That(filter.Matches(_go1, _database), Is.True);
            Assert.That(filter.Matches(_go2, _database), Is.False);
        }

        [Test]
        public void Filter_ByComponentType_MatchesContainingObject()
        {
            _go1.AddComponent<Camera>();

            var filter = new SceneExplorerFilter
            {
                ComponentTypeName = "Camera"
            };

            Assert.That(filter.Matches(_go1, _database), Is.True);
            Assert.That(filter.Matches(_go2, _database), Is.False);
        }

        [Test]
        public void Filter_AndLogic_RequiresBothConditions()
        {
            var tag = _database.CreateTag("Character");
            var tags1 = _go1.AddComponent<SceneSentryTags>();
            tags1.AddTag(tag.Id);

            var tags2 = _go2.AddComponent<SceneSentryTags>();
            tags2.AddTag(tag.Id);

            var filter = new SceneExplorerFilter
            {
                LogicMode = MatchMode.MatchAll_AND,
                SearchText = "Player"
            };
            filter.CustomTagIds.Add(tag.Id);

            // _go1 has tag AND "Player" name -> matches
            // _go2 has tag but NOT "Player" name -> fails
            Assert.That(filter.Matches(_go1, _database), Is.True);
            Assert.That(filter.Matches(_go2, _database), Is.False);
        }

        [Test]
        public void Filter_OrLogic_MatchesEitherCondition()
        {
            var tag = _database.CreateTag("SpecialTag");
            var tagsComp = _go1.AddComponent<SceneSentryTags>();
            tagsComp.AddTag(tag.Id);

            var filter = new SceneExplorerFilter
            {
                LogicMode = MatchMode.MatchAny_OR,
                SearchText = "Goblin"
            };
            filter.CustomTagIds.Add(tag.Id);

            // _go1 matches tag; _go2 matches "Goblin" name -> both should match
            Assert.That(filter.Matches(_go1, _database), Is.True);
            Assert.That(filter.Matches(_go2, _database), Is.True);
        }

        [Test]
        public void Filter_ActiveState_RespectsActiveAndInactive()
        {
            _go2.SetActive(false);

            var activeFilter = new SceneExplorerFilter { ActiveFilter = ActiveStateFilter.ActiveOnly };
            var inactiveFilter = new SceneExplorerFilter { ActiveFilter = ActiveStateFilter.InactiveOnly };

            Assert.That(activeFilter.Matches(_go1, _database), Is.True);
            Assert.That(activeFilter.Matches(_go2, _database), Is.False);

            Assert.That(inactiveFilter.Matches(_go1, _database), Is.False);
            Assert.That(inactiveFilter.Matches(_go2, _database), Is.True);
        }
    }
}
