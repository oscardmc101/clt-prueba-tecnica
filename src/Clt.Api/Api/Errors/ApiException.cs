namespace Clt.Api.Api.Errors;

public sealed class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    public static ApiException NotFound(string resource) => new(404, $"{resource} not found.");
    public static ApiException Conflict(string message) => new(409, message);
}
