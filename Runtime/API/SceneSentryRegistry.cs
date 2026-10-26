using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneSentry
{
    /// <summary>
    /// High-performance runtime registry for fast tag-based object querying.
    /// SceneSentryTags components automatically register/unregister on enable/disable,
    /// enabling O(1) tag lookups at runtime without costly FindObjectsByType scans.
    /// </summary>
    public static class SceneSentryRegistry
    {
        private static readonly Dictionary<string, HashSet<SceneSentryTags>> s_ByTagId = 
            new Dictionary<string, HashSet<SceneSentryTags>>(StringComparer.OrdinalIgnoreCase);

        private static readonly HashSet<SceneSentryTags> s_All = new HashSet<SceneSentryTags>();

        private static readonly object s_Lock = new object();

        public static int RegisteredCount
        {
            get
            {
                lock (s_Lock)
                {
                    return s_All.Count;
                }
            }
        }

        public static void Register(SceneSentryTags component)
        {
            if (component == null) return;

            lock (s_Lock)
            {
                s_All.Add(component);
                IndexComponent(component);
            }
        }

        public static void Unregister(SceneSentryTags component)
        {
            if (component == null) return;

            lock (s_Lock)
            {
                s_All.Remove(component);
                UnindexComponent(component);
            }
        }

        public static void UpdateRegistration(SceneSentryTags component)
        {
            if (component == null) return;

            lock (s_Lock)
            {
                UnindexComponent(component);
                if (component.isActiveAndEnabled)
                {
                    s_All.Add(component);
                    IndexComponent(component);
                }
                else
                {
                    s_All.Remove(component);
                }
            }
        }

        private static void IndexComponent(SceneSentryTags component)
        {
            var tagIds = component.TagIds;
            for (int i = 0; i < tagIds.Count; i++)
            {
                string tagId = tagIds[i];
                if (string.IsNullOrWhiteSpace(tagId)) continue;

                if (!s_ByTagId.TryGetValue(tagId, out var set))
                {
                    set = new HashSet<SceneSentryTags>();
                    s_ByTagId[tagId] = set;
                }
                set.Add(component);
            }
        }

        private static void UnindexComponent(SceneSentryTags component)
        {
            foreach (var kvp in s_ByTagId)
            {
                kvp.Value.Remove(component);
            }
        }

        public static IReadOnlyCollection<SceneSentryTags> FindObjectsWithTag(string tagId)
        {
            if (string.IsNullOrWhiteSpace(tagId)) return Array.Empty<SceneSentryTags>();

            lock (s_Lock)
            {
                if (s_ByTagId.TryGetValue(tagId.Trim(), out var set))
                {
                    // Clean up any destroyed objects lazily
                    set.RemoveWhere(item => item == null);
                    return set;
                }
                return Array.Empty<SceneSentryTags>();
            }
        }

        public static List<SceneSentryTags> FindObjectsWithAnyTag(params string[] tagIds)
        {
            var results = new HashSet<SceneSentryTags>();
            if (tagIds == null || tagIds.Length == 0) return new List<SceneSentryTags>();

            lock (s_Lock)
            {
                for (int i = 0; i < tagIds.Length; i++)
                {
                    string id = tagIds[i];
                    if (string.IsNullOrWhiteSpace(id)) continue;

                    if (s_ByTagId.TryGetValue(id.Trim(), out var set))
                    {
                        foreach (var item in set)
                        {
                            if (item != null) results.Add(item);
                        }
                    }
                }
            }

            return new List<SceneSentryTags>(results);
        }

        public static List<SceneSentryTags> FindObjectsWithAllTags(params string[] tagIds)
        {
            if (tagIds == null || tagIds.Length == 0) return new List<SceneSentryTags>();

            var results = new List<SceneSentryTags>();

            lock (s_Lock)
            {
                // Start with the smallest set
                HashSet<SceneSentryTags> smallestSet = null;
                for (int i = 0; i < tagIds.Length; i++)
                {
                    string id = tagIds[i];
                    if (string.IsNullOrWhiteSpace(id)) return results;

                    if (!s_ByTagId.TryGetValue(id.Trim(), out var set) || set.Count == 0)
                    {
                        return results; // If any tag has zero objects, intersection is empty
                    }

                    if (smallestSet == null || set.Count < smallestSet.Count)
                    {
                        smallestSet = set;
                    }
                }

                if (smallestSet == null) return results;

                foreach (var candidate in smallestSet)
                {
                    if (candidate == null) continue;

                    bool hasAll = true;
                    for (int i = 0; i < tagIds.Length; i++)
                    {
                        string id = tagIds[i].Trim();
                        if (!s_ByTagId.TryGetValue(id, out var set) || !set.Contains(candidate))
                        {
                            hasAll = false;
                            break;
                        }
                    }

                    if (hasAll)
                    {
                        results.Add(candidate);
                    }
                }
            }

            return results;
        }

        public static IReadOnlyCollection<SceneSentryTags> GetAllTaggedObjects()
        {
            lock (s_Lock)
            {
                s_All.RemoveWhere(item => item == null);
                return s_All;
            }
        }

        public static void Clear()
        {
            lock (s_Lock)
            {
                s_ByTagId.Clear();
                s_All.Clear();
            }
        }
    }
}
