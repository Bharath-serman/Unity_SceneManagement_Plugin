using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Represents a cached result item displayed within Scene Explorer.
    /// Stores key metadata to allow smooth repainting without querying GameObjects repeatedly.
    /// </summary>
    public class SceneExplorerResultItem
    {
        public GameObject GameObject { get; }
        public string Name { get; }
        public string HierarchyPath { get; }
        public string SceneName { get; }
        public int Layer { get; }
        public string UnityTag { get; }
        public bool IsActive { get; }
        public IReadOnlyList<string> CustomTagIds { get; }
        public bool HasMissingScript { get; }
        public bool HasMissingReference { get; }

        public SceneExplorerResultItem(GameObject go, TagDatabase database)
        {
            GameObject = go;
            Name = go != null ? go.name : "<Destroyed>";
            SceneName = go != null ? go.scene.name : string.Empty;
            Layer = go != null ? go.layer : 0;
            UnityTag = go != null ? go.tag : string.Empty;
            IsActive = go != null && go.activeInHierarchy;

            if (go != null)
            {
                HierarchyPath = GetGameObjectPath(go);
                if (go.TryGetComponent<SceneSentryTags>(out var tagsComponent))
                {
                    CustomTagIds = new List<string>(tagsComponent.TagIds);
                }
                else
                {
                    CustomTagIds = Array.Empty<string>();
                }

                HasMissingScript = SceneExplorerFilter.HasMissingScript(go);
                HasMissingReference = SceneExplorerFilter.HasMissingReference(go);
            }
            else
            {
                HierarchyPath = string.Empty;
                CustomTagIds = Array.Empty<string>();
            }
        }

        private static string GetGameObjectPath(GameObject go)
        {
            var stack = new List<string>();
            var current = go.transform;
            while (current != null)
            {
                stack.Add(current.name);
                current = current.parent;
            }
            stack.Reverse();
            return string.Join("/", stack);
        }
    }
}
