using System;
using System.Collections.Generic;
using UnityEngine;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Abstract base class providing common functionality and helper constructors
    /// for SceneSentry validation rules.
    /// </summary>
    public abstract class SceneValidationRuleBase : ISceneValidationRule
    {
        public abstract string RuleId { get; }
        public abstract string RuleName { get; }
        public abstract string Description { get; }
        public abstract ValidationCategory Category { get; }
        public virtual ValidationSeverity DefaultSeverity => ValidationSeverity.Warning;

        public bool IsEnabled { get; set; } = true;
        public ValidationSeverity ConfiguredSeverity { get; set; } = ValidationSeverity.Warning;

        protected SceneValidationRuleBase()
        {
            ConfiguredSeverity = DefaultSeverity;
        }

        public abstract IEnumerable<ValidationFinding> Analyze(SceneValidationContext context);

        public virtual bool CanFix(ValidationFinding finding)
        {
            return finding != null && finding.IsFixable;
        }

        public virtual SafeFixResult ApplyFix(ValidationFinding finding)
        {
            if (finding?.FixAction != null)
            {
                return finding.FixAction.Invoke();
            }
            return SafeFixResult.Fail("This finding does not define an automated fix.");
        }

        protected ValidationFinding CreateFinding(
            string title,
            string explanation,
            GameObject targetGameObject,
            Component targetComponent,
            string reason,
            string suggestedCorrection,
            ValidationSeverity? severity = null)
        {
            return new ValidationFinding(
                RuleId,
                RuleName,
                title,
                explanation,
                targetGameObject,
                targetComponent,
                reason,
                suggestedCorrection,
                severity ?? ConfiguredSeverity,
                fixDescription: null,
                fixAction: null);
        }

        protected ValidationFinding CreateFixableFinding(
            string title,
            string explanation,
            GameObject targetGameObject,
            Component targetComponent,
            string reason,
            string suggestedCorrection,
            string fixDescription,
            Func<SafeFixResult> fixAction,
            ValidationSeverity? severity = null)
        {
            return new ValidationFinding(
                RuleId,
                RuleName,
                title,
                explanation,
                targetGameObject,
                targetComponent,
                reason,
                suggestedCorrection,
                severity ?? ConfiguredSeverity,
                fixDescription,
                fixAction);
        }
    }
}
