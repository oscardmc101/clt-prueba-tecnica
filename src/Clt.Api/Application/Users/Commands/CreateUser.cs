using Clt.Api.Application.Common;
using Clt.Api.Domain.Entities;
using Clt.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Application.Users.Commands;

public sealed record CreateUserCommand(string Name, string Email, string? Password = null);

public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserValidator()
    {
        RuleFor(request => request.Name).NotEmpty();
        RuleFor(request => request.Email).NotEmpty().EmailAddress();
        RuleFor(request => request.Password).NotEmpty().When(request => request.Password is not null);
    }
}

public sealed class CreateUserHandler(IDbContextFactory<AppDbContext> factory,
    IValidator<CreateUserCommand> validator, IPasswordHasher<User> passwordHasher)
{
    public async Task<UserResponse> HandleAsync(CreateUserCommand command, CancellationToken ct)
    {
        await validator.EnsureValidAsync(command, ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var user = new User { Name = command.Name.Trim(), Email = command.Email.Trim().ToLowerInvariant() };
        if (command.Password is not null)
            user.PasswordHash = passwordHasher.HashPassword(user, command.Password);
        db.Users.Add(user);
        await DatabaseErrors.SaveAsync(db, "A user with this email already exists.", ct);
        return UserResponse.From(user);
    }
}
