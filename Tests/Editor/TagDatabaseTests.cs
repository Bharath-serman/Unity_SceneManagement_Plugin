using System;
using NUnit.Framework;
using UnityEngine;

namespace SceneSentry.Tests
{
    [TestFixture]
    public class TagDatabaseTests
    {
        private TagDatabase _database;

        [SetUp]
        public void SetUp()
        {
            _database = ScriptableObject.CreateInstance<TagDatabase>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_database != null)
            {
                UnityEngine.Object.DestroyImmediate(_database);
            }
        }

        [Test]
        public void CreateTag_ValidName_CreatesTagWithStableId()
        {
            var tag = _database.CreateTag("Environment", "World geometry and props", Color.green);

            Assert.That(tag, Is.Not.Null);
            Assert.That(tag.Name, Is.EqualTo("Environment"));
            Assert.That(tag.Description, Is.EqualTo("World geometry and props"));
            Assert.That(tag.Id, Is.Not.Null.And.Not.Empty);
            Assert.That(_database.ContainsId(tag.Id), Is.True);
            Assert.That(_database.ContainsName("Environment"), Is.True);
        }

        [Test]
        public void CreateTag_DuplicateName_ThrowsInvalidOperationException()
        {
            _database.CreateTag("Interactive");

            Assert.Throws<InvalidOperationException>(() =>
            {
                _database.CreateTag("Interactive");
            });
        }

        [Test]
        public void RenameTag_ValidNewName_PreservesOriginalStableId()
        {
            var tag = _database.CreateTag("OldName");
            string originalId = tag.Id;

            bool renamed = _database.RenameTag(originalId, "NewName");

            Assert.That(renamed, Is.True);
            Assert.That(tag.Name, Is.EqualTo("NewName"));
            Assert.That(tag.Id, Is.EqualTo(originalId));
            Assert.That(_database.GetTagById(originalId).Name, Is.EqualTo("NewName"));
            Assert.That(_database.ContainsName("OldName"), Is.False);
            Assert.That(_database.ContainsName("NewName"), Is.True);
        }

        [Test]
        public void Hierarchy_ParentChild_CorrectlyIdentifiesChildrenAndRoots()
        {
            var env = _database.CreateTag("Environment");
            var foliage = _database.CreateTag("Foliage", parentId: env.Id);
            var tree = _database.CreateTag("Tree", parentId: foliage.Id);

            var roots = _database.GetRootTags();
            var envChildren = _database.GetChildren(env.Id);
            var foliageChildren = _database.GetChildren(foliage.Id);

            Assert.That(roots.Count, Is.EqualTo(1));
            Assert.That(roots[0].Id, Is.EqualTo(env.Id));
            Assert.That(envChildren.Count, Is.EqualTo(1));
            Assert.That(envChildren[0].Id, Is.EqualTo(foliage.Id));
            Assert.That(foliageChildren.Count, Is.EqualTo(1));
            Assert.That(foliageChildren[0].Id, Is.EqualTo(tree.Id));

            string fullPath = _database.GetHierarchyPath(tree.Id);
            Assert.That(fullPath, Is.EqualTo("Environment / Foliage / Tree"));
        }

        [Test]
        public void CircularHierarchy_PreventedWhenParentingUnderDescendant()
        {
            var parent = _database.CreateTag("Parent");
            var child = _database.CreateTag("Child", parentId: parent.Id);

            Assert.That(_database.IsDescendantOf(child.Id, parent.Id), Is.True);
            Assert.That(_database.IsDescendantOf(parent.Id, child.Id), Is.False);

            // Attempting to make Parent a child of its own Child must fail
            bool updated = _database.UpdateTag(parent.Id, parent.Name, parent.Description, parent.Color, child.Id);

            Assert.That(updated, Is.False);
            Assert.That(parent.ParentId, Is.Null);
        }

        [Test]
        public void DeleteTag_SafelyReparentsDirectChildren()
        {
            var root = _database.CreateTag("Root");
            var mid = _database.CreateTag("Mid", parentId: root.Id);
            var leaf = _database.CreateTag("Leaf", parentId: mid.Id);

            bool deleted = _database.DeleteTag(mid.Id);

            Assert.That(deleted, Is.True);
            Assert.That(_database.ContainsId(mid.Id), Is.False);

            // Leaf should now be reparented to Root
            var updatedLeaf = _database.GetTagById(leaf.Id);
            Assert.That(updatedLeaf.ParentId, Is.EqualTo(root.Id));
        }

        [Test]
        public void ValidateDatabase_FixesCorruptedParentPointers()
        {
            var tag1 = _database.CreateTag("Tag1");
            tag1.ParentId = "non-existent-guid-id";

            _database.ValidateDatabase();

            Assert.That(tag1.ParentId, Is.Null);
        }
    }
}
