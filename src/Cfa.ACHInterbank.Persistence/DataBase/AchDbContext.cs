using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Services;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public class AchDbContext : DbContext
{
    public AchDbContext(DbContextOptions<AchDbContext> options) : base(options) { }


    public DbSet<ClearingHouse> ClearingHouses { get; set; }
    public DbSet<AchCycle> AchCycles { get; set; }
    public DbSet<AchTransaction> AchTransactions { get; set; }
    public DbSet<FinancialInstitution> FinancialInstitutions { get; set; }
    public DbSet<BankHolidayModel> BankHolidays { get; set; }
    public DbSet<ClearingHouseConfig> ClearingHouseConfigs { get; set; }
    public DbSet<ClearingHouseCycleConfig> ClearingHouseCycleConfigs { get; set; }

    public DbSet<NachaHeader> NachaHeaders { get; set; }
    public DbSet<BatchHeader> BatchHeaders { get; set; }
    public DbSet<EntryDetail> EntryDetails { get; set; }
    public DbSet<AddendaRecord> AddendaRecords { get; set; }
    public DbSet<BatchControl> BatchControls { get; set; }
    public DbSet<FileControl> FileControls { get; set; }

    public DbSet<TaskDefinition> TaskDefinitions => Set<TaskDefinition>();
    public DbSet<TaskParameter> TaskParameters => Set<TaskParameter>();
    public DbSet<TaskExecutionLog> TaskExecutionLogs => Set<TaskExecutionLog>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AchDbContext).Assembly);


        modelBuilder.Entity<FinancialInstitution>()
            .HasMany(i => i.SourceTransactions)
            .WithOne(t => t.SourceInstitution)
            .HasForeignKey(t => t.SourceInstitutionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FinancialInstitution>()
            .HasMany(i => i.DestinationTransactions)
            .WithOne(t => t.DestinationInstitution)
            .HasForeignKey(t => t.DestinationInstitutionId)
            .OnDelete(DeleteBehavior.Restrict);


        modelBuilder.Entity<AchTransaction>()
            .HasOne(t => t.AchCycle)
            .WithMany(c => c.Transactions)
            .HasForeignKey(t => t.AchCycleId)
            .OnDelete(DeleteBehavior.Restrict); // o Cascade si aplica

        int year = DateTime.Now.Year;

        modelBuilder.Entity<BankHolidayModel>().HasData(
            new BankHolidayModel { Id = 1, Date = new DateOnly(year, 1, 1), Description = "Año Nuevo" },
            new BankHolidayModel { Id = 2, Date = new DateOnly(year, 1, 6), Description = "Día de los Reyes Magos" },
            new BankHolidayModel { Id = 3, Date = new DateOnly(year, 3, 24), Description = "San José" },
            new BankHolidayModel { Id = 4, Date = new DateOnly(year, 4, 17), Description = "Jueves Santo" },
            new BankHolidayModel { Id = 5, Date = new DateOnly(year, 4, 18), Description = "Viernes Santo" },
            new BankHolidayModel { Id = 6, Date = new DateOnly(year, 5, 1), Description = "Día del Trabajo" },
            new BankHolidayModel { Id = 7, Date = new DateOnly(year, 5, 26), Description = "Ascensión del Señor" },
            new BankHolidayModel { Id = 8, Date = new DateOnly(year, 6, 16), Description = "Corpus Christi" },
            new BankHolidayModel { Id = 9, Date = new DateOnly(year, 6, 23), Description = "Sagrado Corazón" },
            new BankHolidayModel { Id = 10, Date = new DateOnly(year, 7, 20), Description = "Día de la Independencia" },
            new BankHolidayModel { Id = 11, Date = new DateOnly(year, 8, 7), Description = "Batalla de Boyacá" },
            new BankHolidayModel { Id = 12, Date = new DateOnly(year, 8, 18), Description = "La Asunción" },
            new BankHolidayModel { Id = 13, Date = new DateOnly(year, 10, 13), Description = "Día de la Raza" },
            new BankHolidayModel { Id = 14, Date = new DateOnly(year, 11, 3), Description = "Todos los Santos" },
            new BankHolidayModel { Id = 15, Date = new DateOnly(year, 11, 17), Description = "Independencia de Cartagena" },
            new BankHolidayModel { Id = 16, Date = new DateOnly(year, 12, 8), Description = "Inmaculada Concepción" },
            new BankHolidayModel { Id = 17, Date = new DateOnly(year, 12, 25), Description = "Navidad" }
        );


        modelBuilder.Entity<ClearingHouse>().HasData(
            new ClearingHouse { Id = 1, Name = "ACH Colombia", Code = "ACHCOL", ClearingHouseId = 1 },
            new ClearingHouse { Id = 2, Name = "CENIT", Code = "CENIT", ClearingHouseId = 1 }
            );

        modelBuilder.Entity<ClearingHouseConfig>().HasData(new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, HolidayStrategy = "Colombian" });


        modelBuilder.Entity<ClearingHouseCycleConfig>().HasData(
            // ACH Colombia
            new ClearingHouseCycleConfig { Id = 1, ClearingHouseId = 1, CycleName = "Ciclo 1", CutoffTime = new TimeSpan(10, 30, 0), IsActive = true, EffectiveFrom = new DateTime(year, 1, 1) },
            new ClearingHouseCycleConfig { Id = 2, ClearingHouseId = 1, CycleName = "Ciclo 2", CutoffTime = new TimeSpan(13, 00, 0), IsActive = true, EffectiveFrom = new DateTime(year, 1, 1) },
            new ClearingHouseCycleConfig { Id = 3, ClearingHouseId = 1, CycleName = "Ciclo 3", CutoffTime = new TimeSpan(15, 30, 0), IsActive = true, EffectiveFrom = new DateTime(year, 1, 1) },
            new ClearingHouseCycleConfig { Id = 4, ClearingHouseId = 1, CycleName = "Ciclo 4", CutoffTime = new TimeSpan(17, 30, 0), IsActive = true, EffectiveFrom = new DateTime(year, 1, 1) },
            new ClearingHouseCycleConfig { Id = 5, ClearingHouseId = 1, CycleName = "Ciclo 5", CutoffTime = new TimeSpan(19, 00, 0), IsActive = true, EffectiveFrom = new DateTime(year, 1, 1) },

            // CENIT
            new ClearingHouseCycleConfig { Id = 6, ClearingHouseId = 2, CycleName = "Ciclo 1", CutoffTime = new TimeSpan(9, 30, 0), IsActive = true, EffectiveFrom = new DateTime(year, 1, 1) },
            new ClearingHouseCycleConfig { Id = 7, ClearingHouseId = 2, CycleName = "Ciclo 2", CutoffTime = new TimeSpan(12, 00, 0), IsActive = true, EffectiveFrom = new DateTime(year, 1, 1) },
            new ClearingHouseCycleConfig { Id = 8, ClearingHouseId = 2, CycleName = "Ciclo 3", CutoffTime = new TimeSpan(15, 00, 0), IsActive = true, EffectiveFrom = new DateTime(year, 1, 1) },
            new ClearingHouseCycleConfig { Id = 9, ClearingHouseId = 2, CycleName = "Ciclo 4", CutoffTime = new TimeSpan(17, 15, 0), IsActive = true, EffectiveFrom = new DateTime(year, 1, 1) },
            new ClearingHouseCycleConfig { Id = 10, ClearingHouseId = 2, CycleName = "Ciclo 5", CutoffTime = new TimeSpan(19, 15, 0), IsActive = true, EffectiveFrom = new DateTime(year, 1, 1) }
        );


        modelBuilder.Entity<TaskDefinition>().HasData(new TaskDefinition
    {
        Id = 1,
        Code = "AchCycleSeeder",
        Name = "Seed ciclos ACH y CENIT",
        Status = TaskStatusEnum.Enabled,
        CalendarPolicy = CalendarPolicyEnum.OnlyBusinessDays,
        ConcurrencyPolicy = ConcurrencyPolicyEnum.SkipIfRunning,
        RetryOnFailure = true,
        MaxRetries = 3,
        RetryBackoffSeconds = 60,

        // Usar Cron: 1 de enero a las 00:30
        PeriodicityType = PeriodicityTypeEnum.Cron,
        CronExpression = "0 30 0 1 1 ? *",
        TimeZoneId = "America/Bogota",
        StartAt = new DateTimeOffset(new DateTime(year, 1, 1, 0, 30, 0), TimeSpan.FromHours(-5))
    },
    new TaskDefinition
    {
        Id = 2,
        Code = "AchCycleScheduler",
        Name = "Generar ciclos diarios",
        Status = TaskStatusEnum.Enabled,
        CalendarPolicy = CalendarPolicyEnum.OnlyBusinessDays,
        ConcurrencyPolicy = ConcurrencyPolicyEnum.SkipIfRunning,
        RetryOnFailure = true,
        MaxRetries = 3,
        RetryBackoffSeconds = 60,

        // Diario a las 2:00 AM
        PeriodicityType = PeriodicityTypeEnum.DailyAtTime,
        TimeOfDay = new TimeOnly(2, 0),
        TimeZoneId = "America/Bogota",
        StartAt = new DateTimeOffset(new DateTime(year, 1, 1, 2, 0, 0), TimeSpan.FromHours(-5))
    }
);



        modelBuilder.Entity<TaskDefinition>(e =>
        {
            e.ToTable("TaskDefinition");
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.TimeZoneId).HasMaxLength(100);

            // Auditoría
            e.Property(x => x.CreatedAt).IsRequired();
            e.Property(x => x.UpdatedAt).IsRequired();
        });


        modelBuilder.Entity<TaskParameter>(e =>
        {
            e.ToTable("TaskParameters");
            e.HasIndex(x => new { x.TaskDefinitionId, x.Key }).IsUnique();
            e.Property(x => x.Key).HasMaxLength(100).IsRequired();
            e.Property(x => x.Value).HasMaxLength(2000).IsRequired();
        });

        modelBuilder.Entity<TaskExecutionLog>(e =>
        {
            e.ToTable("TaskExecutionLog");
            e.HasIndex(x => x.TaskDefinitionId);
            e.Property(x => x.ExecutionKey).HasMaxLength(64).IsRequired();
        });

    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries<IAuditableEntity>()
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }



}
