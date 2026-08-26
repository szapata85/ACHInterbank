using Cfa.ACHInterbank.Domain.Entities.Audit;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using Cfa.ACHInterbank.Domain.Entities.Branding;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Services;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Security.Claims;
using System.Text.Json;

namespace Cfa.ACHInterbank.Persistence.DataBase;

public class AchDbContext : DbContext, IDataProtectionKeyContext
{
    public const string AuditActionItemKey = "Audit.Action";
    public const string AuditCorrelationItemKey = "Audit.Correlation";
    private static readonly string[] AuditIgnoredProperties = ["CreatedAt", "UpdatedAt"];
    private static readonly TimeSpan ColombiaOffset = TimeSpan.FromHours(-5);
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly TimeProvider _timeProvider;

    public bool AuditEnabled { get; set; } = true;

    public AchDbContext(
        DbContextOptions<AchDbContext> options,
        IHttpContextAccessor? httpContextAccessor = null,
        TimeProvider? timeProvider = null) : base(options)
    {
        _httpContextAccessor = httpContextAccessor;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }


    public DbSet<ClearingHouse> ClearingHouses { get; set; }
    public DbSet<AchCycle> AchCycles { get; set; }
    public DbSet<AchTransaction> AchTransactions { get; set; }
    public DbSet<AchTransactionTraceSequence> AchTransactionTraceSequences => Set<AchTransactionTraceSequence>();
    public DbSet<AchTransactionAddenda> AchTransactionAddendas { get; set; }
    public DbSet<AchTransactionStateEvent> AchTransactionStateEvents { get; set; }
    public DbSet<FinancialInstitution> FinancialInstitutions { get; set; }
    public DbSet<BankHolidayModel> BankHolidays { get; set; }
    public DbSet<ClearingHouseSpecialDate> ClearingHouseSpecialDates { get; set; }
    public DbSet<ClearingHouseConfig> ClearingHouseConfigs { get; set; }
    public DbSet<ClearingHouseCycleConfig> ClearingHouseCycleConfigs { get; set; }

    public DbSet<NachaHeader> NachaHeaders { get; set; }
    public DbSet<BatchHeader> BatchHeaders { get; set; }
    public DbSet<EntryDetail> EntryDetails { get; set; }
    public DbSet<AddendaRecord> AddendaRecords { get; set; }
    public DbSet<BatchControl> BatchControls { get; set; }
    public DbSet<FileControl> FileControls { get; set; }
    public DbSet<IncomingNachaFileIngestion> IncomingNachaFileIngestions { get; set; }
    public DbSet<IncomingNachaFileProcessingResult> IncomingNachaFileProcessingResults { get; set; }
    public DbSet<IncomingNachaTransactionLink> IncomingNachaTransactionLinks { get; set; }
    public DbSet<IncomingNachaEntryClassification> IncomingNachaEntryClassifications { get; set; }
    public DbSet<IncomingNachaProcessingEvent> IncomingNachaProcessingEvents { get; set; }
    public DbSet<IncomingNachaDispatchQueue> IncomingNachaDispatchQueue { get; set; }
    public DbSet<IncomingNachaIntegrationExecution> IncomingNachaIntegrationExecution { get; set; }
    public DbSet<Customer> Customers { get; set; }
    public DbSet<CustomerAccount> CustomerAccounts { get; set; }
    public DbSet<DocumentTypeCatalog> DocumentTypes { get; set; }
    public DbSet<GenderCatalog> GenderTypes { get; set; }
    public DbSet<PersonTypeCatalog> PersonTypes { get; set; }
    public DbSet<PhoneTypeCatalog> PhoneTypes { get; set; }
    public DbSet<EmailTypeCatalog> EmailTypes { get; set; }
    public DbSet<AddressTypeCatalog> AddressTypes { get; set; }
    public DbSet<TransactionCodeCatalog> TransactionCodes { get; set; }
    public DbSet<CustomerAddress> CustomerAddress { get; set; }
    public DbSet<CustomerPhone> CustomerPhones { get; set; }
    public DbSet<CustomerEmail> CustomerEmails { get; set; }
    public DbSet<CustomerThirdParty> CustomerThirdParties { get; set; }

