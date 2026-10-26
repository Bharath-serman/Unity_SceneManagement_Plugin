namespace SceneSentry
{
    /// <summary>
    /// Optional interface for components that require a unique string identifier within a scene.
    /// SceneSentry's Scene Analyzer detects duplicate identifiers across components implementing this interface.
    /// </summary>
    public interface IIdentifiableObject
    {
        /// <summary>
        /// The unique identifier of this object or component.
        /// </summary>
        string UniqueIdentifier { get; }

        /// <summary>
        /// Optional method to regenerate a fresh unique identifier if supported.
        /// Returns true if a new ID was successfully assigned.
        /// </summary>
        bool TryRegenerateIdentifier();
    }
}
