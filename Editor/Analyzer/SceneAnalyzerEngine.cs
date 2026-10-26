using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Core engine executing scene validation rules.
    /// Dynamically discovers all built-in and custom ISceneValidationRule implementations,
    /// applies rule configurations, and generates comprehensive validation reports.
    /// </summary>
    public static class SceneAnalyzerEngine
    {
        private const string DefaultConfigPath = "Assets/SceneSentryData/RuleConfig.asset";
        private static SceneSentryRuleConfig s_RuleConfig;
        private static List<ISceneValidationRule> s_Rules;

        public static List<ValidationFinding> LatestFindings { get; private set; } = new List<ValidationFinding>();
        public static DateTime? LastScanTime { get; private set; }
        public static float LastScanDurationMs { get; private set; }

        public static event Action OnAnalysisCompleted;

        public static SceneSentryRuleConfig GetOrCreateRuleConfig()
        {
            if (s_RuleConfig != null) return s_RuleConfig;

            string[] guids = AssetDatabase.FindAssets("t:SceneSentryRuleConfig");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                s_RuleConfig = AssetDatabase.LoadAssetAtPath<SceneSentryRuleConfig>(path);
                if (s_RuleConfig != null) return s_RuleConfig;
            }

            try
            {
                if (!AssetDatabase.IsValidFolder("Assets/SceneSentryData"))
                {
                    if (AssetDatabase.IsValidFolder("Assets"))
                    {
                        AssetDatabase.CreateFolder("Assets", "SceneSentryData");
                    }
                }

                s_RuleConfig = ScriptableObject.CreateInstance<SceneSentryRuleConfig>();
                if (AssetDatabase.IsValidFolder("Assets/SceneSentryData"))
                {
                    AssetDatabase.CreateAsset(s_RuleConfig, DefaultConfigPath);
                    AssetDatabase.SaveAssets();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SceneSentry] Using in-memory RuleConfig: {ex.Message}");
                s_RuleConfig = ScriptableObject.CreateInstance<SceneSentryRuleConfig>();
            }

            return s_RuleConfig;
        }

        public static IReadOnlyList<ISceneValidationRule> GetAllRules()
        {
            if (s_Rules == null)
            {
                s_Rules = DiscoverRules();
            }
            return s_Rules;
        }

        public static void SetRulesForTesting(List<ISceneValidationRule> testRules)
        {
            s_Rules = testRules;
        }

        private static List<ISceneValidationRule> DiscoverRules()
        {
            var rules = new List<ISceneValidationRule>();
            var ruleTypes = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    foreach (var type in assembly.GetTypes())
                    {
                        if (typeof(ISceneValidationRule).IsAssignableFrom(type) &&
                            !type.IsInterface &&
                            !type.IsAbstract)
                        {
                            ruleTypes.Add(type);
                        }
                    }
                }
                catch
                {
                    // Ignore reflection errors on third-party dynamic assemblies
                }
            }

            for (int i = 0; i < ruleTypes.Count; i++)
            {
                try
                {
                    var instance = (ISceneValidationRule)Activator.CreateInstance(ruleTypes[i]);
                    rules.Add(instance);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SceneSentry] Failed to instantiate rule '{ruleTypes[i].Name}': {ex.Message}");
                }
            }

            return rules;
        }

        public static List<ValidationFinding> RunAnalysis(TagDatabase database = null, SceneSentryRuleConfig config = null, bool showProgress = true)
        {
            var stopwatch = Stopwatch.StartNew();

            var activeDb = database ?? TagDatabaseManager.ActiveDatabase;
            var activeConfig = config ?? GetOrCreateRuleConfig();
            var rules = GetAllRules();

            var context = new SceneValidationContext(activeDb, activeConfig);
            var findings = new List<ValidationFinding>();

            try
            {
                for (int i = 0; i < rules.Count; i++)
                {
                    var rule = rules[i];
                    if (rule == null) continue;

                    // Sync configuration
                    bool isEnabled = activeConfig.IsRuleEnabled(rule.RuleId, rule.IsEnabled);
                    rule.IsEnabled = isEnabled;
                    rule.ConfiguredSeverity = activeConfig.GetRuleSeverity(rule.RuleId, rule.DefaultSeverity);

                    if (!rule.IsEnabled) continue;

                    if (showProgress)
                    {
                        EditorUtility.DisplayProgressBar(
                            "SceneSentry Analyzer",
                            $"Running {rule.RuleName} ({i + 1}/{rules.Count})...",
                            (float)(i + 1) / rules.Count);
                    }

                    try
                    {
                        var ruleFindings = rule.Analyze(context);
                        if (ruleFindings != null)
                        {
                            foreach (var f in ruleFindings)
                            {
                                if (f != null) findings.Add(f);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        findings.Add(new ValidationFinding(
                            ruleId: rule.RuleId,
                            ruleName: rule.RuleName,
                            title: $"Rule Execution Issue: {rule.RuleName}",
                            explanation: $"Rule encountered an exception while analyzing: {ex.Message}",
                            targetGameObject: null,
                            targetComponent: null,
                            reason: ex.Message,
                            suggestedCorrection: "Verify scene GameObject parameters or custom rule definitions.",
                            severity: ValidationSeverity.Error));
                    }
                }
            }
            finally
            {
                if (showProgress)
                {
                    EditorUtility.ClearProgressBar();
                }
            }

            stopwatch.Stop();
            LastScanDurationMs = stopwatch.ElapsedMilliseconds;
            LastScanTime = DateTime.Now;
            LatestFindings = findings;

            OnAnalysisCompleted?.Invoke();
            return findings;
        }
    }
}
