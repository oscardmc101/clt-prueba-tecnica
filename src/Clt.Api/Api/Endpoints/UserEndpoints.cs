using Clt.Api.Application.Users;
using Clt.Api.Application.Users.Commands;
using Clt.Api.Application.Users.Queries;

namespace Clt.Api.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/users").WithTags("Users");
        group.MapPost("", async (CreateUserCommand request, CreateUserHandler handler, CancellationToken ct) =>
        {
            var user = await handler.HandleAsync(request, ct);
            return Results.Created($"/users/{user.Id}", user);
        }).Produces<UserResponse>(201).ProducesValidationProblem().ProducesProblem(409);
        group.MapGet("", async (bool? isActive, ListUsersHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(new ListUsersQuery(isActive), ct)))
            .Produces<List<UserResponse>>();
        group.MapGet("/{id:int}", async (int id, GetUserHandler handler, CancellationToken ct) =>
            Results.Ok(await handler.HandleAsync(new GetUserQuery(id), ct)))
            .Produces<UserResponse>().ProducesProblem(404);
        group.MapPut("/{id:int}", async (int id, UpdateUserCommand request, UpdateUserHandler handler,
            CancellationToken ct) => Results.Ok(await handler.HandleAsync(id, request, ct)))
            .Produces<UserResponse>().ProducesValidationProblem().ProducesProblem(404).ProducesProblem(409);
        group.MapDelete("/{id:int}", async (int id, DeleteUserHandler handler, CancellationToken ct) =>
        {
            await handler.HandleAsync(new DeleteUserCommand(id), ct);
            return Results.NoContent();
        }).Produces(204).ProducesProblem(404);
    }
}
