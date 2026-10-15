using Clt.Api.Api.Errors;
using Clt.Api.Application.Common;
using Clt.Api.Infrastructure.Persistence;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Clt.Api.Application.Addresses.Commands;

public sealed record UpdateAddressCommand(string Street, string City, string Country, string? ZipCode = null);

public sealed class UpdateAddressValidator : AbstractValidator<UpdateAddressCommand>
{
    public UpdateAddressValidator()
    {
        RuleFor(request => request.Street).NotEmpty();
        RuleFor(request => request.City).NotEmpty();
        RuleFor(request => request.Country).NotEmpty();
    }
}

public sealed class UpdateAddressHandler(IDbContextFactory<AppDbContext> factory,
    IValidator<UpdateAddressCommand> validator)
{
    public async Task<AddressResponse> HandleAsync(int id, UpdateAddressCommand command, CancellationToken ct)
    {
        await validator.EnsureValidAsync(command, ct);
        await using var db = await factory.CreateDbContextAsync(ct);
        var address = await db.Addresses.SingleOrDefaultAsync(address => address.Id == id, ct)
            ?? throw ApiException.NotFound("Address");
        address.Street = command.Street.Trim();
        address.City = command.City.Trim();
        address.Country = command.Country.Trim();
        address.ZipCode = command.ZipCode?.Trim();
        await db.SaveChangesAsync(ct);
        return AddressResponse.From(address);
    }
}
