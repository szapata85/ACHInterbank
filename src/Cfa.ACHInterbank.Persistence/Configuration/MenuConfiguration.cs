using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cfa.ACHInterbank.Persistence.Configuration;

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public const int MainMenuId = 1;

    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("Menus");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Description)
            .HasMaxLength(400);

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Menu)
            .HasForeignKey(x => x.MenuId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasData(new Menu
        {
            Id = MainMenuId,
            Name = "Principal",
            Description = "Menú principal",
            IsActive = true
        });
    }
}
