using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Singleton manager responsible for discovering, creating, and persisting the project-wide TagDatabase.
    /// Handles Undo/Redo integration and prevents duplicate database assets from being generated.
    /// </summary>
    [InitializeOnLoad]
    public static class TagDatabaseManager
    {
        private const string DefaultFolder = "Assets/SceneSentryData";
        private const string DefaultAssetPath = "Assets/SceneSentryData/TagDatabase.asset";

        private static TagDatabase s_ActiveDatabase;
        public static event Action OnDatabaseChanged;

        static TagDatabaseManager()
        {
            EditorApplication.delayCall += Initialize;
        }

        public static void Initialize()
        {
            GetOrCreateDatabase();
        }

        public static TagDatabase GetOrCreateDatabase()
        {
            if (s_ActiveDatabase != null)
            {
                return s_ActiveDatabase;
            }

            // Find existing asset
            string[] guids = AssetDatabase.FindAssets("t:TagDatabase");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                s_ActiveDatabase = AssetDatabase.LoadAssetAtPath<TagDatabase>(path);
                if (s_ActiveDatabase != null)
                {
                    s_ActiveDatabase.OnDatabaseModified -= HandleDatabaseModified;
                    s_ActiveDatabase.OnDatabaseModified += HandleDatabaseModified;
                    return s_ActiveDatabase;
                }
            }

            // If not found, create new asset at default location
            try
            {
                if (!AssetDatabase.IsValidFolder(DefaultFolder))
                {
                    if (!AssetDatabase.IsValidFolder("Assets"))
                    {
                        // In non-standard or headless test environments without Assets folder
                        s_ActiveDatabase = ScriptableObject.CreateInstance<TagDatabase>();
                        s_ActiveDatabase.OnDatabaseModified += HandleDatabaseModified;
                        return s_ActiveDatabase;
                    }
                    AssetDatabase.CreateFolder("Assets", "SceneSentryData");
                }

                s_ActiveDatabase = ScriptableObject.CreateInstance<TagDatabase>();
                AssetDatabase.CreateAsset(s_ActiveDatabase, DefaultAssetPath);
                AssetDatabase.SaveAssets();

                s_ActiveDatabase.OnDatabaseModified += HandleDatabaseModified;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SceneSentry] Unable to persist TagDatabase asset, using in-memory database: {ex.Message}");
                s_ActiveDatabase = ScriptableObject.CreateInstance<TagDatabase>();
                s_ActiveDatabase.OnDatabaseModified += HandleDatabaseModified;
            }

            return s_ActiveDatabase;
        }

        public static TagDatabase ActiveDatabase => GetOrCreateDatabase();

        public static void SetActiveDatabaseForTesting(TagDatabase testDb)
        {
            s_ActiveDatabase = testDb;
            if (s_ActiveDatabase != null)
            {
                s_ActiveDatabase.OnDatabaseModified -= HandleDatabaseModified;
                s_ActiveDatabase.OnDatabaseModified += HandleDatabaseModified;
            }
        }

        private static void HandleDatabaseModified()
        {
            if (s_ActiveDatabase != null)
            {
                EditorUtility.SetDirty(s_ActiveDatabase);
            }
            OnDatabaseChanged?.Invoke();
        }

        public static TagDefinition CreateTagWithUndo(string name, string description = "", Color? color = null, string parentId = null)
        {
            var db = ActiveDatabase;
            if (db == null) return null;

            Undo.RecordObject(db, $"SceneSentry: Create Tag '{name}'");
            var tag = db.CreateTag(name, description, color, parentId);
            EditorUtility.SetDirty(db);
            AssetDatabase.SaveAssets();
            OnDatabaseChanged?.Invoke();
            return tag;
        }

        public static bool RenameTagWithUndo(string id, string newName)
        {
            var db = ActiveDatabase;
            if (db == null) return false;

            Undo.RecordObject(db, $"SceneSentry: Rename Tag to '{newName}'");
            bool success = db.RenameTag(id, newName);
            if (success)
            {
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
                OnDatabaseChanged?.Invoke();
            }
            return success;
        }

        public static bool UpdateTagWithUndo(string id, string newName, string description, Color color, string parentId)
        {
            var db = ActiveDatabase;
            if (db == null) return false;

            Undo.RecordObject(db, $"SceneSentry: Update Tag '{newName}'");
            bool success = db.UpdateTag(id, newName, description, color, parentId);
            if (success)
            {
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
                OnDatabaseChanged?.Invoke();
            }
            return success;
        }

        public static bool DeleteTagWithUndo(string id)
        {
            var db = ActiveDatabase;
            if (db == null) return false;

            var tag = db.GetTagById(id);
            string tagName = tag != null ? tag.Name : id;

            Undo.RecordObject(db, $"SceneSentry: Delete Tag '{tagName}'");
            bool success = db.DeleteTag(id);
            if (success)
            {
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
                OnDatabaseChanged?.Invoke();
            }
            return success;
        }
    }
}
