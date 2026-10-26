using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Inspects serialized fields on active components to identify broken object references.
    /// Does not flag intentionally empty/null fields; only flags references where an instance ID
    /// was previously recorded but the target object is missing.
    /// </summary>
    public class MissingObjectReferencesRule : SceneValidationRuleBase
    {
        public override string RuleId => "built-in.missing-references";
        public override string RuleName => "Missing Serialized References";
        public override string Description => "Detects serialized object references that point to destroyed or missing assets/objects.";
        public override ValidationCategory Category => ValidationCategory.MissingReferences;
        public override ValidationSeverity DefaultSeverity => ValidationSeverity.Warning;

        public override IEnumerable<ValidationFinding> Analyze(SceneValidationContext context)
        {
            var objects = context.AllGameObjects;
            for (int i = 0; i < objects.Count; i++)
            {
                var go = objects[i];
                if (go == null) continue;

                var components = go.GetComponents<Component>();
                for (int c = 0; c < components.Length; c++)
                {
                    var comp = components[c];
                    if (comp == null) continue;

                    var so = new SerializedObject(comp);
                    var prop = so.GetIterator();

                    while (prop.NextVisible(true))
                    {
                        if (prop.propertyType == SerializedPropertyType.ObjectReference)
                        {
                            // Missing reference check: instanceID is non-zero, but objectReferenceValue is null
                            if (prop.objectReferenceInstanceIDValue != 0 && prop.objectReferenceValue == null)
                            {
                                string propName = prop.displayName;
                                yield return CreateFinding(
                                    title: $"Missing Reference: {propName} on '{go.name}'",
                                    explanation: $"Field '{propName}' on component '{comp.GetType().Name}' points to an object that was deleted or could not be loaded.",
                                    targetGameObject: go,
                                    targetComponent: comp,
                                    reason: $"Serialized property '{prop.propertyPath}' has instance ID {prop.objectReferenceInstanceIDValue} but resolved to null.",
                                    suggestedCorrection: "Inspect the component in the Inspector and assign a valid reference or clear the field.");
                            }
                        }
                    }
                }
            }
        }
    }
}
