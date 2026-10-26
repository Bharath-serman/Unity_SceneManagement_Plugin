using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Enforces that certain components must be paired with other companion components on the same GameObject.
    /// Example: Rigidbody requires Collider.
    /// </summary>
    public class RequiredComponentCombinationsRule : SceneValidationRuleBase
    {
        public override string RuleId => "built-in.component-combinations";
        public override string RuleName => "Component Combinations";
        public override string Description => "Validates that components requiring specific companion components are properly paired.";
        public override ValidationCategory Category => ValidationCategory.MissingComponents;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Warning;

        public override IEnumerable<ValidationFinding> Analyze(SceneValidationContext context)
        {
            var config = context.RuleConfig;
            var pairs = config?.RequiredComponentPairs;
            if (pairs == null || pairs.Count == 0) yield break;

            var objects = context.AllGameObjects;

            for (int p = 0; p < pairs.Count; p++)
            {
                var pair = pairs[p];
                if (!pair.IsEnabled) continue;
                if (string.IsNullOrWhiteSpace(pair.MainComponentTypeName) || string.IsNullOrWhiteSpace(pair.RequiredPairComponentTypeName))
                {
                    continue;
                }

                for (int i = 0; i < objects.Count; i++)
                {
                    var go = objects[i];
                    if (go == null) continue;

                    if (HasComponentNamed(go, pair.MainComponentTypeName))
                    {
                        if (!HasComponentNamed(go, pair.RequiredPairComponentTypeName))
                        {
                            Type companionType = ResolveComponentType(pair.RequiredPairComponentTypeName);
                            bool canAutoAdd = companionType != null && typeof(Component).IsAssignableFrom(companionType);

                            if (canAutoAdd)
                            {
                                yield return CreateFixableFinding(
                                    title: $"'{go.name}' has '{pair.MainComponentTypeName}' but lacks '{pair.RequiredPairComponentTypeName}'",
                                    explanation: $"GameObject contains component '{pair.MainComponentTypeName}' which typically requires a companion '{pair.RequiredPairComponentTypeName}'.",
                                    targetGameObject: go,
                                    targetComponent: null,
                                    reason: $"Missing required companion component '{pair.RequiredPairComponentTypeName}'.",
                                    suggestedCorrection: $"Add a '{pair.RequiredPairComponentTypeName}' to '{go.name}'.",
                                    fixDescription: $"Add '{pair.RequiredPairComponentTypeName}'",
                                    fixAction: () =>
                                    {
                                        Undo.AddComponent(go, companionType);
                                        EditorUtility.SetDirty(go);
                                        return SafeFixResult.Ok($"Added '{pair.RequiredPairComponentTypeName}' to '{go.name}'.");
                                    },
                                    severity: pair.Severity);
                            }
                            else
                            {
                                yield return CreateFinding(
                                    title: $"'{go.name}' has '{pair.MainComponentTypeName}' but lacks '{pair.RequiredPairComponentTypeName}'",
                                    explanation: $"GameObject contains component '{pair.MainComponentTypeName}' which typically requires a companion '{pair.RequiredPairComponentTypeName}'.",
                                    targetGameObject: go,
                                    targetComponent: null,
                                    reason: $"Missing required companion component '{pair.RequiredPairComponentTypeName}'.",
                                    suggestedCorrection: $"Add a '{pair.RequiredPairComponentTypeName}' to '{go.name}'.",
                                    severity: pair.Severity);
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
