using System.Collections.Generic;

namespace SceneSentry.Editor
{
    /// <summary>
    /// Contract for all SceneSentry scene validation rules.
    /// Custom rules authored by developers implement this interface or inherit from SceneValidationRuleBase.
    /// </summary>
    public interface ISceneValidationRule
    {
        string RuleId { get; }
        string RuleName { get; }
        string Description { get; }
        ValidationCategory Category { get; }
        ValidationSeverity DefaultSeverity { get; }

        bool IsEnabled { get; set; }
        ValidationSeverity ConfiguredSeverity { get; set; }

        IEnumerable<ValidationFinding> Analyze(SceneValidationContext context);
        bool CanFix(ValidationFinding finding);
        SafeFixResult ApplyFix(ValidationFinding finding);
    }
}
