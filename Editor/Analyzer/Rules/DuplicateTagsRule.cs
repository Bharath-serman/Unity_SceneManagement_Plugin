using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Identifies GameObjects with duplicate SceneSentry custom tag assignments.
    /// Safe fix automatically removes duplicate tag references.
    /// </summary>
    public class DuplicateTagsRule : SceneValidationRuleBase
    {
        public override string RuleId => "built-in.duplicate-tags";
        public override string RuleName => "Duplicate Tag Assignments";
        public override string Description => "Detects GameObjects that have been assigned the same custom tag multiple times.";
        public override ValidationCategory Category => ValidationCategory.Tagging;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Warning;

        public override IEnumerable<ValidationFinding> Analyze(SceneValidationContext context)
        {
            var objects = context.AllGameObjects;
            for (int i = 0; i < objects.Count; i++)
            {
                var go = objects[i];
                if (go == null) continue;

                if (go.TryGetComponent<SceneSentryTags>(out var tagsComponent))
                {
                    var tagIds = tagsComponent.TagIds;
                    var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    int duplicates = 0;

                    for (int t = 0; t < tagIds.Count; t++)
                    {
                        if (!seen.Add(tagIds[t]))
                        {
                            duplicates++;
                        }
                    }

                    if (duplicates > 0)
                    {
                        yield return CreateFixableFinding(
                            title: $"Duplicate Tags on '{go.name}'",
                            explanation: $"GameObject has {duplicates} duplicate custom tag assignment(s).",
                            targetGameObject: go,
                            targetComponent: tagsComponent,
                            reason: "The SceneSentryTags component contains redundant identical tag IDs.",
                            suggestedCorrection: "Click 'Fix' to automatically strip duplicate tag IDs from the component.",
                            fixDescription: "Deduplicate custom tags",
                            fixAction: () =>
                            {
                                Undo.RecordObject(tagsComponent, "Deduplicate SceneSentry Tags");
                                int count = tagsComponent.RemoveDuplicates();
                                EditorUtility.SetDirty(tagsComponent);
                                return SafeFixResult.Ok($"Removed {count} duplicate tag(s) on '{go.name}'.");
                            });
                    }
                }
            }
        }
    }
}
