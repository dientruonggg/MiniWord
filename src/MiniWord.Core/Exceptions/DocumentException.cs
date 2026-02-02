namespace MiniWord.Core.Exceptions;

/// <summary>
/// Base exception for all document-related errors
/// Provides structured error information with error codes
/// </summary>
public class DocumentException : Exception
{
    /// <summary>
    /// Error code for categorizing the exception
    /// </summary>
    public string ErrorCode { get; }

    public DocumentException(string message) : base(message)
    {
        ErrorCode = "DOCUMENT_ERROR";
    }

    public DocumentException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public DocumentException(string message, Exception innerException) : base(message, innerException)
    {
        ErrorCode = "DOCUMENT_ERROR";
    }

    public DocumentException(string message, string errorCode, Exception innerException) : base(message, innerException)
    {
        ErrorCode = errorCode;
    }
}
