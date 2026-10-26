namespace SceneSentry.Editor
{
    /// <summary>
    /// Outcome of an automated safe fix operation.
    /// </summary>
    public class SafeFixResult
    {
        public bool Success { get; }
        public string Message { get; }
        public int ObjectsModifiedCount { get; }

        public SafeFixResult(bool success, string message, int objectsModifiedCount = 1)
        {
            Success = success;
            Message = message ?? string.Empty;
            ObjectsModifiedCount = objectsModifiedCount;
        }

        public static SafeFixResult Ok(string message, int count = 1)
        {
            return new SafeFixResult(true, message, count);
        }

        public static SafeFixResult Fail(string reason)
        {
            return new SafeFixResult(false, reason, 0);
        }
    }
}
