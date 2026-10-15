using Clt.Api.Api.Errors;
using Clt.Api.Application.Common;
using FluentValidation;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Application.Users.Commands;

public sealed record BulkCreateUsersCommand(IReadOnlyList<CreateUserCommand?>? Users);
public sealed record BulkItemError(int Status, string Message, Dictionary<string, string[]>? Errors = null);
public sealed record BulkUserResult(int Index, string? Email, bool Success, UserResponse? User, BulkItemError? Error);
public sealed record BulkUsersResponse(int Total, int CreatedCount, int FailedCount, IReadOnlyList<BulkUserResult> Results);

public sealed class BulkCreateUsersValidator : AbstractValidator<BulkCreateUsersCommand>
{
    public BulkCreateUsersValidator() => RuleFor(request => request.Users).NotEmpty();
}

public sealed class BulkCreateUsersHandler(CreateUserHandler createUser,
    IValidator<BulkCreateUsersCommand> validator, ILogger<BulkCreateUsersHandler> logger)
{
    public async Task<BulkUsersResponse> HandleAsync(BulkCreateUsersCommand command, CancellationToken ct)
    {
        await validator.EnsureValidAsync(command, ct);
        using var concurrency = new SemaphoreSlim(4);
        var tasks = command.Users!.Select((user, index) => ProcessAsync(user, index)).ToArray();
        var results = await Task.WhenAll(tasks);
        var created = results.Count(result => result.Success);
        return new BulkUsersResponse(results.Length, created, results.Length - created, results);

        async Task<BulkUserResult> ProcessAsync(CreateUserCommand? user, int index)
        {
            await concurrency.WaitAsync(ct);
            try
            {
                // SQLite's async I/O is synchronous. Run up to four operations on workers.
                // Each CreateUserHandler invocation creates AND disposes its own AppDbContext
                // through IDbContextFactory<AppDbContext>; no context/connection is shared.
                return await Task.Run(async () =>
                {
                    try
                    {
                        if (user is null)
                            return Failure(400, "User cannot be null.");
                        var createdUser = await createUser.HandleAsync(user, ct);
                        return new BulkUserResult(index, user.Email, true, createdUser, null);
                    }
                    catch (ValidationException exception)
                    {
                        var errors = exception.Errors.GroupBy(error => error.PropertyName)
                            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());
                        return Failure(400, "User validation failed.", errors);
                    }
                    catch (ApiException exception)
                    {
                        return Failure(exception.StatusCode, exception.Message);
                    }
                    catch (Exception exception) when (exception is SqliteException { SqliteErrorCode: 5 or 6 } ||
                        exception is DbUpdateException { InnerException: SqliteException { SqliteErrorCode: 5 or 6 } })
                    {
                        return Failure(503, "The database is busy. Please retry this item later.");
                    }
                    catch (OperationCanceledException) when (ct.IsCancellationRequested)
                    {
                        throw;
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Bulk user creation failed at index {Index}", index);
                        return Failure(500, "An unexpected error occurred while creating this user.");
                    }
                }, ct);
            }
            finally
            {
                concurrency.Release();
            }

            BulkUserResult Failure(int status, string message, Dictionary<string, string[]>? errors = null) =>
                new(index, user?.Email, false, null, new BulkItemError(status, message, errors));
        }
    }
}