    public DbSet<TaskDefinition> TaskDefinitions => Set<TaskDefinition>();
    public DbSet<TaskParameter> TaskParameters => Set<TaskParameter>();
    public DbSet<TaskExecutionLog> TaskExecutionLogs => Set<TaskExecutionLog>();
    public DbSet<SchedulerInstanceState> SchedulerInstanceStates => Set<SchedulerInstanceState>();
    public DbSet<SchedulerProbeExecution> SchedulerProbeExecutions => Set<SchedulerProbeExecution>();

    public DbSet<InstitutionClearingHousePreference> InstitutionClearingHousePreferences { get; set; } = null!;

    public DbSet<NachaRecordLayout> NachaRecordLayouts => Set<NachaRecordLayout>();
    public DbSet<NachaRecordField> NachaRecordFields => Set<NachaRecordField>();
    public DbSet<NachaRecordDefinition> NachaRecordDefinitions => Set<NachaRecordDefinition>();
    public DbSet<NachaFileIdentifierMap> NachaFileIdentifierMaps => Set<NachaFileIdentifierMap>();
    public DbSet<CompanyEntryDescriptionCatalog> CompanyEntryDescriptionCatalogs => Set<CompanyEntryDescriptionCatalog>();
    public DbSet<AchBatch> AchBatches => Set<AchBatch>();
    public DbSet<BatchNumberSequence> BatchNumberSequences => Set<BatchNumberSequence>();
    public DbSet<BulkIngestionBatch> BulkIngestionBatches => Set<BulkIngestionBatch>();
    public DbSet<BulkIngestionItem> BulkIngestionItems => Set<BulkIngestionItem>();
    public DbSet<BulkIngestionAttempt> BulkIngestionAttempts => Set<BulkIngestionAttempt>();
    public DbSet<ReturnReason> ReturnReasons => Set<ReturnReason>();
    public DbSet<AchReturnGenerated> AchReturnsGenerated => Set<AchReturnGenerated>();
    public DbSet<AchReturnTraceSequence> AchReturnTraceSequences => Set<AchReturnTraceSequence>();
    public DbSet<AchFileExport> AchFileExports => Set<AchFileExport>();
    public DbSet<AchFileExportTransaction> AchFileExportTransactions => Set<AchFileExportTransaction>();
    public DbSet<AchFileTransmissionAttempt> AchFileTransmissionAttempts => Set<AchFileTransmissionAttempt>();
    public DbSet<AchFileTransportResult> AchFileTransportResults => Set<AchFileTransportResult>();
    public DbSet<ContrapartidaDispatchBatch> ContrapartidaDispatchBatches => Set<ContrapartidaDispatchBatch>();
    public DbSet<ContrapartidaDispatchItem> ContrapartidaDispatchItems => Set<ContrapartidaDispatchItem>();
    public DbSet<ContrapartidaDispatchAttempt> ContrapartidaDispatchAttempts => Set<ContrapartidaDispatchAttempt>();
    public DbSet<CenitCycleQueue> CenitCycleQueues => Set<CenitCycleQueue>();
    public DbSet<CenitCycleExecution> CenitCycleExecutions => Set<CenitCycleExecution>();
    public DbSet<CenitNettingExecution> CenitNettingExecutions => Set<CenitNettingExecution>();
    public DbSet<CenitNetPosition> CenitNetPositions => Set<CenitNetPosition>();
    public DbSet<CenitNettingDetail> CenitNettingDetails => Set<CenitNettingDetail>();
    public DbSet<LiquidityOptimizationDecision> LiquidityOptimizationDecisions => Set<LiquidityOptimizationDecision>();
    public DbSet<ReturnOfReturnFlow> ReturnOfReturnFlows => Set<ReturnOfReturnFlow>();
    public DbSet<AchReturnOfReturnGeneratedFileAudit> AchReturnOfReturnGeneratedFileAudits => Set<AchReturnOfReturnGeneratedFileAudit>();
    public DbSet<AchReturnOfReturnGeneratedFileAuditFlow> AchReturnOfReturnGeneratedFileAuditFlows => Set<AchReturnOfReturnGeneratedFileAuditFlow>();
    public DbSet<PaymentRailCapabilityRegistryEntry> PaymentRailCapabilityRegistry => Set<PaymentRailCapabilityRegistryEntry>();
    public DbSet<ExternalFileSequence> ExternalFileSequences => Set<ExternalFileSequence>();
    public DbSet<ExternalFileNameRegistry> ExternalFileNameRegistry => Set<ExternalFileNameRegistry>();
    public DbSet<ExternalFileNameValidationLog> ExternalFileNameValidationLog => Set<ExternalFileNameValidationLog>();
    public DbSet<ExternalFileNameReservation> ExternalFileNameReservations => Set<ExternalFileNameReservation>();
    public DbSet<AchReturnCode> AchReturnCodes => Set<AchReturnCode>();
    public DbSet<AchFileRejectionCode> AchFileRejectionCodes => Set<AchFileRejectionCode>();
    public DbSet<AchTransactionTypePolicy> AchTransactionTypePolicies => Set<AchTransactionTypePolicy>();
    public DbSet<AchReturnPolicy> AchReturnPolicies => Set<AchReturnPolicy>();
    public DbSet<AchReturnOfReturnPolicy> AchReturnOfReturnPolicies => Set<AchReturnOfReturnPolicy>();
    public DbSet<AchPrenotificationPolicy> AchPrenotificationPolicies => Set<AchPrenotificationPolicy>();
    public DbSet<ClearingHouseTransactionRule> ClearingHouseTransactionRules => Set<ClearingHouseTransactionRule>();
    public DbSet<NachaFileNamingRule> NachaFileNamingRules => Set<NachaFileNamingRule>();
    public DbSet<NachaInboundSimulation> NachaInboundSimulations => Set<NachaInboundSimulation>();
    public DbSet<NachaInboundSimulationEntry> NachaInboundSimulationEntries => Set<NachaInboundSimulationEntry>();
    public DbSet<AchResponseStatusMapping> AchResponseStatusMappings => Set<AchResponseStatusMapping>();
    public DbSet<AchResponse> AchResponses => Set<AchResponse>();
    public DbSet<AchResponseNotificationAttempt> AchResponseNotificationAttempts => Set<AchResponseNotificationAttempt>();
    public DbSet<AchResponseAudit> AchResponseAudits => Set<AchResponseAudit>();
    public DbSet<AchResponseOrphan> AchResponseOrphans => Set<AchResponseOrphan>();
    public DbSet<AchResponseReprocessAttempt> AchResponseReprocessAttempts => Set<AchResponseReprocessAttempt>();
    public DbSet<AchResponseReconciliationCase> AchResponseReconciliationCases => Set<AchResponseReconciliationCase>();
    public DbSet<AchOperationalReconciliationSnapshot> AchOperationalReconciliationSnapshots => Set<AchOperationalReconciliationSnapshot>();
    public DbSet<AchOperationalReconciliationDifference> AchOperationalReconciliationDifferences => Set<AchOperationalReconciliationDifference>();
    public DbSet<CatClearingHouse> CatClearingHouses => Set<CatClearingHouse>();
    public DbSet<CatFlowType> CatFlowTypes => Set<CatFlowType>();
    public DbSet<CatDirection> CatDirections => Set<CatDirection>();
    public DbSet<CatServiceClass> CatServiceClasses => Set<CatServiceClass>();
    public DbSet<CatRecordCode> CatRecordCodes => Set<CatRecordCode>();
    public DbSet<CatConfigStatus> CatConfigStatuses => Set<CatConfigStatus>();
    public DbSet<CatDataSourceType> CatDataSourceTypes => Set<CatDataSourceType>();
    public DbSet<CatRuleType> CatRuleTypes => Set<CatRuleType>();
    public DbSet<CfgProfile> CfgProfiles => Set<CfgProfile>();
    public DbSet<CfgProfileTag> CfgProfileTags => Set<CfgProfileTag>();
    public DbSet<CfgProfileRecord> CfgProfileRecords => Set<CfgProfileRecord>();
    public DbSet<CfgLayoutVariant> CfgLayoutVariants => Set<CfgLayoutVariant>();
    public DbSet<CfgLayoutField> CfgLayoutFields => Set<CfgLayoutField>();
    public DbSet<CfgFieldSourceDefinition> CfgFieldSourceDefinitions => Set<CfgFieldSourceDefinition>();
    public DbSet<CfgFieldRule> CfgFieldRules => Set<CfgFieldRule>();
    public DbSet<CfgRuleSet> CfgRuleSets => Set<CfgRuleSet>();
    public DbSet<CfgRuleSetRule> CfgRuleSetRules => Set<CfgRuleSetRule>();
    public DbSet<CfgPublishRequest> CfgPublishRequests => Set<CfgPublishRequest>();
    public DbSet<HistConfigSnapshot> HistConfigSnapshots => Set<HistConfigSnapshot>();
    public DbSet<HistConfigChange> HistConfigChanges => Set<HistConfigChange>();

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<PasswordRuleSetting> PasswordRuleSettings => Set<PasswordRuleSetting>();
    public DbSet<Menu> Menus => Set<Menu>();
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<MenuItemRole> MenuItemRoles => Set<MenuItemRole>();
    public DbSet<MenuItemPermission> MenuItemPermissions => Set<MenuItemPermission>();

