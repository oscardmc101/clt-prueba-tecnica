using Clt.Api.Application.Addresses;
using Clt.Api.Application.Addresses.Commands;
using Clt.Api.Application.Addresses.Queries;

namespace Clt.Api.Api.Endpoints;

public static class AddressEndpoints
{
    public static void MapAddressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("").WithTags("Addresses");
        group.MapPost("/users/{userId:int}/addresses", async (int userId, CreateAddressCommand request,
            CreateAddressHandler handler, CancellationToken ct) =>
        {
            var address = await handler.HandleAsync(userId, request, ct);
            return Results.Created($"/users/{userId}/addresses", address);
        }).Produces<AddressResponse>(201).ProducesValidationProblem().ProducesProblem(404);
        group.MapGet("/users/{userId:int}/addresses", async (int userId, ListAddressesHandler handler,
            CancellationToken ct) => Results.Ok(await handler.HandleAsync(new ListAddressesQuery(userId), ct)))
            .Produces<List<AddressResponse>>().ProducesProblem(404);
        group.MapPut("/addresses/{id:int}", async (int id, UpdateAddressCommand request,
            UpdateAddressHandler handler, CancellationToken ct) => Results.Ok(await handler.HandleAsync(id, request, ct)))
            .Produces<AddressResponse>().ProducesValidationProblem().ProducesProblem(404);
        group.MapDelete("/addresses/{id:int}", async (int id, DeleteAddressHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new DeleteAddressCommand(id), ct);
            return Results.NoContent();
        }).Produces(204).ProducesProblem(404);
    }
}
