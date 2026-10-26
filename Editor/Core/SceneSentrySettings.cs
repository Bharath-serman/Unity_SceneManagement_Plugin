using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// User preferences and project settings for SceneSentry.
    /// Exposes a clean interface in Project Settings > SceneSentry.
    /// </summary>
    public static class SceneSentrySettings
    {
        private const string ShowHierarchyBadgesKey = "SceneSentry_ShowHierarchyBadges";
        private const string AutoScanOnSaveKey = "SceneSentry_AutoScanOnSave";
        private const string MaxExplorerResultsKey = "SceneSentry_MaxExplorerResults";

        public static bool ShowHierarchyBadges
        {
            get => EditorPrefs.GetBool(ShowHierarchyBadgesKey, true);
            set => EditorPrefs.SetBool(ShowHierarchyBadgesKey, value);
        }

        public static bool AutoScanOnSave
        {
            get => EditorPrefs.GetBool(AutoScanOnSaveKey, false);
            set => EditorPrefs.SetBool(AutoScanOnSaveKey, value);
        }

        public static int MaxExplorerResults
        {
            get => EditorPrefs.GetInt(MaxExplorerResultsKey, 200);
            set => EditorPrefs.SetInt(MaxExplorerResultsKey, Mathf.Clamp(value, 20, 2000));
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/SceneSentry", SettingsScope.Project)
            {
                label = "SceneSentry",
                guiHandler = (searchContext) =>
                {
                    EditorGUILayout.Space(10);
                    EditorGUILayout.LabelField("SceneSentry Configuration", EditorStyles.boldLabel);
                    EditorGUILayout.HelpBox("Configure global behaviors and editor visual indicators for SceneSentry.", MessageType.Info);

                    EditorGUILayout.Space(8);
                    ShowHierarchyBadges = EditorGUILayout.Toggle(new GUIContent("Show Hierarchy Badges", "Renders subtle tag indicators next to tagged GameObjects in the Hierarchy view."), ShowHierarchyBadges);
                    AutoScanOnSave = EditorGUILayout.Toggle(new GUIContent("Auto-Scan On Scene Save", "Automatically runs a quick Scene Analyzer validation when a scene is saved."), AutoScanOnSave);
                    MaxExplorerResults = EditorGUILayout.IntSlider(new GUIContent("Max Explorer Results", "Caps the number of items displayed in Scene Explorer to preserve UI responsiveness."), MaxExplorerResults, 50, 1000);

                    EditorGUILayout.Space(12);
                    EditorGUILayout.LabelField("Database Status", EditorStyles.boldLabel);
                    var db = TagDatabaseManager.ActiveDatabase;
                    if (db != null)
                    {
                        EditorGUILayout.LabelField($"Active Tags in Database: {db.Count}");
                        if (GUILayout.Button("Open Tag Manager", GUILayout.Width(160)))
                        {
                            TagManagerWindow.Open();
                        }
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("No active TagDatabase found.", MessageType.Warning);
                    }
                }
            };
        }
    }
}
