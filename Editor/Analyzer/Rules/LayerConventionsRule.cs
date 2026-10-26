using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Configurable rule ensuring GameObjects with specific custom tags are assigned to appropriate layers.
    /// Example: Objects tagged 'PostProcessing' must be on the 'PostProcessing' layer.
    /// </summary>
    public class LayerConventionsRule : SceneValidationRuleBase
    {
        public override string RuleId => "built-in.layer-conventions";
        public override string RuleName => "Layer Conventions";
        public override string Description => "Enforces that tagged GameObjects reside on their configured Layer.";
        public override ValidationCategory Category => ValidationCategory.NamingOrganization;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Warning;

        public override IEnumerable<ValidationFinding> Analyze(SceneValidationContext context)
        {
            var config = context.RuleConfig;
            var rules = config?.LayerRequirements;
            if (rules == null || rules.Count == 0) yield break;

            var objects = context.AllGameObjects;

            for (int r = 0; r < rules.Count; r++)
            {
                var req = rules[r];
                if (!req.IsEnabled) continue;
                if (string.IsNullOrWhiteSpace(req.TagId) && string.IsNullOrWhiteSpace(req.TagName)) continue;

                for (int i = 0; i < objects.Count; i++)
                {
                    var go = objects[i];
                    if (go == null) continue;

                    bool hasTag = false;
                    if (go.TryGetComponent<SceneSentryTags>(out var tagsComp))
                    {
                        if (!string.IsNullOrEmpty(req.TagId) && tagsComp.HasTagId(req.TagId)) hasTag = true;
                        else if (!string.IsNullOrEmpty(req.TagName) && tagsComp.HasTagId(req.TagName)) hasTag = true;
                    }

                    if (!hasTag && !string.IsNullOrEmpty(req.TagName) && string.Equals(go.tag, req.TagName, StringComparison.OrdinalIgnoreCase))
                    {
                        hasTag = true;
                    }

                    if (hasTag)
                    {
                        if (go.layer != req.ExpectedLayer)
                        {
                            string tagName = !string.IsNullOrEmpty(req.TagName) ? req.TagName : req.TagId;
                            string expectedLayerName = LayerMask.LayerToName(req.ExpectedLayer);
                            if (string.IsNullOrEmpty(expectedLayerName)) expectedLayerName = $"Layer {req.ExpectedLayer}";

                            string currentLayerName = LayerMask.LayerToName(go.layer);
                            if (string.IsNullOrEmpty(currentLayerName)) currentLayerName = $"Layer {go.layer}";

                            yield return CreateFixableFinding(
                                title: $"Wrong Layer on '{go.name}' (Tagged '{tagName}')",
                                explanation: $"GameObject has tag '{tagName}' and is on layer '{currentLayerName}', but should be on layer '{expectedLayerName}'.",
                                targetGameObject: go,
                                targetComponent: null,
                                reason: $"Layer mismatch for tag '{tagName}'. Expected {expectedLayerName}, got {currentLayerName}.",
                                suggestedCorrection: $"Move '{go.name}' to layer '{expectedLayerName}'.",
                                fixDescription: $"Change layer to '{expectedLayerName}'",
                                fixAction: () =>
                                {
                                    Undo.RecordObject(go, $"Assign Layer '{expectedLayerName}'");
                                    go.layer = req.ExpectedLayer;
                                    EditorUtility.SetDirty(go);
                                    return SafeFixResult.Ok($"Moved '{go.name}' to layer '{expectedLayerName}'.");
                                },
                                severity: req.Severity);
                        }
                    }
                }
            }
        }
    }
}
