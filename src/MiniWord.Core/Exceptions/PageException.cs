namespace MiniWord.Core.Exceptions;

/// <summary>
/// Exception thrown when page operations fail
/// </summary>
public class PageException : DocumentException
{
    public PageException(string message) : base(message)
    {
    }

    public PageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
