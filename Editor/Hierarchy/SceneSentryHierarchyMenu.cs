using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Integrates SceneSentry into the Unity Hierarchy window:
    /// - Context menu actions for tagging GameObjects.
    /// - Subtle visual tag indicators rendered on hierarchy rows.
    /// </summary>
    [InitializeOnLoad]
    public static class SceneSentryHierarchyMenu
    {
        static SceneSentryHierarchyMenu()
        {
            EditorApplication.hierarchyWindowItemOnGUI -= OnHierarchyGUI;
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            if (!SceneSentrySettings.ShowHierarchyBadges) return;

            var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
            if (go == null) return;

            if (!go.TryGetComponent<SceneSentryTags>(out var tagsComponent) || tagsComponent.TagCount == 0)
            {
                return;
            }

            var db = TagDatabaseManager.ActiveDatabase;
            var tagIds = tagsComponent.TagIds;

            // Draw badges aligned to the right side of hierarchy row
            float rightOffset = selectionRect.xMax - 6f;
            int maxBadges = Mathf.Min(3, tagIds.Count);

            for (int i = 0; i < maxBadges; i++)
            {
                string tagId = tagIds[i];
                var tagDef = db != null ? db.GetTagById(tagId) : null;
                Color dotColor = tagDef != null ? tagDef.Color : Color.gray;

                rightOffset -= 10f;
                var dotRect = new Rect(rightOffset, selectionRect.y + (selectionRect.height - 8f) * 0.5f, 8f, 8f);

                EditorGUI.DrawRect(dotRect, dotColor);
            }

            if (tagIds.Count > 3)
            {
                rightOffset -= 14f;
                var moreRect = new Rect(rightOffset, selectionRect.y, 14f, selectionRect.height);
                GUI.Label(moreRect, "+", EditorStyles.miniLabel);
            }
        }

        [MenuItem("GameObject/SceneSentry/Add SceneSentry Tags", false, 20)]
        private static void AddTagsComponent(MenuCommand command)
        {
            var targetGo = command.context as GameObject;
            if (targetGo == null && Selection.activeGameObject != null)
            {
                targetGo = Selection.activeGameObject;
            }

            if (targetGo != null)
            {
                if (!targetGo.TryGetComponent<SceneSentryTags>(out _))
                {
                    Undo.AddComponent<SceneSentryTags>(targetGo);
                }
            }
        }

        [MenuItem("GameObject/SceneSentry/Clear All Tags", false, 21)]
        private static void ClearAllTags(MenuCommand command)
        {
            var gameObjects = Selection.gameObjects;
            if (gameObjects == null || gameObjects.Length == 0) return;

            Undo.RecordObjects(gameObjects, "Clear SceneSentry Tags");
            for (int i = 0; i < gameObjects.Length; i++)
            {
                if (gameObjects[i].TryGetComponent<SceneSentryTags>(out var comp))
                {
                    Undo.RecordObject(comp, "Clear SceneSentry Tags");
                    comp.ClearTags();
                    EditorUtility.SetDirty(comp);
                }
            }
        }

        [MenuItem("GameObject/SceneSentry/Inspect in Scene Explorer", false, 22)]
        private static void InspectInSceneExplorer()
        {
            SceneExplorerWindow.Open();
        }
    }
}
