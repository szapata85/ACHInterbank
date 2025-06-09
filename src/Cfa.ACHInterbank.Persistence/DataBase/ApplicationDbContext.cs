using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public class ApplicationDbContext : DbContext
{
    public DbSet<ClearingHouse> ClearingHouses { get; set; }
    public DbSet<AchCycle> AchCycles { get; set; }
    public DbSet<AchTransaction> AchTransactions { get; set; }
    public DbSet<BankHoliday> BankHolidays { get; set; }
    public DbSet<ClearingHouseConfig> ClearingHouseConfigs { get; set; }


    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<BankHoliday>().HasData(
            new BankHoliday { Id = 1, Date = new DateTime(2025, 1, 1), Description = "Año Nuevo" },
            new BankHoliday { Id = 2, Date = new DateTime(2025, 1, 6), Description = "Día de los Reyes Magos" },
            new BankHoliday { Id = 3, Date = new DateTime(2025, 3, 24), Description = "San José" },
            new BankHoliday { Id = 4, Date = new DateTime(2025, 4, 17), Description = "Jueves Santo" },
            new BankHoliday { Id = 5, Date = new DateTime(2025, 4, 18), Description = "Viernes Santo" },
            new BankHoliday { Id = 6, Date = new DateTime(2025, 5, 1), Description = "Día del Trabajo" },
            new BankHoliday { Id = 7, Date = new DateTime(2025, 5, 26), Description = "Ascensión del Señor" },
            new BankHoliday { Id = 8, Date = new DateTime(2025, 6, 16), Description = "Corpus Christi" },
            new BankHoliday { Id = 9, Date = new DateTime(2025, 6, 23), Description = "Sagrado Corazón" },
            new BankHoliday { Id = 10, Date = new DateTime(2025, 7, 20), Description = "Día de la Independencia" },
            new BankHoliday { Id = 11, Date = new DateTime(2025, 8, 7), Description = "Batalla de Boyacá" },
            new BankHoliday { Id = 12, Date = new DateTime(2025, 8, 18), Description = "La Asunción" },
            new BankHoliday { Id = 13, Date = new DateTime(2025, 10, 13), Description = "Día de la Raza" },
            new BankHoliday { Id = 14, Date = new DateTime(2025, 11, 3), Description = "Todos los Santos" },
            new BankHoliday { Id = 15, Date = new DateTime(2025, 11, 17), Description = "Independencia de Cartagena" },
            new BankHoliday { Id = 16, Date = new DateTime(2025, 12, 8), Description = "Inmaculada Concepción" },
            new BankHoliday { Id = 17, Date = new DateTime(2025, 12, 25), Description = "Navidad" }
        );
    }

}

