using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneSentry
{
    /// <summary>
    /// Persistent ScriptableObject database containing all custom tag definitions for the project.
    /// Manages tag creation, hierarchy, color schemes, and fast ID/Name indexing.
    /// </summary>
    [CreateAssetMenu(fileName = "SceneSentryTagDatabase", menuName = "SceneSentry/Tag Database", order = 100)]
    public class TagDatabase : ScriptableObject, ISerializationCallbackReceiver
    {
        [SerializeField] private List<TagDefinition> _tags = new List<TagDefinition>();

        [NonSerialized] private Dictionary<string, TagDefinition> _byId = new Dictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase);
        [NonSerialized] private Dictionary<string, TagDefinition> _byName = new Dictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyList<TagDefinition> Tags => _tags;
        public int Count => _tags.Count;

        public event Action OnDatabaseModified;

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            RebuildIndices();
        }

        private void OnEnable()
        {
            RebuildIndices();
        }

        public void RebuildIndices()
        {
            _byId.Clear();
            _byName.Clear();

            for (int i = 0; i < _tags.Count; i++)
            {
                var tag = _tags[i];
                if (tag == null) continue;

                if (!string.IsNullOrEmpty(tag.Id))
                {
                    _byId[tag.Id] = tag;
                }

                if (!string.IsNullOrEmpty(tag.Name))
                {
                    _byName[tag.Name] = tag;
                }
            }
        }

        public TagDefinition GetTagById(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;
            if (_byId.TryGetValue(id.Trim(), out var tag)) return tag;
            return null;
        }

        public TagDefinition GetTagByName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;
            if (_byName.TryGetValue(name.Trim(), out var tag)) return tag;
            return null;
        }

        public bool ContainsId(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            return _byId.ContainsKey(id.Trim());
        }

        public bool ContainsName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            return _byName.ContainsKey(name.Trim());
        }

        public TagDefinition CreateTag(string name, string description = "", Color? color = null, string parentId = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Tag name cannot be null or empty.", nameof(name));
            }

            string trimmedName = name.Trim();
            if (ContainsName(trimmedName))
            {
                throw new InvalidOperationException($"A tag named '{trimmedName}' already exists.");
            }

            if (!string.IsNullOrEmpty(parentId) && !ContainsId(parentId))
            {
                throw new ArgumentException($"Parent tag with ID '{parentId}' does not exist.", nameof(parentId));
            }

            var tag = TagDefinition.Create(trimmedName, description, color, parentId);
            _tags.Add(tag);
            RebuildIndices();
            NotifyModified();
            return tag;
        }

        public bool RenameTag(string id, string newName)
        {
            var tag = GetTagById(id);
            if (tag == null) return false;

            if (string.IsNullOrWhiteSpace(newName)) return false;
            string trimmed = newName.Trim();

            if (string.Equals(tag.Name, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                tag.Name = trimmed;
                NotifyModified();
                return true;
            }

            if (ContainsName(trimmed)) return false;

            tag.Name = trimmed;
            RebuildIndices();
            NotifyModified();
            return true;
        }

        public bool UpdateTag(string id, string newName, string newDescription, Color newColor, string newParentId)
        {
            var tag = GetTagById(id);
            if (tag == null) return false;

            if (string.IsNullOrWhiteSpace(newName)) return false;
            string trimmed = newName.Trim();

            if (!string.Equals(tag.Name, trimmed, StringComparison.OrdinalIgnoreCase) && ContainsName(trimmed))
            {
                return false;
            }

            string cleanParent = string.IsNullOrWhiteSpace(newParentId) ? null : newParentId.Trim();
            if (!string.IsNullOrEmpty(cleanParent))
            {
                if (string.Equals(cleanParent, id, StringComparison.OrdinalIgnoreCase)) return false;
                if (!ContainsId(cleanParent)) return false;
                if (IsDescendantOf(cleanParent, id)) return false; // Circular check
            }

            tag.Name = trimmed;
            tag.Description = newDescription ?? string.Empty;
            tag.Color = newColor;
            tag.ParentId = cleanParent;

            RebuildIndices();
            NotifyModified();
            return true;
        }

        public bool DeleteTag(string id)
        {
            var tag = GetTagById(id);
            if (tag == null) return false;

            // Reparent any direct children to the deleted tag's parent to avoid broken references
            string orphanedParent = tag.ParentId;
            for (int i = 0; i < _tags.Count; i++)
            {
                if (string.Equals(_tags[i].ParentId, id, StringComparison.OrdinalIgnoreCase))
                {
                    _tags[i].ParentId = orphanedParent;
                }
            }

            _tags.Remove(tag);
            RebuildIndices();
            NotifyModified();
            return true;
        }

        public bool IsDescendantOf(string candidateId, string ancestorId)
        {
            if (string.IsNullOrEmpty(candidateId) || string.IsNullOrEmpty(ancestorId)) return false;
            if (string.Equals(candidateId, ancestorId, StringComparison.OrdinalIgnoreCase)) return true;

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string currentId = candidateId;

            while (!string.IsNullOrEmpty(currentId))
            {
                if (visited.Contains(currentId)) break; // Cycle prevention
                visited.Add(currentId);

                var current = GetTagById(currentId);
                if (current == null) break;

                if (string.Equals(current.ParentId, ancestorId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                currentId = current.ParentId;
            }

            return false;
        }

        public List<TagDefinition> GetChildren(string parentId)
        {
            var list = new List<TagDefinition>();
            string cleanParent = string.IsNullOrWhiteSpace(parentId) ? null : parentId.Trim();

            for (int i = 0; i < _tags.Count; i++)
            {
                var tag = _tags[i];
                if (tag == null) continue;

                if (string.IsNullOrEmpty(cleanParent))
                {
                    if (string.IsNullOrEmpty(tag.ParentId)) list.Add(tag);
                }
                else
                {
                    if (string.Equals(tag.ParentId, cleanParent, StringComparison.OrdinalIgnoreCase))
                    {
                        list.Add(tag);
                    }
                }
            }

            return list;
        }

        public List<TagDefinition> GetRootTags()
        {
            return GetChildren(null);
        }

        public string GetHierarchyPath(string tagId, string separator = " / ")
        {
            var tag = GetTagById(tagId);
            if (tag == null) return string.Empty;

            var stack = new List<string>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var current = tag;

            while (current != null)
            {
                if (visited.Contains(current.Id)) break;
                visited.Add(current.Id);

                stack.Add(current.Name);
                current = string.IsNullOrEmpty(current.ParentId) ? null : GetTagById(current.ParentId);
            }

            stack.Reverse();
            return string.Join(separator, stack);
        }

        public void ValidateDatabase()
        {
            var cleanList = new List<TagDefinition>();
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < _tags.Count; i++)
            {
                var tag = _tags[i];
                if (tag == null) continue;

                if (string.IsNullOrEmpty(tag.Id) || ids.Contains(tag.Id))
                {
                    // Generate new ID if missing or duplicate
                    tag = new TagDefinition(Guid.NewGuid().ToString("N"), tag.Name, tag.Description, tag.Color, tag.ParentId);
                }

                if (string.IsNullOrWhiteSpace(tag.Name))
                {
                    tag.Name = $"Tag_{tag.Id.Substring(0, 6)}";
                }

                // Ensure unique name
                string baseName = tag.Name;
                int suffix = 1;
                while (names.Contains(tag.Name))
                {
                    tag.Name = $"{baseName}_{suffix++}";
                }

                names.Add(tag.Name);
                ids.Add(tag.Id);
                cleanList.Add(tag);
            }

            _tags = cleanList;

            // Fix broken parent links and circular relationships
            for (int i = 0; i < _tags.Count; i++)
            {
                var tag = _tags[i];
                if (!string.IsNullOrEmpty(tag.ParentId))
                {
                    if (!ids.Contains(tag.ParentId) || string.Equals(tag.ParentId, tag.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        tag.ParentId = null;
                    }
                    else if (IsDescendantOf(tag.ParentId, tag.Id))
                    {
                        tag.ParentId = null;
                    }
                }
            }

            RebuildIndices();
            NotifyModified();
        }

        public void Clear()
        {
            _tags.Clear();
            RebuildIndices();
            NotifyModified();
        }

        private void NotifyModified()
        {
            OnDatabaseModified?.Invoke();
        }
    }
}
