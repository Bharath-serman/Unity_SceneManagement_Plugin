namespace SceneSentry.Editor
{
    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    public enum ValidationCategory
    {
        MissingComponents,
        MissingReferences,
        Tagging,
        NamingOrganization,
        Identifiers,
        Custom
    }
}
