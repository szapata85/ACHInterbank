using Cfa.ACHInterbank.Domain.Entities.User;

namespace Cfa.ACHInterbank.Domain.Entities.Navigation;

public class MenuItem
{
    public int Id { get; set; }
    public int? MenuId { get; set; }
    public int? ParentId { get; set; }
    public string Label { get; set; } = default!;
    public string Route { get; set; } = default!;
    public string? Icon { get; set; }
    public int Order { get; set; }
    public bool Exact { get; set; }
    public bool IsActive { get; set; } = true;

    public Menu? Menu { get; set; }
    public MenuItem? Parent { get; set; }
    public ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
    public ICollection<MenuItemRole> MenuItemRoles { get; set; } = new List<MenuItemRole>();
    public ICollection<MenuItemPermission> MenuItemPermissions { get; set; } = new List<MenuItemPermission>();
}
