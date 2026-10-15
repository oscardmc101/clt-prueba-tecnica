namespace Clt.Api.Domain.Entities;

public sealed class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string? PasswordHash { get; set; }
    public ICollection<Address> Addresses { get; set; } = new List<Address>();
}
