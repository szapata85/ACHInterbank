namespace Cfa.ACHInterbank.Application.Navigation;

public class MenuItemDto
{
    public int Id { get; set; }
    public string Label { get; set; } = default!;
    public string Route { get; set; } = default!;
    public string? Icon { get; set; }
    public bool Exact { get; set; }
    public int Order { get; set; }
    public IList<MenuItemDto> Children { get; set; } = new List<MenuItemDto>();
}
