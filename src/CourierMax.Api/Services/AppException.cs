namespace CourierMax.Api.Services;

public sealed class AppException : Exception
{
    public AppException(int statusCode, string message, string? detail = null)
        : base(message)
    {
        StatusCode = statusCode;
        Detail = detail;
    }

    public int StatusCode { get; }
    public string? Detail { get; }
}
