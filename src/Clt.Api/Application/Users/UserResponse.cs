using Clt.Api.Domain.Entities;

namespace Clt.Api.Application.Users;

public sealed record UserResponse(int Id, string Name, string Email, bool IsActive)
{
    public static UserResponse From(User user) => new(user.Id, user.Name, user.Email, user.IsActive);
}
