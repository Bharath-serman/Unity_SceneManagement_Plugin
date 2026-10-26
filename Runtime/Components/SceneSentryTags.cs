using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneSentry
{
    /// <summary>
    /// Component attached to GameObjects to assign multiple custom SceneSentry tags.
    /// Does not modify or interfere with Unity's built-in tag or layer fields.
    /// Automatically registers with the runtime SceneSentryRegistry for zero-allocation tag lookups.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("SceneSentry/SceneSentry Tags")]
    [ExecuteAlways]
    public class SceneSentryTags : MonoBehaviour
    {
        [SerializeField] private List<string> _tagIds = new List<string>();

        public IReadOnlyList<string> TagIds => _tagIds;
        public int TagCount => _tagIds.Count;

        private void OnEnable()
        {
            SceneSentryRegistry.Register(this);
        }

        private void OnDisable()
        {
            SceneSentryRegistry.Unregister(this);
        }

        private void OnDestroy()
        {
            SceneSentryRegistry.Unregister(this);
        }

        public bool HasTagId(string tagId)
        {
            if (string.IsNullOrWhiteSpace(tagId)) return false;
            string cleanId = tagId.Trim();

            for (int i = 0; i < _tagIds.Count; i++)
            {
                if (string.Equals(_tagIds[i], cleanId, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasTagName(string tagName, TagDatabase database)
        {
            if (string.IsNullOrWhiteSpace(tagName) || database == null) return false;
            var tagDef = database.GetTagByName(tagName);
            if (tagDef == null) return false;
            return HasTagId(tagDef.Id);
        }

        public bool HasAnyTagId(IEnumerable<string> tagIds)
        {
            if (tagIds == null) return false;
            foreach (var id in tagIds)
            {
                if (HasTagId(id)) return true;
            }
            return false;
        }

        public bool HasAllTagIds(IEnumerable<string> tagIds)
        {
            if (tagIds == null) return true;
            foreach (var id in tagIds)
            {
                if (!HasTagId(id)) return false;
            }
            return true;
        }

        public bool AddTag(string tagId)
        {
            if (string.IsNullOrWhiteSpace(tagId)) return false;
            string clean = tagId.Trim();

            if (HasTagId(clean)) return false;

            _tagIds.Add(clean);
            SceneSentryRegistry.UpdateRegistration(this);
            return true;
        }

        public bool RemoveTag(string tagId)
        {
            if (string.IsNullOrWhiteSpace(tagId)) return false;
            string clean = tagId.Trim();
            bool removed = false;

            for (int i = _tagIds.Count - 1; i >= 0; i--)
            {
                if (string.Equals(_tagIds[i], clean, StringComparison.OrdinalIgnoreCase))
                {
                    _tagIds.RemoveAt(i);
                    removed = true;
                }
            }

            if (removed)
            {
                SceneSentryRegistry.UpdateRegistration(this);
            }

            return removed;
        }

        public void SetTags(IEnumerable<string> tagIds)
        {
            _tagIds.Clear();
            if (tagIds != null)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var id in tagIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        string clean = id.Trim();
                        if (seen.Add(clean))
                        {
                            _tagIds.Add(clean);
                        }
                    }
                }
            }

            SceneSentryRegistry.UpdateRegistration(this);
        }

        public void ClearTags()
        {
            if (_tagIds.Count > 0)
            {
                _tagIds.Clear();
                SceneSentryRegistry.UpdateRegistration(this);
            }
        }

        public int RemoveDuplicates()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var clean = new List<string>(_tagIds.Count);
            int duplicatesRemoved = 0;

            for (int i = 0; i < _tagIds.Count; i++)
            {
                string id = _tagIds[i];
                if (string.IsNullOrWhiteSpace(id))
                {
                    duplicatesRemoved++;
                    continue;
                }

                if (seen.Add(id))
                {
                    clean.Add(id);
                }
                else
                {
                    duplicatesRemoved++;
                }
            }

            if (duplicatesRemoved > 0)
            {
                _tagIds = clean;
                SceneSentryRegistry.UpdateRegistration(this);
            }

            return duplicatesRemoved;
        }

        public int CleanInvalidTags(TagDatabase database)
        {
            if (database == null) return 0;

            int removed = 0;
            for (int i = _tagIds.Count - 1; i >= 0; i--)
            {
                if (!database.ContainsId(_tagIds[i]))
                {
                    _tagIds.RemoveAt(i);
                    removed++;
                }
            }

            if (removed > 0)
            {
                SceneSentryRegistry.UpdateRegistration(this);
            }

            return removed;
        }
    }
}
