using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Identifies GameObjects holding custom tag IDs that no longer exist in the TagDatabase.
    /// Typically occurs when a tag was deleted from the TagDatabase.
    /// </summary>
    public class InvalidTagReferencesRule : SceneValidationRuleBase
    {
        public override string RuleId => "built-in.invalid-tags";
        public override string RuleName => "Invalid Tag References";
        public override string Description => "Detects GameObjects holding tag IDs that no longer exist in the project TagDatabase.";
        public override ValidationCategory Category => ValidationCategory.Tagging;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Warning;

        public override IEnumerable<ValidationFinding> Analyze(SceneValidationContext context)
        {
            var db = context.TagDatabase;
            if (db == null) yield break;

            var objects = context.AllGameObjects;
            for (int i = 0; i < objects.Count; i++)
            {
                var go = objects[i];
                if (go == null) continue;

                if (go.TryGetComponent<SceneSentryTags>(out var tagsComponent))
                {
                    var tagIds = tagsComponent.TagIds;
                    int invalidCount = 0;

                    for (int t = 0; t < tagIds.Count; t++)
                    {
                        if (!db.ContainsId(tagIds[t]))
                        {
                            invalidCount++;
                        }
                    }

                    if (invalidCount > 0)
                    {
                        yield return CreateFixableFinding(
                            title: $"Invalid Tag IDs on '{go.name}'",
                            explanation: $"GameObject references {invalidCount} tag ID(s) that are missing from the TagDatabase.",
                            targetGameObject: go,
                            targetComponent: tagsComponent,
                            reason: "The referenced tag definitions were likely deleted from the TagDatabase.",
                            suggestedCorrection: "Click 'Fix' to purge orphaned tag IDs from the object.",
                            fixDescription: "Purge orphaned tag IDs",
                            fixAction: () =>
                            {
                                Undo.RecordObject(tagsComponent, "Purge Invalid Tag IDs");
                                int count = tagsComponent.CleanInvalidTags(db);
                                EditorUtility.SetDirty(tagsComponent);
                                return SafeFixResult.Ok($"Purged {count} orphaned tag(s) on '{go.name}'.");
                            });
                    }
                }
            }
        }
    }
}
