using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Configurable rule ensuring GameObjects with specific components carry required custom tags.
    /// Example: Any object with a Light component must be tagged 'Lighting'.
    /// </summary>
    public class ComponentRequiresTagRule : SceneValidationRuleBase
    {
        public override string RuleId => "built-in.component-requires-tag";
        public override string RuleName => "Component Requires Tag";
        public override string Description => "Enforces that GameObjects possessing a designated component must have an associated custom tag.";
        public override ValidationCategory Category => ValidationCategory.Tagging;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Warning;

        public override IEnumerable<ValidationFinding> Analyze(SceneValidationContext context)
        {
            var config = context.RuleConfig;
            if (config == null || config.ComponentRequiresTags.Count == 0) yield break;

            var objects = context.AllGameObjects;
            var rules = config.ComponentRequiresTags;

            for (int r = 0; r < rules.Count; r++)
            {
                var req = rules[r];
                if (!req.IsEnabled) continue;
                if (string.IsNullOrWhiteSpace(req.ComponentTypeName)) continue;
                if (string.IsNullOrWhiteSpace(req.RequiredTagId) && string.IsNullOrWhiteSpace(req.RequiredTagName)) continue;

                for (int i = 0; i < objects.Count; i++)
                {
                    var go = objects[i];
                    if (go == null) continue;

                    if (HasComponentNamed(go, req.ComponentTypeName))
                    {
                        bool hasTag = false;
                        if (go.TryGetComponent<SceneSentryTags>(out var tagsComp))
                        {
                            if (!string.IsNullOrEmpty(req.RequiredTagId) && tagsComp.HasTagId(req.RequiredTagId)) hasTag = true;
                            else if (!string.IsNullOrEmpty(req.RequiredTagName) && tagsComp.HasTagId(req.RequiredTagName)) hasTag = true;
                            else if (!string.IsNullOrEmpty(req.RequiredTagName) && context.TagDatabase != null && tagsComp.HasTagName(req.RequiredTagName, context.TagDatabase)) hasTag = true;
                        }

                        if (!hasTag && !string.IsNullOrEmpty(req.RequiredTagName) && string.Equals(go.tag, req.RequiredTagName, StringComparison.OrdinalIgnoreCase))
                        {
                            hasTag = true;
                        }

                        if (!hasTag)
                        {
                            string tagName = !string.IsNullOrEmpty(req.RequiredTagName) ? req.RequiredTagName : req.RequiredTagId;
                            string tagToAssign = !string.IsNullOrEmpty(req.RequiredTagId) ? req.RequiredTagId : req.RequiredTagName;

                            yield return CreateFixableFinding(
                                title: $"Missing Tag '{tagName}' on '{go.name}'",
                                explanation: $"GameObject has component '{req.ComponentTypeName}' which requires custom tag '{tagName}'.",
                                targetGameObject: go,
                                targetComponent: null,
                                reason: $"Component-Tag constraint '{req.ComponentTypeName} -> {tagName}' is violated.",
                                suggestedCorrection: $"Assign tag '{tagName}' to this GameObject.",
                                fixDescription: $"Assign tag '{tagName}'",
                                fixAction: () =>
                                {
                                    if (!go.TryGetComponent<SceneSentryTags>(out var tc))
                                    {
                                        tc = Undo.AddComponent<SceneSentryTags>(go);
                                    }
                                    Undo.RecordObject(tc, $"Add Tag '{tagName}'");
                                    tc.AddTag(tagToAssign);
                                    EditorUtility.SetDirty(tc);
                                    return SafeFixResult.Ok($"Assigned tag '{tagName}' to '{go.name}'.");
                                },
                                severity: req.Severity);
                        }
                    }
                }
            }
        }

        private static bool HasComponentNamed(GameObject go, string componentName)
        {
            var components = go.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                var comp = components[i];
                if (comp == null) continue;
                if (string.Equals(comp.GetType().Name, componentName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
