using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Api.Errors;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        IResult result;
        if (exception is ValidationException validation)
        {
            result = Results.ValidationProblem(validation.Errors
                .GroupBy(error => error.PropertyName)
                .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray()));
        }
        else if (exception is ApiException api)
        {
            result = Results.Problem(statusCode: api.StatusCode, detail: api.Message);
        }
        else if (exception is SqliteException { SqliteErrorCode: 5 or 6 } ||
                 exception is DbUpdateException { InnerException: SqliteException { SqliteErrorCode: 5 or 6 } })
        {
            result = Results.Problem(statusCode: 503, detail: "The database is busy. Please retry later.");
        }
        else if (exception is BadHttpRequestException badRequest)
        {
            result = Results.Problem(statusCode: badRequest.StatusCode, detail: "Invalid request.");
        }
        else
        {
            logger.LogError(exception, "Unhandled API error. TraceId: {TraceId}", context.TraceIdentifier);
            result = Results.Problem(statusCode: 500, detail: "An unexpected error occurred.");
        }
        await result.ExecuteAsync(context);
        return true;
    }
}
