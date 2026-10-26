using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneSentry
{
    /// <summary>
    /// Developer-friendly runtime API for querying SceneSentry tags on GameObjects.
    /// Provides zero-allocation tag checks and fast registry-based queries.
    /// </summary>
    public static class SceneSentryAPI
    {
        /// <summary>
        /// Checks whether the specified GameObject has a SceneSentry tag by ID or by Name.
        /// </summary>
        public static bool HasTag(GameObject go, string tagIdOrName, TagDatabase database = null)
        {
            if (go == null || string.IsNullOrWhiteSpace(tagIdOrName)) return false;

            if (!go.TryGetComponent<SceneSentryTags>(out var tagsComponent))
            {
                return false;
            }

            // Check directly by ID first
            if (tagsComponent.HasTagId(tagIdOrName))
            {
                return true;
            }

            // If database provided, resolve name to ID
            if (database != null)
            {
                var def = database.GetTagByName(tagIdOrName);
                if (def != null && tagsComponent.HasTagId(def.Id))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the Component's GameObject has a SceneSentry tag.
        /// </summary>
        public static bool HasTag(Component component, string tagIdOrName, TagDatabase database = null)
        {
            return component != null && HasTag(component.gameObject, tagIdOrName, database);
        }

        /// <summary>
        /// Checks whether the GameObject has ANY of the specified tags.
        /// </summary>
        public static bool HasAnyTag(GameObject go, IEnumerable<string> tagIdsOrNames, TagDatabase database = null)
        {
            if (go == null || tagIdsOrNames == null) return false;

            if (!go.TryGetComponent<SceneSentryTags>(out var tagsComponent))
            {
                return false;
            }

            foreach (var tag in tagIdsOrNames)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                if (tagsComponent.HasTagId(tag)) return true;

                if (database != null)
                {
                    var def = database.GetTagByName(tag);
                    if (def != null && tagsComponent.HasTagId(def.Id)) return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks whether the GameObject has ALL of the specified tags.
        /// </summary>
        public static bool HasAllTags(GameObject go, IEnumerable<string> tagIdsOrNames, TagDatabase database = null)
        {
            if (go == null || tagIdsOrNames == null) return false;

            if (!go.TryGetComponent<SceneSentryTags>(out var tagsComponent))
            {
                return false;
            }

            foreach (var tag in tagIdsOrNames)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;

                bool matched = tagsComponent.HasTagId(tag);
                if (!matched && database != null)
                {
                    var def = database.GetTagByName(tag);
                    if (def != null && tagsComponent.HasTagId(def.Id))
                    {
                        matched = true;
                    }
                }

                if (!matched) return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieves the list of assigned custom tag IDs on the GameObject.
        /// </summary>
        public static IReadOnlyList<string> GetTagIds(GameObject go)
        {
            if (go != null && go.TryGetComponent<SceneSentryTags>(out var tagsComponent))
            {
                return tagsComponent.TagIds;
            }
            return Array.Empty<string>();
        }

        /// <summary>
        /// Retrieves the list of resolved TagDefinitions assigned to the GameObject.
        /// </summary>
        public static List<TagDefinition> GetTags(GameObject go, TagDatabase database)
        {
            var results = new List<TagDefinition>();
            if (go == null || database == null) return results;

            if (go.TryGetComponent<SceneSentryTags>(out var tagsComponent))
            {
                var ids = tagsComponent.TagIds;
                for (int i = 0; i < ids.Count; i++)
                {
                    var def = database.GetTagById(ids[i]);
                    if (def != null)
                    {
                        results.Add(def);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Returns all active GameObjects currently registered with the given tag ID.
        /// Utilizes the O(1) runtime registry.
        /// </summary>
        public static List<GameObject> FindGameObjectsWithTag(string tagId)
        {
            var taggedComponents = SceneSentryRegistry.FindObjectsWithTag(tagId);
            var results = new List<GameObject>(taggedComponents.Count);

            foreach (var comp in taggedComponents)
            {
                if (comp != null)
                {
                    results.Add(comp.gameObject);
                }
            }

            return results;
        }

        /// <summary>
        /// Returns all active GameObjects currently registered with ALL given tag IDs.
        /// </summary>
        public static List<GameObject> FindGameObjectsWithAllTags(params string[] tagIds)
        {
            var taggedComponents = SceneSentryRegistry.FindObjectsWithAllTags(tagIds);
            var results = new List<GameObject>(taggedComponents.Count);

            for (int i = 0; i < taggedComponents.Count; i++)
            {
                var comp = taggedComponents[i];
                if (comp != null)
                {
                    results.Add(comp.gameObject);
                }
            }

            return results;
        }

        /// <summary>
        /// Returns all active GameObjects currently registered with ANY of the given tag IDs.
        /// </summary>
        public static List<GameObject> FindGameObjectsWithAnyTag(params string[] tagIds)
        {
            var taggedComponents = SceneSentryRegistry.FindObjectsWithAnyTag(tagIds);
            var results = new List<GameObject>(taggedComponents.Count);

            for (int i = 0; i < taggedComponents.Count; i++)
            {
                var comp = taggedComponents[i];
                if (comp != null)
                {
                    results.Add(comp.gameObject);
                }
            }

            return results;
        }
    }
}
