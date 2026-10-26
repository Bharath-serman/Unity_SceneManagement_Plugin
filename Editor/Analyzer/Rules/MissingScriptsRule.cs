using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Detects GameObjects with missing MonoBehaviour scripts (unlinked / deleted script assets).
    /// Safe fix removes the dangling missing script components.
    /// </summary>
    public class MissingScriptsRule : SceneValidationRuleBase
    {
        public override string RuleId => "built-in.missing-scripts";
        public override string RuleName => "Missing Script Components";
        public override string Description => "Identifies GameObjects containing missing MonoBehaviour scripts.";
        public override ValidationCategory Category => ValidationCategory.MissingComponents;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Error;

        public override IEnumerable<ValidationFinding> Analyze(SceneValidationContext context)
        {
            var objects = context.AllGameObjects;
            for (int i = 0; i < objects.Count; i++)
            {
                var go = objects[i];
                if (go == null) continue;

                var components = go.GetComponents<Component>();
                int missingCount = 0;

                for (int c = 0; c < components.Length; c++)
                {
                    if (components[c] == null)
                    {
                        missingCount++;
                    }
                }

                if (missingCount > 0)
                {
                    yield return CreateFixableFinding(
                        title: $"Missing Script on '{go.name}'",
                        explanation: $"GameObject has {missingCount} missing script component(s). This usually happens when a script file was deleted, renamed, or failed to compile.",
                        targetGameObject: go,
                        targetComponent: null,
                        reason: $"Detected {missingCount} null component reference(s) on GameObject.",
                        suggestedCorrection: "Reattach the missing script or click 'Fix' to safely remove the dangling missing component(s).",
                        fixDescription: $"Remove {missingCount} missing MonoBehaviour script(s)",
                        fixAction: () =>
                        {
                            Undo.RegisterCompleteObjectUndo(go, "Remove Missing Scripts");
                            int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
                            EditorUtility.SetDirty(go);
                            return SafeFixResult.Ok($"Removed {removed} missing script(s) from '{go.name}'.");
                        });
                }
            }
        }
    }
}
