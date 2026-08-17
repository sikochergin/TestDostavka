using System.Net;

public sealed class YooKassaApiException : Exception
{
    public YooKassaApiException(
        HttpStatusCode statusCode,
        string? errorCode,
        string message,
        string? parameter = null,
        string? responseBody = null)
        : base(message)
    {
        StatusCode = statusCode;
        ErrorCode = errorCode;
        Parameter = parameter;
        ResponseBody = responseBody;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ErrorCode { get; }

    public string? Parameter { get; }

    public string? ResponseBody { get; }
}