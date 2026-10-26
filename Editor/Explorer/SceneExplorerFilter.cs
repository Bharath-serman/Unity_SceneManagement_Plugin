using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    public enum MatchMode
    {
        MatchAll_AND,
        MatchAny_OR
    }

    public enum ActiveStateFilter
    {
        All,
        ActiveOnly,
        InactiveOnly
    }

    /// <summary>
    /// Represents the search and filter criteria used by SceneExplorerEngine.
    /// Supports combinatorial logic across names, custom tags, Unity tags, layers, and components.
    /// </summary>
    [Serializable]
    public class SceneExplorerFilter
    {
        public string SearchText = string.Empty;
        public MatchMode LogicMode = MatchMode.MatchAll_AND;
        public ActiveStateFilter ActiveFilter = ActiveStateFilter.All;

        public List<string> CustomTagIds = new List<string>();
        public string BuiltInTag = string.Empty;
        public int LayerMask = ~0; // All layers by default
        public bool UseLayerFilter = false;

        public string ComponentTypeName = string.Empty;
        public string TargetSceneName = string.Empty;

        public bool MissingScriptsOnly = false;
        public bool MissingReferencesOnly = false;

        public bool HasActiveFilters()
        {
            return !string.IsNullOrWhiteSpace(SearchText) ||
                   ActiveFilter != ActiveStateFilter.All ||
                   CustomTagIds.Count > 0 ||
                   !string.IsNullOrWhiteSpace(BuiltInTag) ||
                   UseLayerFilter ||
                   !string.IsNullOrWhiteSpace(ComponentTypeName) ||
                   !string.IsNullOrWhiteSpace(TargetSceneName) ||
                   MissingScriptsOnly ||
                   MissingReferencesOnly;
        }

        public void Reset()
        {
            SearchText = string.Empty;
            LogicMode = MatchMode.MatchAll_AND;
            ActiveFilter = ActiveStateFilter.All;
            CustomTagIds.Clear();
            BuiltInTag = string.Empty;
            LayerMask = ~0;
            UseLayerFilter = false;
            ComponentTypeName = string.Empty;
            TargetSceneName = string.Empty;
            MissingScriptsOnly = false;
            MissingReferencesOnly = false;
        }

        public bool Matches(GameObject go, TagDatabase database)
        {
            if (go == null) return false;

            // 1. Mandatory hard constraints (active state, scene target)
            if (ActiveFilter == ActiveStateFilter.ActiveOnly && !go.activeInHierarchy) return false;
            if (ActiveFilter == ActiveStateFilter.InactiveOnly && go.activeInHierarchy) return false;

            if (!string.IsNullOrWhiteSpace(TargetSceneName) &&
                !string.Equals(go.scene.name, TargetSceneName.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Quick filters
            if (MissingScriptsOnly && !HasMissingScript(go)) return false;
            if (MissingReferencesOnly && !HasMissingReference(go)) return false;

            // Collect conditions that are currently specified
            var conditions = new List<bool>();

            // Text search (Name or path)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                string term = SearchText.Trim();
                bool nameMatch = go.name.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
                conditions.Add(nameMatch);
            }

            // Custom Tags (matches custom tags by ID, by Name, or Unity native tag by Name)
            if (CustomTagIds.Count > 0)
            {
                bool tagMatch = false;

                if (go.TryGetComponent<SceneSentryTags>(out var tagsComponent))
                {
                    // 1. Check directly by tag ID
                    if (tagsComponent.HasAnyTagId(CustomTagIds))
                    {
                        tagMatch = true;
                    }
                    else if (database != null)
                    {
                        // 2. Check if tagsComponent matches by tag name
                        for (int t = 0; t < CustomTagIds.Count; t++)
                        {
                            var def = database.GetTagById(CustomTagIds[t]);
                            if (def != null && tagsComponent.HasTagName(def.Name, database))
                            {
                                tagMatch = true;
                                break;
                            }
                        }
                    }
                }

                // 3. Fallback: also match if Unity's native tag matches the selected tag name
                if (!tagMatch && database != null)
                {
                    for (int t = 0; t < CustomTagIds.Count; t++)
                    {
                        var def = database.GetTagById(CustomTagIds[t]);
                        if (def != null && string.Equals(go.tag, def.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            tagMatch = true;
                            break;
                        }
                        if (string.Equals(go.tag, CustomTagIds[t], StringComparison.OrdinalIgnoreCase))
                        {
                            tagMatch = true;
                            break;
                        }
                    }
                }

                conditions.Add(tagMatch);
            }

            // Built-in Tag
            if (!string.IsNullOrWhiteSpace(BuiltInTag))
            {
                bool unityTagMatch = string.Equals(go.tag, BuiltInTag.Trim(), StringComparison.OrdinalIgnoreCase);
                conditions.Add(unityTagMatch);
            }

            // Layer
            if (UseLayerFilter)
            {
                bool layerMatch = (LayerMask & (1 << go.layer)) != 0;
                conditions.Add(layerMatch);
            }

            // Component Type
            if (!string.IsNullOrWhiteSpace(ComponentTypeName))
            {
                bool compMatch = MatchesComponent(go, ComponentTypeName.Trim());
                conditions.Add(compMatch);
            }

            if (conditions.Count == 0)
            {
                return true; // No conditions specified -> matches all remaining
            }

            if (LogicMode == MatchMode.MatchAll_AND)
            {
                for (int i = 0; i < conditions.Count; i++)
                {
                    if (!conditions[i]) return false;
                }
                return true;
            }
            else // MatchAny_OR
            {
                for (int i = 0; i < conditions.Count; i++)
                {
                    if (conditions[i]) return true;
                }
                return false;
            }
        }

        private static bool MatchesComponent(GameObject go, string componentName)
        {
            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                var comp = components[i];
                if (comp == null) continue;
                string typeName = comp.GetType().Name;
                if (typeName.IndexOf(componentName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        public static bool HasMissingScript(GameObject go)
        {
            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null) return true;
            }
            return false;
        }

        public static bool HasMissingReference(GameObject go)
        {
            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                var comp = components[i];
                if (comp == null) continue;

                var so = new SerializedObject(comp);
                var prop = so.GetIterator();

                while (prop.NextVisible(true))
                {
                    if (prop.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        if (prop.objectReferenceInstanceIDValue != 0 && prop.objectReferenceValue == null)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }
}
