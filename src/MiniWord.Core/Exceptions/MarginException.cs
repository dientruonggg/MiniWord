namespace MiniWord.Core.Exceptions;

/// <summary>
/// Exception thrown when margin validation fails
/// </summary>
public class MarginException : DocumentException
{
    public MarginException(string message) : base(message)
    {
    }

    public MarginException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
