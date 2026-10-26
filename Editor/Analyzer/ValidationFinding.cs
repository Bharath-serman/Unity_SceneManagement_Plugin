using System;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Represents a specific issue or observation produced by a SceneValidationRule.
    /// Contains rich contextual information, severity, and safe remediation hooks.
    /// </summary>
    public class ValidationFinding
    {
        public string RuleId { get; }
        public string RuleName { get; }
        public string Title { get; }
        public string Explanation { get; }
        public GameObject TargetGameObject { get; }
        public Component TargetComponent { get; }
        public string Reason { get; }
        public string SuggestedCorrection { get; }
        public ValidationSeverity Severity { get; }
        public bool IsFixable => FixAction != null;
        public string FixDescription { get; }
        public Func<SafeFixResult> FixAction { get; }

        public ValidationFinding(
            string ruleId,
            string ruleName,
            string title,
            string explanation,
            GameObject targetGameObject,
            Component targetComponent,
            string reason,
            string suggestedCorrection,
            ValidationSeverity severity,
            string fixDescription = null,
            Func<SafeFixResult> fixAction = null)
        {
            RuleId = ruleId ?? string.Empty;
            RuleName = ruleName ?? string.Empty;
            Title = title ?? string.Empty;
            Explanation = explanation ?? string.Empty;
            TargetGameObject = targetGameObject;
            TargetComponent = targetComponent;
            Reason = reason ?? string.Empty;
            SuggestedCorrection = suggestedCorrection ?? string.Empty;
            Severity = severity;
            FixDescription = fixDescription ?? string.Empty;
            FixAction = fixAction;
        }
    }
}
