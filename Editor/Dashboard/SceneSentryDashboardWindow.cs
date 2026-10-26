using System;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Central dashboard and primary entry point for the SceneSentry toolkit.
    /// Provides quick navigation between tools, high-level scene health metrics, and documentation links.
    /// </summary>
    public class SceneSentryDashboardWindow : EditorWindow
    {
        private Vector2 _scrollPos;

        [MenuItem("Window/SceneSentry/Dashboard", false, 0)]
        [MenuItem("Window/SceneSentry/Open SceneSentry", false, 1)]
        public static void Open()
        {
            var window = GetWindow<SceneSentryDashboardWindow>("SceneSentry");
            window.minSize = new Vector2(560, 480);
            window.Show();
        }

        private void OnEnable()
        {
            TagDatabaseManager.OnDatabaseChanged += Repaint;
            SceneAnalyzerEngine.OnAnalysisCompleted += Repaint;
        }

        private void OnDisable()
        {
            TagDatabaseManager.OnDatabaseChanged -= Repaint;
            SceneAnalyzerEngine.OnAnalysisCompleted -= Repaint;
        }

        private void OnGUI()
        {
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            DrawHeroHeader();
            EditorGUILayout.Space(10);

            DrawQuickNavigationGrid();
            EditorGUILayout.Space(12);

            DrawSceneHealthCard();
            EditorGUILayout.Space(12);

            DrawStatisticsCard();
            EditorGUILayout.Space(12);

            DrawDocumentationCard();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeroHeader()
        {
            EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            GUILayout.Label("SceneSentry", SceneSentryStyles.HeaderTitleStyle);
            GUILayout.Label("Find it. Check it. Fix it. - Complete Scene Organization & Validation for Unity 6", SceneSentryStyles.HeaderSubtitleStyle);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            var versionRect = GUILayoutUtility.GetRect(65, 22, GUILayout.Width(65));
            SceneSentryStyles.DrawBadge(versionRect, "v1.0.0", SceneSentryStyles.AccentColor);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawQuickNavigationGrid()
        {
            EditorGUILayout.LabelField("Toolkit Tools", SceneSentryStyles.SectionHeaderStyle);

            // Compute exact half width for 2-column layout that evenly spreads across the window width
            float cardWidth = Mathf.Max(180f, (position.width - 28f) * 0.5f);

            EditorGUILayout.BeginHorizontal();

            // Card 1: Scene Explorer
            DrawToolCard(
                "Scene Explorer",
                "Search and filter GameObjects across scenes using tags, components, and layers.",
                "Open Explorer",
                () => SceneExplorerWindow.Open(),
                cardWidth);

            // Card 2: Scene Analyzer
            DrawToolCard(
                "Scene Analyzer",
                "Scan scene for missing scripts, broken references, and rule violations.",
                "Open Analyzer",
                () => SceneAnalyzerWindow.Open(),
                cardWidth);

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginHorizontal();

            // Card 3: Tag Manager
            DrawToolCard(
                "Tag Manager",
                "Create, color-code, and organize custom tags in a hierarchical taxonomy.",
                "Open Tag Manager",
                () => TagManagerWindow.Open(),
                cardWidth);

            // Card 4: Settings & Rules
            DrawToolCard(
                "Project Settings",
                "Configure validation rule parameters, component requirements, and editor preferences.",
                "Open Settings",
                () => SettingsService.OpenProjectSettings("Project/SceneSentry"),
                cardWidth);

            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolCard(string title, string description, string buttonText, Action onClick, float width)
        {
            EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle, GUILayout.Width(width), GUILayout.MinHeight(105));

            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel, GUILayout.MinHeight(34));

            GUILayout.FlexibleSpace();
            if (GUILayout.Button(buttonText, EditorStyles.miniButton, GUILayout.Height(22)))
            {
                onClick?.Invoke();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawSceneHealthCard()
        {
            EditorGUILayout.LabelField("Scene Health Overview", SceneSentryStyles.SectionHeaderStyle);
            EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);

            if (SceneAnalyzerEngine.LastScanTime.HasValue)
            {
                var findings = SceneAnalyzerEngine.LatestFindings;
                int errors = 0;
                int warnings = 0;
                int info = 0;

                for (int i = 0; i < findings.Count; i++)
                {
                    if (findings[i].Severity == ValidationSeverity.Error) errors++;
                    else if (findings[i].Severity == ValidationSeverity.Warning) warnings++;
                    else if (findings[i].Severity == ValidationSeverity.Info) info++;
                }

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Last Scan: {SceneAnalyzerEngine.LastScanTime.Value:HH:mm:ss} ({SceneAnalyzerEngine.LastScanDurationMs:F0}ms)", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Rescan Active Scene", EditorStyles.miniButton, GUILayout.Width(140)))
                {
                    SceneAnalyzerEngine.RunAnalysis();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(8);
                EditorGUILayout.BeginHorizontal();

                // Badges
                var r1 = GUILayoutUtility.GetRect(110, 24, GUILayout.Width(110));
                SceneSentryStyles.DrawBadge(r1, $"{errors} Errors", errors > 0 ? SceneSentryStyles.ErrorColor : SceneSentryStyles.SuccessColor);

                GUILayout.Space(8);
                var r2 = GUILayoutUtility.GetRect(120, 24, GUILayout.Width(120));
                SceneSentryStyles.DrawBadge(r2, $"{warnings} Warnings", warnings > 0 ? SceneSentryStyles.WarningColor : SceneSentryStyles.SuccessColor);

                GUILayout.Space(8);
                var r3 = GUILayoutUtility.GetRect(100, 24, GUILayout.Width(100));
                SceneSentryStyles.DrawBadge(r3, $"{info} Info", SceneSentryStyles.InfoColor);

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("View Findings", EditorStyles.miniButton, GUILayout.Width(120), GUILayout.Height(24)))  //Width of the View Findings Button.
                {
                    SceneAnalyzerWindow.Open();
                }

                EditorGUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("No scene scan has been executed yet in this session.", MessageType.Info);
                EditorGUILayout.Space(4);

                if (GUILayout.Button("Scan Active Scene Now", GUILayout.Height(28)))
                {
                    SceneAnalyzerEngine.RunAnalysis();
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawStatisticsCard()
        {
            EditorGUILayout.LabelField("Project & Scene Metrics", SceneSentryStyles.SectionHeaderStyle);
            EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);

            var db = TagDatabaseManager.ActiveDatabase;
            int totalTags = db != null ? db.Count : 0;
            int registeredTaggedObjects = SceneSentryRegistry.RegisteredCount;

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Defined Custom Tags", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(totalTags.ToString(), SceneSentryStyles.HeaderTitleStyle);
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Active Tagged GameObjects", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(registeredTaggedObjects.ToString(), SceneSentryStyles.HeaderTitleStyle);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Manage Tags", EditorStyles.miniButton, GUILayout.Width(110)))
            {
                TagManagerWindow.Open();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawDocumentationCard()
        {
            EditorGUILayout.LabelField("Help & Documentation", SceneSentryStyles.SectionHeaderStyle);
            EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);

            EditorGUILayout.LabelField("Need guidance or creating custom validation rules?", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Read the local documentation included directly in this package.", EditorStyles.miniLabel);

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("User Guide", EditorStyles.miniButton))
            {
                OpenFileOrUrl("Packages/com.scenesentry.toolkit/Documentation~/UserGuide.md");
            }

            if (GUILayout.Button("API Reference", EditorStyles.miniButton))
            {
                OpenFileOrUrl("Packages/com.scenesentry.toolkit/Documentation~/ApiReference.md");
            }

            if (GUILayout.Button("Custom Rules Guide", EditorStyles.miniButton))
            {
                OpenFileOrUrl("Packages/com.scenesentry.toolkit/Documentation~/CustomRulesGuide.md");
            }

            if (GUILayout.Button("Installation Guide", EditorStyles.miniButton))
            {
                OpenFileOrUrl("Packages/com.scenesentry.toolkit/Documentation~/InstallationGuide.md");
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private static void OpenFileOrUrl(string relativePath)
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
            if (asset != null)
            {
                AssetDatabase.OpenAsset(asset);
            }
            else
            {
                // Fallback to absolute file opening or message
                string fullPath = System.IO.Path.GetFullPath(relativePath);
                if (System.IO.File.Exists(fullPath))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(fullPath) { UseShellExecute = true });
                }
                else
                {
                    EditorUtility.DisplayDialog("SceneSentry Documentation", $"Documentation file located at:\n{relativePath}", "OK");
                }
            }
        }
    }
}
