using Clt.Api.Api.Errors;
using Clt.Api.Application.Common;
using Clt.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Application.Users.Commands;

public sealed record UpdateUserCommand(string Name, string Email, bool? IsActive = null);

public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
{
    public UpdateUserValidator()
    {
        RuleFor(request => request.Name).NotEmpty();
        RuleFor(request => request.Email).NotEmpty().EmailAddress();
    }
}

public sealed class UpdateUserHandler(IDbContextFactory<AppDbContext> factory,
    IValidator<UpdateUserCommand> validator)
{
    public async Task<UserResponse> HandleAsync(int id, UpdateUserCommand command, CancellationToken ct)
    {
        await validator.EnsureValidAsync(command, ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = await db.Users.SingleOrDefaultAsync(user => user.Id == id, ct)
            ?? throw ApiException.NotFound("User");
        user.Name = command.Name.Trim();
        user.Email = command.Email.Trim().ToLowerInvariant();
        if (command.IsActive.HasValue) user.IsActive = command.IsActive.Value;
        await DatabaseErrors.SaveAsync(db, "A user with this email already exists.", ct);
        return UserResponse.From(user);
    }
}
