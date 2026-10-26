using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Search engine for Scene Explorer. Traverses loaded scenes and yields cached result items
    /// matching the specified filter criteria.
    /// </summary>
    public static class SceneExplorerEngine
    {
        public static List<SceneExplorerResultItem> Search(SceneExplorerFilter filter, TagDatabase database, int maxResults = 500)
        {
            var results = new List<SceneExplorerResultItem>();
            if (filter == null) return results;

            var visitedRoots = new HashSet<GameObject>();
            int sceneCount = SceneManager.sceneCount;

            for (int i = 0; i < sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.IsValid()) continue;

                GameObject[] roots = null;
                try
                {
                    roots = scene.GetRootGameObjects();
                }
                catch { }

                if (roots != null)
                {
                    for (int r = 0; r < roots.Length; r++)
                    {
                        var rootGo = roots[r];
                        if (rootGo != null && visitedRoots.Add(rootGo))
                        {
                            TraverseGameObject(rootGo, filter, database, results, maxResults);
                            if (results.Count >= maxResults) return results;
                        }
                    }
                }
            }

            // Fallback for active scene if not already traversed
            var activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                GameObject[] activeRoots = null;
                try { activeRoots = activeScene.GetRootGameObjects(); } catch { }
                if (activeRoots != null)
                {
                    for (int r = 0; r < activeRoots.Length; r++)
                    {
                        var rootGo = activeRoots[r];
                        if (rootGo != null && visitedRoots.Add(rootGo))
                        {
                            TraverseGameObject(rootGo, filter, database, results, maxResults);
                            if (results.Count >= maxResults) return results;
                        }
                    }
                }
            }

            return results;
        }

        private static void TraverseGameObject(GameObject go, SceneExplorerFilter filter, TagDatabase db, List<SceneExplorerResultItem> results, int maxResults)
        {
            if (go == null) return;

            if (filter.Matches(go, db))
            {
                results.Add(new SceneExplorerResultItem(go, db));
                if (results.Count >= maxResults) return;
            }

            int childCount = go.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                var child = go.transform.GetChild(i);
                if (child != null)
                {
                    TraverseGameObject(child.gameObject, filter, db, results, maxResults);
                    if (results.Count >= maxResults) return;
                }
            }
        }
    }
}
