namespace todo;

/// <summary>
/// An error that is the user's to fix (bad list name, missing configuration, ...).
/// These are reported as a plain message rather than a stack trace.
/// </summary>
public class TodoCliException : Exception
{
    public TodoCliException(string message) : base(message) { }
}