    public DbSet<DigitalEnvelopeCertificate> DigitalEnvelopeCertificates => Set<DigitalEnvelopeCertificate>();
    public DbSet<DigitalCertificate> DigitalCertificates => Set<DigitalCertificate>();
    public DbSet<DigitalCertificateVersion> DigitalCertificateVersions => Set<DigitalCertificateVersion>();
    public DbSet<CertificateUsageLog> CertificateUsageLogs => Set<CertificateUsageLog>();
    public DbSet<CertificateRotationHistory> CertificateRotationHistories => Set<CertificateRotationHistory>();
    public DbSet<CertificateLoadAudit> CertificateLoadAudits => Set<CertificateLoadAudit>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<DigitalEnvelopeOperationLog> DigitalEnvelopeOperationLogs => Set<DigitalEnvelopeOperationLog>();
    public DbSet<NachaSecurityOperation> NachaSecurityOperations => Set<NachaSecurityOperation>();
    public DbSet<BrandingSetting> BrandingSettings => Set<BrandingSetting>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<AuthLog> AuthLogs => Set<AuthLog>();
    public DbSet<NavigationLog> NavigationLogs => Set<NavigationLog>();
    public DbSet<LoginLockoutSetting> LoginLockoutSettings => Set<LoginLockoutSetting>();
    public DbSet<SoapIntegrationSetting> SoapIntegrationSettings => Set<SoapIntegrationSetting>();
    public DbSet<IntegrationMethod> IntegrationMethods => Set<IntegrationMethod>();
    public DbSet<IntegrationResponseCode> IntegrationResponseCodes => Set<IntegrationResponseCode>();
    public DbSet<IntegrationMethodParameter> IntegrationMethodParameters => Set<IntegrationMethodParameter>();
    public DbSet<IntegrationSourceCatalogField> IntegrationSourceCatalogFields => Set<IntegrationSourceCatalogField>();
    public DbSet<IntegrationMappingSet> IntegrationMappingSets => Set<IntegrationMappingSet>();
    public DbSet<IntegrationMappingRule> IntegrationMappingRules => Set<IntegrationMappingRule>();
    public DbSet<IntegrationMappingSetHistory> IntegrationMappingSetHistory => Set<IntegrationMappingSetHistory>();
    public DbSet<IntegrationMappingTrace> IntegrationMappingTraces => Set<IntegrationMappingTrace>();
    public DbSet<IntegrationMappingTraceEntry> IntegrationMappingTraceEntries => Set<IntegrationMappingTraceEntry>();




    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AchDbContext).Assembly);

        var isPostgres = Database.ProviderName?.Contains("Npgsql") == true;
        modelBuilder.Entity<AchTransactionStateEvent>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("UX_AchTransactionStateEvents_IdempotencyKey")
            .HasFilter(isPostgres ? "\"IdempotencyKey\" IS NOT NULL" : "[IdempotencyKey] IS NOT NULL");
        modelBuilder.Entity<AchFileExport>()
            .HasIndex(x => new { x.AchCycleId, x.ExportKind, x.IsEncrypted, x.Version })
            .IsUnique()
            .HasDatabaseName("UX_AchFileExports_Cycle_Kind_Encrypted_Version")
            .HasFilter(isPostgres ? "\"Version\" IS NOT NULL" : "[Version] IS NOT NULL");
        modelBuilder.Entity<CenitCycleQueue>()
            .HasIndex(x => new { x.AchTransactionId, x.TargetAchCycleId })
            .IsUnique()
            .HasDatabaseName("UX_CenitCycleQueues_ActiveTarget")
            .HasFilter(isPostgres ? "\"Status\" = 'Queued'" : "[Status] = 'Queued'");
        modelBuilder.Entity<DigitalEnvelopeCertificate>()
            .Property(c => c.UploadedAt)
            .HasDefaultValueSql(isPostgres ? "timezone('utc', now())" : "GETUTCDATE()");

        modelBuilder.Entity<DigitalCertificateVersion>()
            .HasIndex(c => new
            {
                c.FinancialInstitutionId,
                c.ClearingHouseId,
                c.Environment,
                c.Purpose,
                c.HolderType
            })
            .HasDatabaseName("UX_DCV_Active_Context")
            .IsUnique()
            .HasFilter(isPostgres
                ? "\"Status\" = 2"
                : "[Status] = 2");

        modelBuilder.Entity<IncomingNachaFileIngestion>()
            .HasIndex(x => new { x.FileHashSha256, x.FileSize })
            .IsUnique()
            .HasDatabaseName("UX_IncomingNachaFileIngestions_FileHash_FileSize_Canonical")
            .HasFilter(isPostgres
                ? "\"IsReprocess\" = false"
                : "[IsReprocess] = 0");

        if (isPostgres)
        {
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                value => value.Kind == DateTimeKind.Utc ? value : DateTime.SpecifyKind(value, DateTimeKind.Utc),
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                value => value.HasValue
                    ? (value.Value.Kind == DateTimeKind.Utc
                        ? value.Value
                        : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc))
                    : value,
                value => value.HasValue
                    ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                    : value);

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.GetValueConverter() != null)
                    {
                        continue;
                    }

                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }
        }

        var auditDefaultSql = isPostgres ? "timezone('utc', now())" : "SYSUTCDATETIME()";
        modelBuilder.Entity<User>()
            .Property(u => u.CreatedAt)
            .HasDefaultValueSql(auditDefaultSql);
        modelBuilder.Entity<User>()
            .Property(u => u.UpdatedAt)
            .HasDefaultValueSql(auditDefaultSql);
        modelBuilder.Entity<UserRole>()
            .Property(ur => ur.CreatedAt)
            .HasDefaultValueSql(auditDefaultSql);
        modelBuilder.Entity<UserRole>()
            .Property(ur => ur.UpdatedAt)
            .HasDefaultValueSql(auditDefaultSql);

        var primaryFilter = isPostgres ? "\"IsPrimary\" = true" : "[IsPrimary] = 1";
        modelBuilder.Entity<CustomerPhone>()
            .HasIndex(p => new { p.CustomerId, p.IsPrimary })
            .IsUnique()
            .HasFilter(primaryFilter);
        modelBuilder.Entity<CustomerEmail>()
            .HasIndex(e => new { e.CustomerId, e.IsPrimary })
            .IsUnique()
            .HasFilter(primaryFilter);

        modelBuilder.Entity<ClearingHouseCycleConfig>()
            .Property(c => c.EffectiveFrom)
            .HasConversion(
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc),
                value => DateTime.SpecifyKind(value, DateTimeKind.Utc));

        modelBuilder.Entity<ClearingHouseCycleConfig>()
            .Property(c => c.EffectiveTo)
            .HasConversion(
                value => value.HasValue
                    ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                    : value,
                value => value.HasValue
                    ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
                    : value);

        modelBuilder.Entity<ClearingHouseCycleConfig>()
            .Property(c => c.PolicyVersion)
            .HasMaxLength(80)
            .IsRequired();



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
            e.Property(x => x.Description).HasMaxLength(1000);
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
            e.HasIndex(x => x.ExecutionId).IsUnique();
            e.HasIndex(x => x.RequestId).IsUnique();
            e.HasIndex(x => x.ManualConcurrencyKey)
                .IsUnique()
                .HasFilter(isPostgres ? "\"ManualConcurrencyKey\" IS NOT NULL" : "[ManualConcurrencyKey] IS NOT NULL");
            e.HasIndex(x => new { x.TaskCode, x.StartedAt });
            e.HasIndex(x => new { x.Status, x.StartedAt });
            e.Property(x => x.ExecutionKey).HasMaxLength(64).IsRequired();
            e.Property(x => x.TaskCode).HasMaxLength(100).IsRequired();
            e.Property(x => x.JobName).HasMaxLength(150).IsRequired();
            e.Property(x => x.JobGroup).HasMaxLength(100).IsRequired();
            e.Property(x => x.TriggerName).HasMaxLength(150).IsRequired();
            e.Property(x => x.TriggerType).HasMaxLength(30).IsRequired();
            e.Property(x => x.FireInstanceId).HasMaxLength(200).IsRequired();
            e.Property(x => x.SchedulerInstanceId).HasMaxLength(200).IsRequired();
            e.Property(x => x.SchedulerInstanceName).HasMaxLength(200).IsRequired();
            e.Property(x => x.RequestedByUserId).HasMaxLength(100);
            e.Property(x => x.RequestedByUserName).HasMaxLength(200);
            e.Property(x => x.RequestReason).HasMaxLength(500);
            e.Property(x => x.RequestId).HasMaxLength(36);
            e.Property(x => x.IdempotencyKey).HasMaxLength(200).IsRequired();
            e.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
            e.Property(x => x.OriginalFireInstanceId).HasMaxLength(200);
            e.Property(x => x.RecoveredByInstanceId).HasMaxLength(200);
            e.Property(x => x.RecoveryResult).HasMaxLength(500);
            e.Property(x => x.ErrorCode).HasMaxLength(100);
            e.Property(x => x.ManualConcurrencyKey).HasMaxLength(100);
        });

        modelBuilder.Entity<SchedulerInstanceState>(e =>
        {
            e.ToTable("SchedulerInstanceStates");
            e.HasIndex(x => new { x.SchedulerName, x.InstanceId }).IsUnique();
            e.HasIndex(x => x.LastHeartbeatUtc);
            e.Property(x => x.SchedulerName).HasMaxLength(120).IsRequired();
            e.Property(x => x.InstanceId).HasMaxLength(200).IsRequired();
            e.Property(x => x.InstanceName).HasMaxLength(200).IsRequired();
            e.Property(x => x.HostName).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
            e.Property(x => x.Version).HasMaxLength(50).IsRequired();
        });

        modelBuilder.Entity<SchedulerProbeExecution>(e =>
        {
            e.ToTable("SchedulerProbeExecutions");
            e.HasIndex(x => x.ProbeKey).IsUnique();
            e.HasIndex(x => x.ExecutionId);
            e.Property(x => x.ProbeKey).HasMaxLength(200).IsRequired();
            e.Property(x => x.SchedulerInstanceId).HasMaxLength(200).IsRequired();
            e.Property(x => x.Status).HasMaxLength(30).IsRequired();
        });

    }

    public override int SaveChanges()
    {
        return SaveChangesAsync().GetAwaiter().GetResult();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (ChangeTracker.Entries<AchResponseAudit>().Any(x => x.State is EntityState.Modified or EntityState.Deleted))
            throw new InvalidOperationException("La auditoría de respuestas ACH es inmutable.");

        foreach (var entry in ChangeTracker.Entries<AchTransaction>().Where(x => x.State == EntityState.Modified))
        {
            var classificationChanged = HasChanged(entry.Property(x => x.Direction))
                || HasChanged(entry.Property(x => x.Origin))
                || HasChanged(entry.Property(x => x.MonetaryIntegrationRoute))
                || HasChanged(entry.Property(x => x.ClassificationStatus))
                || HasChanged(entry.Property(x => x.SourceInstitutionWasDefaultAtCreation))
                || HasChanged(entry.Property(x => x.ClassifiedAtUtc))
                || HasChanged(entry.Property(x => x.ClassificationVersion));
            if (classificationChanged)
            {
                throw new InvalidOperationException("La clasificación histórica de una transacción ACH es inmutable.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<AchFileExport>().Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            var export = entry.Entity;
            var requiresTransmissionEvidence = export.LifecycleStatus is
                AchFileExportLifecycleStatus.Transmitted or
                AchFileExportLifecycleStatus.Acknowledged or
                AchFileExportLifecycleStatus.Accepted or
                AchFileExportLifecycleStatus.Rejected;
            if (requiresTransmissionEvidence
                && (string.IsNullOrWhiteSpace(export.TransmissionReference) || !export.TransmittedAtUtc.HasValue))
            {
                throw new InvalidOperationException("Un archivo no puede declararse transmitido sin referencia externa y fecha verificables.");
            }

            var requiresAcknowledgementEvidence = export.LifecycleStatus is
                AchFileExportLifecycleStatus.Acknowledged or
                AchFileExportLifecycleStatus.Accepted or
                AchFileExportLifecycleStatus.Rejected;
            if (requiresAcknowledgementEvidence
                && (!export.AcknowledgedAtUtc.HasValue || string.IsNullOrWhiteSpace(export.AcknowledgementCode)))
            {
                throw new InvalidOperationException("Un archivo no puede declararse acusado, aceptado o rechazado sin evidencia de acuse.");
            }
        }

        foreach (var entry in ChangeTracker.Entries<AchResponse>().Where(x => x.State is EntityState.Added or EntityState.Modified))
            entry.Entity.Version = Guid.NewGuid();
        foreach (var entry in ChangeTracker.Entries<AchResponseStatusMapping>().Where(x => x.State is EntityState.Added or EntityState.Modified))
            entry.Entity.Version = Guid.NewGuid();
        foreach (var entry in ChangeTracker.Entries<AchResponseOrphan>().Where(x => x.State is EntityState.Added or EntityState.Modified))
            entry.Entity.Version = Guid.NewGuid();
        foreach (var entry in ChangeTracker.Entries<AchResponseReconciliationCase>().Where(x => x.State is EntityState.Added or EntityState.Modified))
            entry.Entity.Version = Guid.NewGuid();
        foreach (var entry in ChangeTracker.Entries<AchResponseReprocessAttempt>().Where(x => x.State is EntityState.Added or EntityState.Modified))
            entry.Entity.Version = Guid.NewGuid();

        var now = _timeProvider.GetUtcNow();
        var changedBy = ResolveChangedBy();
        var auditNow = now.ToOffset(ColombiaOffset).DateTime;
        var auditEntries = AuditEnabled
            ? BuildAuditEntries(auditNow, changedBy, ResolveAuditCorrelation(), ResolveAuditAction())
            : [];

        var entries = ChangeTracker
            .Entries<IAuditableEntity>()
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                var createdAt = entry.Property(nameof(IAuditableEntity.CreatedAt));
                if (createdAt.IsModified)
                {
                    createdAt.CurrentValue = createdAt.OriginalValue;
                    createdAt.IsModified = false;
                }

                var updatedAt = entry.Property(nameof(IAuditableEntity.UpdatedAt));
                var explicitlyUpdatedForAggregateChange = updatedAt.IsModified
                    && !Equals(updatedAt.CurrentValue, updatedAt.OriginalValue);
                var hasRealModification = entry.Properties.Any(property =>
                    property.IsModified
                    && property.Metadata.Name is not nameof(IAuditableEntity.CreatedAt)
                    && property.Metadata.Name is not nameof(IAuditableEntity.UpdatedAt));

                if (hasRealModification || explicitlyUpdatedForAggregateChange)
                {
                    entry.Entity.UpdatedAt = now;
                }
                else
                {
                    updatedAt.IsModified = false;
                }
            }
        }

        var profileEntries = ChangeTracker
            .Entries<CfgProfile>()
            .Where(x => x.State is EntityState.Added or EntityState.Modified)
            .ToList();

        foreach (var entry in profileEntries)
        {
            entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            entry.Property(x => x.RowVersion).IsModified = true;
        }

        if (auditEntries.Count > 0)
        {
            AuditLogs.AddRange(auditEntries);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    private static bool HasChanged<T>(PropertyEntry<AchTransaction, T> property)
        => property.IsModified && !EqualityComparer<T>.Default.Equals(property.OriginalValue, property.CurrentValue);

    private List<AuditLog> BuildAuditEntries(DateTime now, string changedBy, string? correlationId, string? actionContext)
    {
        var auditEntries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog)
            {
                if (entry.State is EntityState.Modified or EntityState.Deleted)
                {
                    throw new InvalidOperationException("Audit logs are immutable and cannot be modified.");
                }

                continue;
            }

            if (entry.Entity is TaskExecutionLog)
            {
                continue;
            }

            if (entry.State is EntityState.Detached or EntityState.Unchanged)
            {
                continue;
            }

            var isModified = entry.State == EntityState.Modified;
            var beforeJson = entry.State == EntityState.Added ? null : SerializeValues(entry, useOriginalValues: true, onlyModified: false);
            var afterJson = entry.State == EntityState.Deleted ? null : SerializeValues(entry, useOriginalValues: false, onlyModified: false);
            var changedFields = SerializeChangedFields(entry, isModified);

            if (beforeJson is null && afterJson is null && changedFields is null)
            {
                continue;
            }

            auditEntries.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                EntityName = entry.Metadata.ClrType.Name,
                EntityId = GetPrimaryKey(entry),
                Action = actionContext ?? entry.State.ToString(),
                ChangedBy = changedBy,
                ChangedAt = now,
                CorrelationId = correlationId,
                ChangedFields = changedFields,
                BeforeJson = beforeJson,
                AfterJson = afterJson
            });
        }

        return auditEntries;
    }

    private static string? SerializeValues(EntityEntry entry, bool useOriginalValues, bool onlyModified)
    {
        var properties = entry.Properties
            .Where(property => !property.Metadata.IsShadowProperty())
            .Where(property => !AuditIgnoredProperties.Contains(property.Metadata.Name, StringComparer.OrdinalIgnoreCase))
            .Where(property => !property.IsTemporary);

        if (onlyModified)
        {
            properties = properties.Where(property => property.IsModified);
        }

        var values = new Dictionary<string, object?>();
        foreach (var property in properties)
        {
            var value = useOriginalValues ? property.OriginalValue : property.CurrentValue;
            values[property.Metadata.Name] = value;
        }

        if (values.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(values);
    }

    private static string? SerializeChangedFields(EntityEntry entry, bool onlyModified)
    {
        var properties = entry.Properties
            .Where(property => !property.Metadata.IsShadowProperty())
            .Where(property => !AuditIgnoredProperties.Contains(property.Metadata.Name, StringComparer.OrdinalIgnoreCase))
            .Where(property => !property.IsTemporary);

        if (onlyModified)
        {
            properties = properties.Where(property => property.IsModified);
        }

        var names = properties.Select(property => property.Metadata.Name).Distinct().ToList();
        if (names.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(names);
    }

    private static string GetPrimaryKey(EntityEntry entry)
    {
        var keyProperties = entry.Properties
            .Where(property => property.Metadata.IsPrimaryKey())
            .ToList();

        if (keyProperties.Count == 1)
        {
            var value = keyProperties[0].CurrentValue ?? keyProperties[0].OriginalValue;
            return value?.ToString() ?? string.Empty;
        }

        var keyValues = keyProperties.Select(property =>
        {
            var value = property.CurrentValue ?? property.OriginalValue;
            return $"{property.Metadata.Name}={value}";
        });

        return string.Join(",", keyValues);
    }

    public async Task<IReadOnlyList<object>> ExecuteDynamicSqlAsync(
        string sql,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var results = new List<object>();
        await using var connection = Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (key, value) in parameters)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = $"@{key}";
            parameter.Value = value ?? DBNull.Value;
            command.Parameters.Add(parameter);
        }

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] = await reader.IsDBNullAsync(i, cancellationToken) ? null : reader.GetValue(i);
            }

            results.Add(row);
        }

        return results;
    }

    private string ResolveChangedBy()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        var userId = user?.FindFirst("uid")?.Value
            ?? user?.FindFirst("sub")?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return userId;
        }

        var name = user?.Identity?.Name;
        return string.IsNullOrWhiteSpace(name) ? "system" : name;
    }

    private string? ResolveAuditCorrelation()
    {
        var context = _httpContextAccessor?.HttpContext;
        if (context?.Items.TryGetValue(AuditCorrelationItemKey, out var value) == true && value is string configured)
        {
            return configured.Length <= 128 ? configured : configured[..128];
        }

        return string.IsNullOrWhiteSpace(context?.TraceIdentifier) ? null : context.TraceIdentifier;
    }

    private string? ResolveAuditAction()
    {
        var context = _httpContextAccessor?.HttpContext;
        return context?.Items.TryGetValue(AuditActionItemKey, out var value) == true
            ? value as string
            : null;
    }



}
