using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Validates GameObject naming conventions:
    /// - Flags empty or whitespace-only names.
    /// - Flags duplicate sibling names sharing the same parent.
    /// </summary>
    public class NamingConventionsRule : SceneValidationRuleBase
    {
        public override string RuleId => "built-in.naming-conventions";
        public override string RuleName => "Naming Conventions";
        public override string Description => "Checks for empty names, whitespace, or duplicate names among direct siblings.";
        public override ValidationCategory Category => ValidationCategory.NamingOrganization;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Warning;

        public override IEnumerable<ValidationFinding> Analyze(SceneValidationContext context)
        {
            var config = context.RuleConfig;
            bool checkEmpty = config != null ? config.CheckEmptyNames : true;
            bool checkDuplicates = config != null ? config.CheckSiblingDuplicateNames : true;

            var objects = context.AllGameObjects;
            // Group by parent Transform instance ID (0 = scene root / null parent)
            var siblingBuckets = new Dictionary<int, (Transform parentTransform, Dictionary<string, List<GameObject>> nameDict)>();

            for (int i = 0; i < objects.Count; i++)
            {
                var go = objects[i];
                if (go == null) continue;

                // 1. Empty or whitespace check
                if (checkEmpty && string.IsNullOrWhiteSpace(go.name))
                {
                    yield return CreateFixableFinding(
                        title: "Empty or Whitespace-only Name on GameObject",
                        explanation: "The GameObject has an empty name or contains only whitespace characters.",
                        targetGameObject: go,
                        targetComponent: null,
                        reason: "GameObject name is empty.",
                        suggestedCorrection: "Assign a meaningful name to the GameObject.",
                        fixDescription: "Rename to 'GameObject'",
                        fixAction: () =>
                        {
                            Undo.RecordObject(go, "Fix Empty GameObject Name");
                            go.name = "GameObject";
                            EditorUtility.SetDirty(go);
                            return SafeFixResult.Ok("Renamed empty GameObject to 'GameObject'.");
                        });
                }

                // 2. Sibling duplicate check
                if (checkDuplicates)
                {
                    var parent = go.transform.parent;
                    int parentId = parent != null ? parent.GetInstanceID() : 0;

                    if (!siblingBuckets.TryGetValue(parentId, out var bucket))
                    {
                        bucket = (parent, new Dictionary<string, List<GameObject>>(StringComparer.OrdinalIgnoreCase));
                        siblingBuckets[parentId] = bucket;
                    }

                    string rawName = go.name ?? string.Empty;
                    string key = rawName.Trim();
                    if (string.IsNullOrEmpty(key))
                    {
                        key = "[Empty]";
                    }

                    if (!bucket.nameDict.TryGetValue(key, out var list))
                    {
                        list = new List<GameObject>();
                        bucket.nameDict[key] = list;
                    }
                    list.Add(go);
                }
            }

            if (checkDuplicates)
            {
                foreach (var parentKvp in siblingBuckets)
                {
                    var parentTransform = parentKvp.Value.parentTransform;
                    var nameDict = parentKvp.Value.nameDict;

                    foreach (var nameKvp in nameDict)
                    {
                        if (nameKvp.Value.Count > 1)
                        {
                            var duplicates = nameKvp.Value;
                            for (int d = 1; d < duplicates.Count; d++)
                            {
                                var dupGo = duplicates[d];
                                if (dupGo == null) continue;

                                string originalName = dupGo.name ?? "GameObject";
                                int index = d + 1;
                                string suggestedName = $"{originalName}_{index}";
                                string parentDisplayName = parentTransform != null ? parentTransform.name : "Scene Root";

                                yield return CreateFixableFinding(
                                    title: $"Duplicate Sibling Name '{originalName}'",
                                    explanation: $"GameObject shares the exact same name with its sibling under parent '{parentDisplayName}'.",
                                    targetGameObject: dupGo,
                                    targetComponent: null,
                                    reason: "Duplicate sibling names can cause ambiguity when querying via GameObject.Find or hierarchy paths.",
                                    suggestedCorrection: $"Rename to '{suggestedName}'.",
                                    fixDescription: $"Rename to '{suggestedName}'",
                                    fixAction: () =>
                                    {
                                        Undo.RecordObject(dupGo, "Disambiguate Sibling Name");
                                        dupGo.name = suggestedName;
                                        EditorUtility.SetDirty(dupGo);
                                        return SafeFixResult.Ok($"Renamed duplicate sibling to '{suggestedName}'.");
                                    },
                                    severity: ValidationSeverity.Info);
                            }
                        }
                    }
                }
            }
        }
    }
}
