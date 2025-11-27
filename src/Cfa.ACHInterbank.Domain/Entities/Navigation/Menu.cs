namespace Cfa.ACHInterbank.Domain.Entities.Navigation;

public class Menu
{
    public int Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MenuItem> Items { get; set; } = new List<MenuItem>();
}
