using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
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
    public DbSet<AchTransactionAddenda> AchTransactionAddendas { get; set; }
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
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerAddress> CustomerAddress { get; set; }
    public DbSet<CustomerPhone> CustomerPhones { get; set; }
    public DbSet<CustomerEmail> CustomerEmails { get; set; }

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

        //int year = DateTime.Now.Year;




        //modelBuilder.Entity<ClearingHouse>().HasData(
        //    new ClearingHouse { Id = 1, Name = "ACH Colombia", Code = "ACHCOL", ClearingHouseId = 1 },
        //    new ClearingHouse { Id = 2, Name = "CENIT", Code = "CENIT", ClearingHouseId = 1 }
        //    );

        //modelBuilder.Entity<ClearingHouseConfig>().HasData(new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, HolidayStrategy = "Colombian" });


        //    modelBuilder.Entity<TaskDefinition>().HasData(
        //new TaskDefinition
        //{
        //    Id = 1,
        //    Code = "AchCycleSeeder",
        //    Name = "Seed ciclos ACH y CENIT",
        //    Status = TaskStatusEnum.Enabled,
        //    CalendarPolicy = CalendarPolicyEnum.OnlyBusinessDays,
        //    ConcurrencyPolicy = ConcurrencyPolicyEnum.SkipIfRunning,
        //    RetryOnFailure = true,
        //    MaxRetries = 3,
        //    RetryBackoffSeconds = 60,

        //    PeriodicityType = PeriodicityTypeEnum.Cron,
        //    CronExpression = "0 30 0 1 1 ? *",
        //    TimeZoneId = "America/Bogota",
        //    StartAt = new DateTimeOffset(2025, 1, 1, 0, 30, 0, new TimeSpan(-5, 0, 0))
        //},
        //new TaskDefinition
        //{
        //    Id = 2,
        //    Code = "AchCycleScheduler",
        //    Name = "Generar ciclos diarios",
        //    Status = TaskStatusEnum.Enabled,
        //    CalendarPolicy = CalendarPolicyEnum.OnlyBusinessDays,
        //    ConcurrencyPolicy = ConcurrencyPolicyEnum.SkipIfRunning,
        //    RetryOnFailure = true,
        //    MaxRetries = 3,
        //    RetryBackoffSeconds = 60,

        //    PeriodicityType = PeriodicityTypeEnum.DailyAtTime,
        //    TimeOfDayTicks = new TimeOnly(2, 0).Ticks,
        //    TimeZoneId = "America/Bogota",
        //    StartAt = new DateTimeOffset(2025, 1, 1, 2, 0, 0, new TimeSpan(-5, 0, 0))
        //}
        //);




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
