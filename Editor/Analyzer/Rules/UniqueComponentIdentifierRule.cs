using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Validates uniqueness of identifiers on components implementing IIdentifiableObject.
    /// Detects duplicate IDs across loaded scenes to prevent runtime conflicts.
    /// </summary>
    public class UniqueComponentIdentifierRule : SceneValidationRuleBase
    {
        public override string RuleId => "built-in.unique-identifiers";
        public override string RuleName => "Unique Component Identifiers";
        public override string Description => "Detects duplicate identifiers across components implementing the IIdentifiableObject interface.";
        public override ValidationCategory Category => ValidationCategory.Identifiers;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Error;

        public override IEnumerable<ValidationFinding> Analyze(SceneValidationContext context)
        {
            var objects = context.AllGameObjects;
            var seenIdentifiers = new Dictionary<string, List<(GameObject Go, IIdentifiableObject Identifiable, Component Comp)>>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < objects.Count; i++)
            {
                var go = objects[i];
                if (go == null) continue;

                var components = go.GetComponents<Component>();
                for (int c = 0; c < components.Length; c++)
                {
                    var comp = components[c];
                    if (comp is IIdentifiableObject identifiable)
                    {
                        string id = identifiable.UniqueIdentifier;
                        if (string.IsNullOrWhiteSpace(id)) continue;

                        string cleanId = id.Trim();
                        if (!seenIdentifiers.TryGetValue(cleanId, out var list))
                        {
                            list = new List<(GameObject, IIdentifiableObject, Component)>();
                            seenIdentifiers[cleanId] = list;
                        }
                        list.Add((go, identifiable, comp));
                    }
                }
            }

            foreach (var kvp in seenIdentifiers)
            {
                if (kvp.Value.Count > 1)
                {
                    var duplicates = kvp.Value;
                    for (int d = 1; d < duplicates.Count; d++)
                    {
                        var dup = duplicates[d];
                        string id = kvp.Key;
                        var targetComp = dup.Comp;
                        var targetGo = dup.Go;
                        var identifiable = dup.Identifiable;

                        yield return CreateFixableFinding(
                            title: $"Duplicate Identifier '{id}' on '{targetGo.name}'",
                            explanation: $"Component '{targetComp.GetType().Name}' on '{targetGo.name}' has the same identifier as '{duplicates[0].Go.name}'.",
                            targetGameObject: targetGo,
                            targetComponent: targetComp,
                            reason: $"Identifier '{id}' is registered by multiple components.",
                            suggestedCorrection: "Click 'Fix' to regenerate a new unique identifier or assign one manually in the Inspector.",
                            fixDescription: "Regenerate unique identifier",
                            fixAction: () =>
                            {
                                Undo.RecordObject(targetComp, "Regenerate Unique Identifier");
                                bool success = identifiable.TryRegenerateIdentifier();
                                if (success)
                                {
                                    EditorUtility.SetDirty(targetComp);
                                    return SafeFixResult.Ok($"Regenerated identifier for '{targetGo.name}'.");
                                }
                                return SafeFixResult.Fail($"Failed to automatically regenerate identifier on '{targetComp.GetType().Name}'.");
                            });
                    }
                }
            }
        }
    }
}
