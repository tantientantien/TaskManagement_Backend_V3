namespace Backend.Common;

public class ClerkApiException : Exception
{
    public int StatusCode { get; }
    public ClerkApiException(string message, int statusCode) : base(message)
    {
        StatusCode = statusCode;
    }
}