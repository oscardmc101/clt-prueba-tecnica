using System.Security.Cryptography;
using System.Text;

namespace Clt.Api.Api.Middleware;

public sealed class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly byte[] _expectedHash;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        var key = configuration["ApiKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("ApiKey configuration is required.");
        _expectedHash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-API-KEY", out var supplied) || supplied.Count != 1 ||
            !CryptographicOperations.FixedTimeEquals(_expectedHash,
                SHA256.HashData(Encoding.UTF8.GetBytes(supplied.ToString()))))
        {
            await Results.Problem(statusCode: 401, detail: "A valid X-API-KEY header is required.")
                .ExecuteAsync(context);
            return;
        }
        await _next(context);
    }
}
