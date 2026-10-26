using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Configurable rule ensuring GameObjects with specific custom tags contain required components.
    /// Example: Any object tagged 'Interactive' must have a Collider or interaction component.
    /// </summary>
    public class TagRequiresComponentRule : SceneValidationRuleBase
    {
        public override string RuleId => "built-in.tag-requires-component";
        public override string RuleName => "Tag Requires Component";
        public override string Description => "Enforces that GameObjects tagged with a specific custom tag must have a designated component.";
        public override ValidationCategory Category => ValidationCategory.MissingComponents;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Error;

        public override IEnumerable<ValidationFinding> Analyze(SceneValidationContext context)
        {
            var config = context.RuleConfig;
            if (config == null || config.TagRequiresComponents.Count == 0) yield break;

            var objects = context.AllGameObjects;
            var rules = config.TagRequiresComponents;

            for (int r = 0; r < rules.Count; r++)
            {
                var req = rules[r];
                if (!req.IsEnabled) continue;
                if (string.IsNullOrWhiteSpace(req.TagId) && string.IsNullOrWhiteSpace(req.TagName)) continue;
                if (string.IsNullOrWhiteSpace(req.RequiredComponentTypeName)) continue;

                string targetTagId = req.TagId;
                string targetTagName = req.TagName;

                for (int i = 0; i < objects.Count; i++)
                {
                    var go = objects[i];
                    if (go == null) continue;

                    bool hasTag = false;
                    if (go.TryGetComponent<SceneSentryTags>(out var tagsComp))
                    {
                        if (!string.IsNullOrEmpty(targetTagId) && tagsComp.HasTagId(targetTagId)) hasTag = true;
                        else if (!string.IsNullOrEmpty(targetTagName) && tagsComp.HasTagId(targetTagName)) hasTag = true;
                        else if (!string.IsNullOrEmpty(targetTagName) && context.TagDatabase != null && tagsComp.HasTagName(targetTagName, context.TagDatabase)) hasTag = true;
                    }

                    if (!hasTag && !string.IsNullOrEmpty(targetTagName) && string.Equals(go.tag, targetTagName, StringComparison.OrdinalIgnoreCase))
                    {
                        hasTag = true;
                    }

                    if (hasTag)
                    {
                        if (!HasComponentNamed(go, req.RequiredComponentTypeName))
                        {
                            Type compType = ResolveComponentType(req.RequiredComponentTypeName);
                            bool canAutoAdd = compType != null && typeof(Component).IsAssignableFrom(compType);

                            string displayTag = !string.IsNullOrEmpty(req.TagName) ? req.TagName : req.TagId;

                            if (canAutoAdd)
                            {
                                yield return CreateFixableFinding(
                                    title: $"Missing '{req.RequiredComponentTypeName}' on Tagged '{go.name}'",
                                    explanation: $"GameObject is tagged '{displayTag}' which requires component '{req.RequiredComponentTypeName}'.",
                                    targetGameObject: go,
                                    targetComponent: null,
                                    reason: $"Tag-Component constraint '{displayTag} -> {req.RequiredComponentTypeName}' is violated.",
                                    suggestedCorrection: $"Add a '{req.RequiredComponentTypeName}' component to the GameObject.",
                                    fixDescription: $"Add component '{req.RequiredComponentTypeName}'",
                                    fixAction: () =>
                                    {
                                        Undo.AddComponent(go, compType);
                                        EditorUtility.SetDirty(go);
                                        return SafeFixResult.Ok($"Added component '{req.RequiredComponentTypeName}' to '{go.name}'.");
                                    },
                                    severity: req.Severity);
                            }
                            else
                            {
                                yield return CreateFinding(
                                    title: $"Missing '{req.RequiredComponentTypeName}' on Tagged '{go.name}'",
                                    explanation: $"GameObject is tagged '{displayTag}' which requires component '{req.RequiredComponentTypeName}'.",
                                    targetGameObject: go,
                                    targetComponent: null,
                                    reason: $"Tag-Component constraint '{displayTag} -> {req.RequiredComponentTypeName}' is violated.",
                                    suggestedCorrection: $"Add a '{req.RequiredComponentTypeName}' component to the GameObject.",
                                    severity: req.Severity);
                            }
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

        private static Type ResolveComponentType(string typeName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = assembly.GetType(typeName, false, true);
                if (t != null && typeof(Component).IsAssignableFrom(t)) return t;

                // Also check without namespace
                foreach (var exportedType in assembly.GetExportedTypes())
                {
                    if (string.Equals(exportedType.Name, typeName, StringComparison.OrdinalIgnoreCase) &&
                        typeof(Component).IsAssignableFrom(exportedType))
                    {
                        return exportedType;
                    }
                }
            }
            return null;
        }
    }
}
