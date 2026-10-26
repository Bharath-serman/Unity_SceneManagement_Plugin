using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Contextual data provided to validation rules during a scene scan.
    /// Gathers all loaded GameObjects once to avoid redundant scene traversals across rules.
    /// </summary>
    public class SceneValidationContext
    {
        public IReadOnlyList<GameObject> AllGameObjects { get; }
        public TagDatabase TagDatabase { get; }
        public SceneSentryRuleConfig RuleConfig { get; }

        public SceneValidationContext(TagDatabase database, SceneSentryRuleConfig config)
        {
            TagDatabase = database;
            RuleConfig = config;

            var list = new List<GameObject>();
            int sceneCount = SceneManager.sceneCount;

            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                var roots = scene.GetRootGameObjects();
                for (int r = 0; r < roots.Length; r++)
                {
                    Traverse(roots[r], list);
                }
            }

            AllGameObjects = list;
        }

        public SceneValidationContext(IReadOnlyList<GameObject> objects, TagDatabase database, SceneSentryRuleConfig config)
        {
            AllGameObjects = objects ?? new List<GameObject>();
            TagDatabase = database;
            RuleConfig = config;
        }

        private static void Traverse(GameObject go, List<GameObject> collector)
        {
            if (go == null) return;
            collector.Add(go);

            int childCount = go.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = go.transform.GetChild(i);
                if (child != null)
                {
                    Traverse(child.gameObject, collector);
                }
            }
        }
    }
}
