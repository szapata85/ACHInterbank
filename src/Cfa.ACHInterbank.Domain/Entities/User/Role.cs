using System.Collections.ObjectModel;

namespace Cfa.ACHInterbank.Domain.Entities.User;

public class Role
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new Collection<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new Collection<RolePermission>();
}
