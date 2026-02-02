namespace MiniWord.Core.Exceptions;

/// <summary>
/// Exception thrown when document operations fail
/// </summary>
public class DocumentException : Exception
{
    public DocumentException(string message) : base(message)
    {
    }

    public DocumentException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
