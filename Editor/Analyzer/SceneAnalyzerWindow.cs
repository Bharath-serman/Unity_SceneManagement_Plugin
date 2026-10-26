using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Interactive Scene Analyzer window.
    /// Runs validation rules against loaded scenes, displays actionable findings with severity indicators,
    /// and provides individual and batch safe fixes.
    /// </summary>
    public class SceneAnalyzerWindow : EditorWindow
    {
        private enum TabMode { Findings, RuleConfiguration }
        private TabMode _currentTab = TabMode.Findings;

        private List<ValidationFinding> _findings = new List<ValidationFinding>();
        private Vector2 _findingsScrollPos;
        private Vector2 _rulesScrollPos;

        private bool _filterErrors = true;
        private bool _filterWarnings = true;
        private bool _filterInfo = true;
        private string _searchFilter = string.Empty;
        private bool _coreRulesExpanded = true;

        // New rule inputs for configuration tab
        private string _newTagCompTag = string.Empty;
        private string _newTagCompType = string.Empty;
        private string _newCompTagType = string.Empty;
        private string _newCompTagTag = string.Empty;
        private string _newPairMain = string.Empty;
        private string _newPairReq = string.Empty;

        [MenuItem("Window/SceneSentry/Scene Analyzer", false, 11)]
        public static void Open()
        {
            var window = GetWindow<SceneAnalyzerWindow>("Scene Analyzer");
            window.minSize = new Vector2(520, 440);
            window.Show();
        }

        private void OnEnable()
        {
            _findings = SceneAnalyzerEngine.LatestFindings;
            SceneAnalyzerEngine.OnAnalysisCompleted += HandleAnalysisCompleted;
        }

        private void OnDisable()
        {
            SceneAnalyzerEngine.OnAnalysisCompleted -= HandleAnalysisCompleted;
        }

        private void HandleAnalysisCompleted()
        {
            _findings = SceneAnalyzerEngine.LatestFindings;
            Repaint();
        }

        private void OnGUI()
        {
            DrawWindowHeader();
            DrawTabBar();

            if (_currentTab == TabMode.Findings)
            {
                DrawFindingsView();
            }
            else
            {
                DrawRuleConfigurationView();
            }
        }

        private void DrawWindowHeader()
        {
            EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField("Scene Analyzer", SceneSentryStyles.HeaderTitleStyle);
            EditorGUILayout.LabelField("Automated scene validation, error detection, and safe remediation.", SceneSentryStyles.HeaderSubtitleStyle);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Run Analysis", GUILayout.Width(130), GUILayout.Height(30)))
            {
                _findings = SceneAnalyzerEngine.RunAnalysis();
                Repaint();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        private void DrawTabBar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            if (GUILayout.Toggle(_currentTab == TabMode.Findings, "Validation Findings", EditorStyles.toolbarButton, GUILayout.Width(140)))
            {
                _currentTab = TabMode.Findings;
            }
            if (GUILayout.Toggle(_currentTab == TabMode.RuleConfiguration, "Rule Configuration", EditorStyles.toolbarButton, GUILayout.Width(140)))
            {
                _currentTab = TabMode.RuleConfiguration;
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFindingsView()
        {
            DrawFilterToolbar();
            DrawFindingsList();
        }

        private void DrawFilterToolbar()
        {
            int errorCount = 0;
            int warningCount = 0;
            int infoCount = 0;
            int fixableCount = 0;

            for (int i = 0; i < _findings.Count; i++)
            {
                var f = _findings[i];
                if (f.Severity == ValidationSeverity.Error) errorCount++;
                else if (f.Severity == ValidationSeverity.Warning) warningCount++;
                else if (f.Severity == ValidationSeverity.Info) infoCount++;

                if (f.IsFixable) fixableCount++;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // Filter toggles
            _filterErrors = GUILayout.Toggle(_filterErrors, $"Errors ({errorCount})", EditorStyles.toolbarButton, GUILayout.Width(85));
            _filterWarnings = GUILayout.Toggle(_filterWarnings, $"Warnings ({warningCount})", EditorStyles.toolbarButton, GUILayout.Width(100));
            _filterInfo = GUILayout.Toggle(_filterInfo, $"Info ({infoCount})", EditorStyles.toolbarButton, GUILayout.Width(75));

            // Search query field
            GUILayout.Space(10);
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(180));
            if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                _searchFilter = string.Empty;
                GUI.FocusControl(null);
            }

            GUILayout.FlexibleSpace();

            // Batch Fix Button
            EditorGUI.BeginDisabledGroup(fixableCount == 0);
            if (GUILayout.Button($"Batch Fix Safe Issues ({fixableCount})", EditorStyles.toolbarButton, GUILayout.Width(170)))
            {
                var result = SafeFixManager.ApplyBatchFixes(_findings);
                EditorUtility.DisplayDialog("Safe Fix Result", result.Message, "OK");
                _findings = SceneAnalyzerEngine.RunAnalysis();
                Repaint();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();
        }

        private void DrawFindingsList()
        {
            if (SceneAnalyzerEngine.LastScanTime == null)
            {
                DrawFirstRunBanner();
                return;
            }

            var visibleFindings = GetFilteredFindings();

            if (visibleFindings.Count == 0)
            {
                DrawAllClearBanner();
                return;
            }

            _findingsScrollPos = EditorGUILayout.BeginScrollView(_findingsScrollPos);

            for (int i = 0; i < visibleFindings.Count; i++)
            {
                DrawFindingCard(visibleFindings[i]);
            }

            EditorGUILayout.EndScrollView();
        }

        private List<ValidationFinding> GetFilteredFindings()
        {
            var list = new List<ValidationFinding>();
            for (int i = 0; i < _findings.Count; i++)
            {
                var f = _findings[i];

                if (f.Severity == ValidationSeverity.Error && !_filterErrors) continue;
                if (f.Severity == ValidationSeverity.Warning && !_filterWarnings) continue;
                if (f.Severity == ValidationSeverity.Info && !_filterInfo) continue;

                if (!string.IsNullOrWhiteSpace(_searchFilter))
                {
                    string q = _searchFilter.Trim();
                    bool match = f.Title.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 f.Explanation.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 f.RuleName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                 (f.TargetGameObject != null && f.TargetGameObject.name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);

                    if (!match) continue;
                }

                list.Add(f);
            }
            return list;
        }

        private void DrawFindingCard(ValidationFinding finding)
        {
            var cardStyle = SceneSentryStyles.GetFindingCardStyle(finding.Severity);
            EditorGUILayout.BeginVertical(cardStyle);

            EditorGUILayout.BeginHorizontal();

            // Severity Badge
            Color sevColor = finding.Severity == ValidationSeverity.Error ? SceneSentryStyles.ErrorColor :
                             finding.Severity == ValidationSeverity.Warning ? SceneSentryStyles.WarningColor :
                             SceneSentryStyles.InfoColor;

            var badgeRect = GUILayoutUtility.GetRect(65, 20, GUILayout.Width(65));
            SceneSentryStyles.DrawBadge(badgeRect, finding.Severity.ToString().ToUpper(), sevColor);

            GUILayout.Space(6);

            // Title & Rule Name (Word-wrapped to prevent any clipping)
            EditorGUILayout.BeginVertical();
            GUILayout.Label(finding.Title, SceneSentryStyles.FindingTitleStyle);
            GUILayout.Label($"Rule: {finding.RuleName}", SceneSentryStyles.FindingSuggestionStyle);
            EditorGUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Target Object & Ping Button
            if (finding.TargetGameObject != null)
            {
                if (GUILayout.Button("Ping", EditorStyles.miniButton, GUILayout.Width(45)))
                {
                    EditorGUIUtility.PingObject(finding.TargetGameObject);
                }

                if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(50)))
                {
                    Selection.activeGameObject = finding.TargetGameObject;
                    EditorGUIUtility.PingObject(finding.TargetGameObject);
                }
            }

            // Fix Action Button
            if (finding.IsFixable)
            {
                if (GUILayout.Button("Fix", EditorStyles.miniButton, GUILayout.Width(45)))
                {
                    var result = SafeFixManager.ApplySingleFix(finding);
                    if (result.Success)
                    {
                        _findings = SceneAnalyzerEngine.RunAnalysis();
                        Repaint();
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Fix Error", result.Message, "OK");
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            // Contextual Explanation & Guidance (Word-wrapped)
            EditorGUILayout.Space(4);
            GUILayout.Label(finding.Explanation, SceneSentryStyles.FindingExplanationStyle);

            if (!string.IsNullOrEmpty(finding.SuggestedCorrection))
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Suggestion:", EditorStyles.miniBoldLabel, GUILayout.Width(75));
                GUILayout.Label(finding.SuggestedCorrection, SceneSentryStyles.FindingSuggestionStyle);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawFirstRunBanner()
        {
            EditorGUILayout.Space(40);
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.Label("Ready to Analyze Loaded Scenes", SceneSentryStyles.CenteredHeaderStyle);
            GUILayout.Label("Click 'Run Analysis' to scan for missing scripts, broken references, and rule violations.", SceneSentryStyles.CenteredSubtitleStyle);

            EditorGUILayout.Space(12);

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Run Scene Analysis", GUILayout.Width(160), GUILayout.Height(30)))
            {
                _findings = SceneAnalyzerEngine.RunAnalysis();
                Repaint();
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void DrawAllClearBanner()
        {
            EditorGUILayout.Space(40);
            EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();

            GUILayout.Label("All Clear! No Scene Issues Found", SceneSentryStyles.CenteredHeaderStyle);
            string duration = $"{SceneAnalyzerEngine.LastScanDurationMs:F0}ms";
            GUILayout.Label($"All active validation rules passed successfully (Scan took {duration}).", SceneSentryStyles.CenteredSubtitleStyle);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
        }

        private void DrawRuleConfigurationView()
        {
            var config = SceneAnalyzerEngine.GetOrCreateRuleConfig();
            var allRules = SceneAnalyzerEngine.GetAllRules();

            _rulesScrollPos = EditorGUILayout.BeginScrollView(_rulesScrollPos);

            EditorGUILayout.Space(6);
            GUILayout.Label("Validation Rules Configuration", SceneSentryStyles.HeaderTitleStyle);
            EditorGUILayout.HelpBox("Enable, disable, or adjust severity levels for each core and custom validation rule.", MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            _coreRulesExpanded = EditorGUILayout.Foldout(_coreRulesExpanded, $"Core System Rules ({allRules.Count})", true, EditorStyles.foldoutHeader);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_coreRulesExpanded ? "Collapse" : "Expand", EditorStyles.miniButton, GUILayout.Width(70)))
            {
                _coreRulesExpanded = !_coreRulesExpanded;
            }
            EditorGUILayout.EndHorizontal();

            if (_coreRulesExpanded)
            {
                for (int i = 0; i < allRules.Count; i++)
                {
                    var rule = allRules[i];
                    if (rule == null) continue;

                    EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);
                    EditorGUILayout.BeginHorizontal();

                    bool isEnabled = config.IsRuleEnabled(rule.RuleId, rule.IsEnabled);
                    bool newEnabled = EditorGUILayout.Toggle(isEnabled, GUILayout.Width(18));
                    if (newEnabled != isEnabled)
                    {
                        config.SetRuleEnabled(rule.RuleId, newEnabled);
                    }

                    GUILayout.Label(rule.RuleName, SceneSentryStyles.FindingTitleStyle, GUILayout.ExpandWidth(true));

                    var currentSev = config.GetRuleSeverity(rule.RuleId, rule.DefaultSeverity);
                    var newSev = (ValidationSeverity)EditorGUILayout.EnumPopup(currentSev, GUILayout.Width(80));
                    if (newSev != currentSev)
                    {
                        config.SetRuleSeverity(rule.RuleId, newSev);
                    }

                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(22);
                    GUILayout.Label(rule.Description, SceneSentryStyles.FindingExplanationStyle, GUILayout.ExpandWidth(true));
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                }
            }

            EditorGUILayout.Space(12);
            DrawConfiguredCustomRulesSection(config);

            EditorGUILayout.Space(12);
            DrawAddNewRuleSection(config);

            EditorGUILayout.EndScrollView();
        }

        private void DrawConfiguredCustomRulesSection(SceneSentryRuleConfig config)
        {
            GUILayout.Label("Configured Validation Rules (User Custom Rules)", SceneSentryStyles.SectionHeaderStyle);

            var tagCompRules = config.TagRequiresComponents;
            var compTagRules = config.ComponentRequiresTags;
            var pairRules = config.RequiredComponentPairs;
            var layerRules = config.LayerRequirements;

            int totalCustom = tagCompRules.Count + compTagRules.Count + pairRules.Count + layerRules.Count;

            if (totalCustom == 0)
            {
                EditorGUILayout.HelpBox("No custom rules created yet. Use the section below to add custom validation rules.", MessageType.None);
                return;
            }

            // 1. Tag Requires Component Rules
            for (int i = 0; i < tagCompRules.Count; i++)
            {
                var r = tagCompRules[i];
                EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);

                EditorGUILayout.BeginHorizontal();
                bool newEnabled = EditorGUILayout.Toggle(r.IsEnabled, GUILayout.Width(18));
                if (newEnabled != r.IsEnabled)
                {
                    r.IsEnabled = newEnabled;
                    EditorUtility.SetDirty(config);
                }

                GUILayout.Label($"Tag '{r.TagName}' requires component '{r.RequiredComponentTypeName}'", SceneSentryStyles.FindingTitleStyle, GUILayout.ExpandWidth(true));

                var newSev = (ValidationSeverity)EditorGUILayout.EnumPopup(r.Severity, GUILayout.Width(80));
                if (newSev != r.Severity)
                {
                    r.Severity = newSev;
                    EditorUtility.SetDirty(config);
                }

                if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(58)))
                {
                    config.RemoveTagComponentRequirementAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(22);
                GUILayout.Label($"Enforces that any GameObject carrying the '{r.TagName}' tag has a '{r.RequiredComponentTypeName}' component.", SceneSentryStyles.FindingSuggestionStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            // 2. Component Requires Tag Rules
            for (int i = 0; i < compTagRules.Count; i++)
            {
                var r = compTagRules[i];
                EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);

                EditorGUILayout.BeginHorizontal();
                bool newEnabled = EditorGUILayout.Toggle(r.IsEnabled, GUILayout.Width(18));
                if (newEnabled != r.IsEnabled)
                {
                    r.IsEnabled = newEnabled;
                    EditorUtility.SetDirty(config);
                }

                GUILayout.Label($"Component '{r.ComponentTypeName}' requires tag '{r.RequiredTagName}'", SceneSentryStyles.FindingTitleStyle, GUILayout.ExpandWidth(true));

                var newSev = (ValidationSeverity)EditorGUILayout.EnumPopup(r.Severity, GUILayout.Width(80));
                if (newSev != r.Severity)
                {
                    r.Severity = newSev;
                    EditorUtility.SetDirty(config);
                }

                if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(58)))
                {
                    config.RemoveComponentTagRequirementAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(22);
                GUILayout.Label($"Enforces that any GameObject with a '{r.ComponentTypeName}' carries the '{r.RequiredTagName}' tag.", SceneSentryStyles.FindingSuggestionStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            // 3. Component Pair Rules
            for (int i = 0; i < pairRules.Count; i++)
            {
                var p = pairRules[i];
                EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);

                EditorGUILayout.BeginHorizontal();
                bool newEnabled = EditorGUILayout.Toggle(p.IsEnabled, GUILayout.Width(18));
                if (newEnabled != p.IsEnabled)
                {
                    p.IsEnabled = newEnabled;
                    EditorUtility.SetDirty(config);
                }

                GUILayout.Label($"Component '{p.MainComponentTypeName}' requires companion '{p.RequiredPairComponentTypeName}'", SceneSentryStyles.FindingTitleStyle, GUILayout.ExpandWidth(true));

                var newSev = (ValidationSeverity)EditorGUILayout.EnumPopup(p.Severity, GUILayout.Width(80));
                if (newSev != p.Severity)
                {
                    p.Severity = newSev;
                    EditorUtility.SetDirty(config);
                }

                if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(58)))
                {
                    config.RemoveComponentPairRequirementAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(22);
                GUILayout.Label($"Enforces companion pairing between '{p.MainComponentTypeName}' and '{p.RequiredPairComponentTypeName}'.", SceneSentryStyles.FindingSuggestionStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            // 4. Layer Rules
            for (int i = 0; i < layerRules.Count; i++)
            {
                var l = layerRules[i];
                string layerName = LayerMask.LayerToName(l.ExpectedLayer);
                if (string.IsNullOrEmpty(layerName)) layerName = $"Layer {l.ExpectedLayer}";

                EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);

                EditorGUILayout.BeginHorizontal();
                bool newEnabled = EditorGUILayout.Toggle(l.IsEnabled, GUILayout.Width(18));
                if (newEnabled != l.IsEnabled)
                {
                    l.IsEnabled = newEnabled;
                    EditorUtility.SetDirty(config);
                }

                GUILayout.Label($"Tag '{l.TagName}' requires layer '{layerName}'", SceneSentryStyles.FindingTitleStyle, GUILayout.ExpandWidth(true));

                var newSev = (ValidationSeverity)EditorGUILayout.EnumPopup(l.Severity, GUILayout.Width(80));
                if (newSev != l.Severity)
                {
                    l.Severity = newSev;
                    EditorUtility.SetDirty(config);
                }

                if (GUILayout.Button("Remove", EditorStyles.miniButton, GUILayout.Width(58)))
                {
                    config.RemoveLayerRequirementAt(i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(22);
                GUILayout.Label($"Enforces that GameObjects tagged '{l.TagName}' reside on layer '{layerName}'.", SceneSentryStyles.FindingSuggestionStyle, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawAddNewRuleSection(SceneSentryRuleConfig config)
        {
            GUILayout.Label("Add New Validation Rule", SceneSentryStyles.SectionHeaderStyle);

            EditorGUILayout.BeginVertical(SceneSentryStyles.CardStyle);

            // Rule Type 1: Tag Requires Component
            EditorGUILayout.LabelField("Tag Requires Component Constraint", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _newTagCompTag = EditorGUILayout.TextField("Tag Name:", _newTagCompTag);
            _newTagCompType = EditorGUILayout.TextField("Required Component:", _newTagCompType);

            if (GUILayout.Button("+ Add Rule", EditorStyles.miniButton, GUILayout.Width(85)))
            {
                if (!string.IsNullOrWhiteSpace(_newTagCompTag) && !string.IsNullOrWhiteSpace(_newTagCompType))
                {
                    var db = TagDatabaseManager.ActiveDatabase;
                    var tagDef = db != null ? db.GetTagByName(_newTagCompTag.Trim()) : null;
                    string tagId = tagDef != null ? tagDef.Id : _newTagCompTag.Trim();

                    config.AddTagComponentRequirement(tagId, _newTagCompTag.Trim(), _newTagCompType.Trim());
                    _newTagCompTag = string.Empty;
                    _newTagCompType = string.Empty;
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Rule Type 2: Component Requires Tag
            EditorGUILayout.LabelField("Component Requires Tag Constraint", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _newCompTagType = EditorGUILayout.TextField("Component Name:", _newCompTagType);
            _newCompTagTag = EditorGUILayout.TextField("Required Tag:", _newCompTagTag);

            if (GUILayout.Button("+ Add Rule", EditorStyles.miniButton, GUILayout.Width(85)))
            {
                if (!string.IsNullOrWhiteSpace(_newCompTagType) && !string.IsNullOrWhiteSpace(_newCompTagTag))
                {
                    var db = TagDatabaseManager.ActiveDatabase;
                    var tagDef = db != null ? db.GetTagByName(_newCompTagTag.Trim()) : null;
                    string tagId = tagDef != null ? tagDef.Id : _newCompTagTag.Trim();

                    config.AddComponentTagRequirement(_newCompTagType.Trim(), tagId, _newCompTagTag.Trim());
                    _newCompTagType = string.Empty;
                    _newCompTagTag = string.Empty;
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // Rule Type 3: Component Pair Requirement
            EditorGUILayout.LabelField("Component Pair Constraint", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            _newPairMain = EditorGUILayout.TextField("Main Component:", _newPairMain);
            _newPairReq = EditorGUILayout.TextField("Companion Component:", _newPairReq);

            if (GUILayout.Button("+ Add Pair", EditorStyles.miniButton, GUILayout.Width(85)))
            {
                if (!string.IsNullOrWhiteSpace(_newPairMain) && !string.IsNullOrWhiteSpace(_newPairReq))
                {
                    config.AddComponentPairRequirement(_newPairMain.Trim(), _newPairReq.Trim());
                    _newPairMain = string.Empty;
                    _newPairReq = string.Empty;
                    GUI.FocusControl(null);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}
