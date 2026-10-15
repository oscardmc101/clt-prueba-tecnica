using Clt.Api.Api.Errors;
using Clt.Api.Application.Common;
using Clt.Api.Domain.Entities;
using Clt.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Application.Addresses.Commands;

public sealed record CreateAddressCommand(string Street, string City, string Country, string? ZipCode = null);

public sealed class CreateAddressValidator : AbstractValidator<CreateAddressCommand>
{
    public CreateAddressValidator()
    {
        RuleFor(request => request.Street).NotEmpty();
        RuleFor(request => request.City).NotEmpty();
        RuleFor(request => request.Country).NotEmpty();
    }
}

public sealed class CreateAddressHandler(IDbContextFactory<AppDbContext> factory,
    IValidator<CreateAddressCommand> validator)
{
    public async Task<AddressResponse> HandleAsync(int userId, CreateAddressCommand command, CancellationToken ct)
    {
        await validator.EnsureValidAsync(command, ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        if (!await db.Users.AnyAsync(user => user.Id == userId, ct)) throw ApiException.NotFound("User");
        var address = new Address
        {
            UserId = userId, Street = command.Street.Trim(), City = command.City.Trim(),
            Country = command.Country.Trim(), ZipCode = command.ZipCode?.Trim()
        };
        db.Addresses.Add(address);
        await db.SaveChangesAsync(ct);
        return AddressResponse.From(address);
    }
}
