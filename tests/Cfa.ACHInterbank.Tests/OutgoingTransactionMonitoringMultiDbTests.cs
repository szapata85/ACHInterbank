using Cfa.ACHInterbank.Application.OutgoingTransactionMonitoring;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.OutgoingTransactionMonitoring;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Cfa.ACHInterbank.Persistence.Security.Services;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Cfa.ACHInterbank.Tests;

public sealed class OutgoingTransactionMonitoringMultiDbTests
{
    private const string RequiredVariable = "RUN_OUTGOING_MONITOR_MULTIDB";
    private static readonly DateTimeOffset ScenarioNow = new(2026, 8, 2, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset MonitoringNowAtUtcDateBoundary = new(2026, 8, 5, 0, 30, 0, TimeSpan.Zero);

    [Fact]
    [Trait("Category", "OutgoingMonitorMultiDb")]
    [Trait("Provider", "SqlServer")]
    public Task QueryAndDetail_RunAgainstSqlServer() => RunAsync(DatabaseProvider.SqlServer);

    [Fact]
    [Trait("Category", "OutgoingMonitorMultiDb")]
    [Trait("Provider", "PostgreSql")]
    public Task QueryAndDetail_RunAgainstPostgreSql() => RunAsync(DatabaseProvider.PostgreSql);

    [Fact]
    [Trait("Category", "LocalRuntimeFixture")]
    public async Task SeedDeterministicLocalRuntimeFixture_WhenExplicitlyEnabled()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable("SEED_OUTGOING_MONITOR_RUNTIME_FIXTURE"), "true", StringComparison.OrdinalIgnoreCase))
            return;
        var provider = Environment.GetEnvironmentVariable("OUTGOING_MONITOR_RUNTIME_PROVIDER") ?? "SqlServer";
        var connectionVariable = provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase)
            ? "OUTGOING_MONITOR_RUNTIME_POSTGRES_CONNECTION_STRING"
            : "OUTGOING_MONITOR_RUNTIME_SQLSERVER_CONNECTION_STRING";
        var connectionString = Environment.GetEnvironmentVariable(connectionVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException($"Falta {connectionVariable}.");
        var builder = new DbContextOptionsBuilder<AchDbContext>();
        if (provider.Equals("PostgreSql", StringComparison.OrdinalIgnoreCase)) builder.UseNpgsql(connectionString);
        else builder.UseSqlServer(connectionString, sql => sql.MigrationsAssembly("Cfa.ACHInterbank.Persistence.Migrations.SqlServer"));
        var options = builder.Options;
        await using var context = new AchDbContext(options);
        var id = await SeedAsync(context);
        var phase4 = await SeedPhase4Async(context);
        (await context.AchTransactions.AnyAsync(item => item.Id == id)).Should().BeTrue();
        (await context.AchTransactions.CountAsync(item => item.TransactionExternalId.StartsWith("UAT-F4-MON-SAL-"))).Should().Be(37);
        (await context.ContrapartidaDispatchAttempts.CountAsync(item => item.DispatchItem.AchTransactionId == phase4.RetrySucceeded)).Should().Be(2);
        (await context.AchFileExportTransactions.CountAsync(item => item.AchTransactionId == phase4.ExactFile)).Should().Be(1);
    }

    private static async Task RunAsync(DatabaseProvider provider)
    {
        EnsureConfiguration(provider);
        await using var fixture = await DatabaseFixture.CreateAsync(provider);
        await using var context = fixture.CreateContext();
        await context.Database.MigrateAsync();
        var transactionId = await SeedAsync(context);
        var persisted = await context.AchTransactions.AsNoTracking()
            .Where(item => item.Id == transactionId)
            .Select(item => new { item.CreatedAt, item.Direction, item.ClassificationStatus })
            .SingleAsync();
        persisted.Direction.Should().Be(AchTransactionDirection.Outgoing);
        persisted.ClassificationStatus.Should().Be(AchTransactionClassificationStatus.Determined);
        var service = new OutgoingTransactionMonitoringQueryService(context, new OutgoingTransactionMonitoringStatusPolicy(),
            new FixedTimeProvider(MonitoringNowAtUtcDateBoundary));

        var page = await service.SearchAsync(new OutgoingTransactionMonitoringQuery
        {
            FromUtc = persisted.CreatedAt.AddDays(-1),
            ToUtc = persisted.CreatedAt.AddDays(1),
            PageSize = 10
        });

        page.TotalItems.Should().Be(1, "solo la clasificación persistida de salida determinada pertenece al monitor");
        page.Items.Should().ContainSingle();
        page.Items[0].Id.Should().Be(transactionId);
        page.Items[0].MaskedDestinationAccount.Should().Be("******7890");
        page.Items[0].InitialResultDisplayName.Should().Be("Aceptada");
        page.Items[0].SubsequentSituationDisplayName.Should().Be("Devuelta posteriormente");
        page.Items[0].FileName.Should().Be("SALIDA.002");
        page.Items[0].FileVersion.Should().Be(2);
        page.Items[0].FileLifecycleStatusDisplayName.Should().Be("Protegido; sin evidencia de transmisión");

        var detail = await service.GetDetailAsync(transactionId, includeTechnicalDetail: false);
        detail.Should().NotBeNull();
        detail!.Files.Should().HaveCount(2);
        detail.Files.Select(item => item.Version).Should().Equal(1, 2);
        detail.Files.Should().OnlyContain(item => !item.HasTransmissionEvidence);
        detail.Timeline.Should().Contain(item => item.StageDisplayName == "Aceptación");
        detail.Timeline.Should().Contain(item => item.StageDisplayName == "Devolución");
        detail.TechnicalDetail.Should().BeNull();

        var ids = await SeedPhase4Async(context);
        var phase4 = await service.SearchAsync(new OutgoingTransactionMonitoringQuery
        {
            FromUtc = persisted.CreatedAt.AddDays(-1), ToUtc = persisted.CreatedAt.AddDays(1), PageSize = 50
        });
        phase4.TotalItems.Should().Be(37);
        phase4.Items.Should().OnlyContain(item => !item.TransactionExternalId.Contains("HISTORICA"));
        phase4.Items.Where(item => item.TransactionExternalId.StartsWith("UAT-F4-MON-SAL-"))
            .Should().OnlyContain(item => item.MaskedDestinationAccount == "******7890");
        phase4.Items.Single(item => item.Id == ids.FutureCycle).ProcessStatusCode.Should().Be("Scheduled");
        phase4.Items.Single(item => item.Id == ids.FutureCycle).NextExpectedStepDisplayName.Should().Contain("fecha prevista");
        var futureDetail = await service.GetDetailAsync(ids.FutureCycle, includeTechnicalDetail: false);
        futureDetail!.Summary.ProcessStatusCode.Should().Be("Scheduled");
        phase4.Items.Single(item => item.Id == ids.AchSpecialDateCycle).ProcessStatusCode.Should().Be("Scheduled");
        phase4.Items.Single(item => item.Id == ids.CenitSpecialDateCycle).ProcessStatusCode.Should().Be("Scheduled");
        phase4.Items.Single(item => item.Id == ids.PendingResponse).InitialResultCode.Should().Be("PendingResponse");
        phase4.Items.Single(item => item.Id == ids.Accepted).InitialResultCode.Should().Be("Accepted");
        phase4.Items.Single(item => item.Id == ids.Rejected).InitialResultCode.Should().Be("Rejected");
        phase4.Items.Single(item => item.Id == ids.TechnicalFailure).ProcessStatusCode.Should().Be("TechnicalError");
        phase4.Items.Single(item => item.Id == ids.TechnicalFailure).InitialResultCode.Should().Be("NotDetermined");
        phase4.Items.Single(item => item.Id == ids.AcceptedReturned).SubsequentSituationCode.Should().Be("ReturnedLater");
        phase4.Items.Single(item => item.Id == ids.WithoutFile).FileName.Should().BeNull();
        phase4.Items.Single(item => item.Id == ids.ExactFile).FileName.Should().Be("UAT-F4-SALIDA.001");
        phase4.Items.Single(item => item.Id == ids.ExactFile).FileVersion.Should().Be(1);

        var rejected = await service.SearchAsync(new OutgoingTransactionMonitoringQuery
        {
            FromUtc = persisted.CreatedAt.AddDays(-1), ToUtc = persisted.CreatedAt.AddDays(1), ResponseCode = " r01 ", PageSize = 10
        });
        rejected.Items.Should().ContainSingle(item => item.Id == ids.Rejected);
        var scheduled = await service.SearchAsync(new OutgoingTransactionMonitoringQuery
        {
            FromUtc = persisted.CreatedAt.AddDays(-1), ToUtc = persisted.CreatedAt.AddDays(1), ProcessStatus = "Scheduled", PageSize = 10
        });
        scheduled.Items.Select(item => item.Id).Should().BeEquivalentTo([
            ids.FutureCycle,
            ids.AchSpecialDateCycle,
            ids.CenitSpecialDateCycle]);
        var pending = await service.SearchAsync(new OutgoingTransactionMonitoringQuery
        {
            FromUtc = persisted.CreatedAt.AddDays(-1), ToUtc = persisted.CreatedAt.AddDays(1), InitialResult = "PendingResponse", PageSize = 10
        });
        pending.Items.Should().ContainSingle(item => item.Id == ids.PendingResponse);

        var firstPage = await service.SearchAsync(new OutgoingTransactionMonitoringQuery
        {
            FromUtc = persisted.CreatedAt.AddDays(-1), ToUtc = persisted.CreatedAt.AddDays(1), PageSize = 10
        });
        var secondPage = await service.SearchAsync(new OutgoingTransactionMonitoringQuery
        {
            FromUtc = persisted.CreatedAt.AddDays(-1), ToUtc = persisted.CreatedAt.AddDays(1), PageNumber = 2, PageSize = 10
        });
        firstPage.TotalPages.Should().Be(4);
        firstPage.Items.Select(item => item.Id).Should().NotIntersectWith(secondPage.Items.Select(item => item.Id));

        var exactFileDetail = await service.GetDetailAsync(ids.ExactFile, includeTechnicalDetail: true);
        exactFileDetail!.Files.Should().ContainSingle();
        exactFileDetail.Files[0].FileName.Should().Be("UAT-F4-SALIDA.001");
        exactFileDetail.Files[0].HasTransmissionEvidence.Should().BeFalse();
        exactFileDetail.TechnicalDetail.Should().NotBeNull();

        var returnedDetail = await service.GetDetailAsync(ids.AcceptedReturned, includeTechnicalDetail: false);
        returnedDetail!.Timeline.Should().Contain(item => item.StageDisplayName == "Aceptaci\u00f3n");
        returnedDetail.Timeline.Should().Contain(item => item.StageDisplayName == "Devoluci\u00f3n");
        returnedDetail.TechnicalDetail.Should().BeNull();

        var retryAttempts = await context.ContrapartidaDispatchAttempts.AsNoTracking()
            .Where(item => item.DispatchItem.AchTransactionId == ids.RetrySucceeded)
            .OrderBy(item => item.AttemptNumber).ToListAsync();
        retryAttempts.Should().HaveCount(2);
        retryAttempts.Select(item => item.IsTechnicalFailure).Should().Equal(true, false);
        retryAttempts.Select(item => item.IsSuccessful).Should().Equal(false, true);
        (await context.AchTransactions.CountAsync(item => item.Id == ids.RetrySucceeded)).Should().Be(1);

        await ValidateProcContrapartidasBootstrapAsync(context, transactionId);
    }

    private static async Task ValidateProcContrapartidasBootstrapAsync(AchDbContext context, int transactionId)
    {
        var bootstrapper = new IntegrationMappingBootstrapper(context);
        await bootstrapper.EnsureAsync();
        var method = await context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Contrapartidas");
        var firstSet = await context.IntegrationMappingSets.SingleAsync(x =>
            x.MethodId == method.Id && x.Status == IntegrationMappingSetStatusEnum.Published && x.IsActive);
        var firstRuleIds = await context.IntegrationMappingRules
            .Where(x => x.MappingSetId == firstSet.Id)
            .OrderBy(x => x.ParameterId)
            .Select(x => x.Id)
            .ToListAsync();

        await bootstrapper.EnsureAsync();
        await bootstrapper.EnsureAsync();

        var finalSet = await context.IntegrationMappingSets.SingleAsync(x =>
            x.MethodId == method.Id && x.Status == IntegrationMappingSetStatusEnum.Published && x.IsActive);
        var finalRuleIds = await context.IntegrationMappingRules
            .Where(x => x.MappingSetId == finalSet.Id)
            .OrderBy(x => x.ParameterId)
            .Select(x => x.Id)
            .ToListAsync();
        finalSet.Id.Should().Be(firstSet.Id);
        finalRuleIds.Should().Equal(firstRuleIds);
        finalRuleIds.Should().HaveCount(17);

        var transaction = await context.AchTransactions.SingleAsync(x => x.Id == transactionId);
        transaction.Type = TransactionTypeEnum.Debit;
        transaction.TransactionCode = "27";
        await context.SaveChangesAsync();
        var cycle = await context.AchCycles
            .Include(x => x.ClearingHouse)
            .SingleAsync(x => x.Id == transaction.AchCycleId);

        var resolution = await new ProcContrapartidasFunctionalMappingResolver(context)
            .TryResolveAsync(cycle, [transaction], DateTime.UtcNow);
        resolution.Should().NotBeNull();
        resolution!.UsedFallback.Should().BeFalse();
        resolution.Contract.OFIDTX.Should().Be(transaction.Reference);
        resolution.Contract.OFMONDEB.Should().Be(transaction.Amount);
        resolution.Contract.OFDD.Should().Be("D");

        var operationResolver = new TransactionIntegrationOperationResolver(context);
        var operation = await operationResolver.ResolveAsync(transaction);
        var readiness = await new IntegrationMappingReadinessService(context, new IntegrationCatalogService(context))
            .EvaluateAsync(operation);
        readiness.IsReady.Should().BeTrue();
        readiness.Status.Should().Be("ReadyWithWarnings");
        readiness.UsesFallback.Should().BeFalse();
        readiness.Errors.Should().BeEmpty();

        var settingsBootstrapper = new SoapIntegrationSettingsBootstrapper(context, BuildSoapBootstrapConfiguration());
        await settingsBootstrapper.EnsureAsync();
        await settingsBootstrapper.EnsureAsync();
        (await context.SoapIntegrationSettings.CountAsync()).Should().Be(1);
        var settings = await context.SoapIntegrationSettings.AsNoTracking().SingleAsync();
        settings.WscfaachMappingsJson.Should().Contain("Proc_Contrapartidas");
    }

    private static IConfiguration BuildSoapBootstrapConfiguration()
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["SoapIntegrationBootstrap:Enabled"] = "true",
            ["SoapIntegrationBootstrap:DefaultTimeoutSeconds"] = "15",
            ["SoapIntegrationBootstrap:ProcContrapartidas:Endpoint"] = "http://localhost:7083/WSCFAACH.svc",
            ["SoapIntegrationBootstrap:ProcContrapartidas:SoapAction"] = "http://tempuri.org/IWSCFAACH/Proc_Contrapartidas",
            ["SoapIntegrationBootstrap:ProcContrapartidas:OperatingMode"] = "Live",
            ["SoapIntegrationBootstrap:ProcContrapartidas:TimeoutSeconds"] = "15",
            ["SoapIntegrationBootstrap:ProcContrapartidas:Enabled"] = "true",
            ["SoapIntegrationBootstrap:ProcTransacciones:Endpoint"] = "http://localhost:7083/WSCFAACH.svc",
            ["SoapIntegrationBootstrap:ProcTransacciones:SoapAction"] = "http://tempuri.org/IWSCFAACH/Proc_Transacciones",
            ["SoapIntegrationBootstrap:ProcTransacciones:OperatingMode"] = "Live",
            ["SoapIntegrationBootstrap:ProcTransacciones:TimeoutSeconds"] = "15",
            ["SoapIntegrationBootstrap:ProcTransacciones:Enabled"] = "true",
            ["SoapIntegrationBootstrap:RegistrarRespuestaTransaccion:Endpoint"] = "http://localhost:7083/WSAxonRespuestaTransacciones.svc",
            ["SoapIntegrationBootstrap:RegistrarRespuestaTransaccion:SoapAction"] = "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion",
            ["SoapIntegrationBootstrap:RegistrarRespuestaTransaccion:OperatingMode"] = "Live",
            ["SoapIntegrationBootstrap:RegistrarRespuestaTransaccion:TimeoutSeconds"] = "15",
            ["SoapIntegrationBootstrap:RegistrarRespuestaTransaccion:Enabled"] = "true"
        }).Build();

    private static async Task<int> SeedAsync(AchDbContext context)
    {
        var existing = await context.AchTransactions.AsNoTracking()
            .Where(item => item.TransactionExternalId == "MON2-OUT-001")
            .Select(item => (int?)item.Id)
            .SingleOrDefaultAsync();
        if (existing.HasValue) return existing.Value;

        var now = ScenarioNow.AddHours(-2);
        var configuration = new ClearingHouseConfig { TimeZoneId = "America/Bogota" };
        context.Add(configuration);
        await context.SaveChangesAsync();
        var house = new ClearingHouse { Name = "Cámara de prueba", Code = "MON2", OriginCode = "MON2", ClearingHouseId = configuration.Id };
        var source = Institution("CFA local", true, "00001", "001");
        var destination = Institution("Entidad destino de prueba", false, "00002", "002");
        context.AddRange(house, source, destination);
        await context.SaveChangesAsync();
        var cycle = new AchCycle
        {
            Id = "MON2-CYCLE-20260802",
            CycleName = "Ciclo monitor 2",
            ProcessingDate = new DateTime(2026, 8, 2),
            StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(12), CutoffTime = TimeSpan.FromHours(11),
            ClearingHouseId = house.Id, OperationalStatus = AchCycleOperationalStatus.Open
        };
        var batch = new AchBatch
        {
            AchCycleId = cycle.Id, ServiceClassCode = "220", CompanyName = "CFA",
            CompanyIdentification = "MONITOR", OriginOrOdfi = "00000001", EffectiveEntryDate = new DateTime(2026, 8, 2),
            BatchSequenceNumber = 1, CompanyEntryDescriptionId = 1
        };
        context.AddRange(cycle, batch);
        await context.SaveChangesAsync();

        var outgoing = Transaction("MON2-OUT-001", "123456789012345", AchTransactionDirection.Outgoing,
            AchTransactionClassificationStatus.Determined, source.Id, destination.Id, cycle.Id, batch.Id, now);
        var historical = Transaction("MON2-OLD-001", "123456789012346", AchTransactionDirection.Unknown,
            AchTransactionClassificationStatus.Unknown, source.Id, destination.Id, cycle.Id, batch.Id, now.AddMinutes(1));
        context.AchTransactions.AddRange(outgoing, historical);
        await context.SaveChangesAsync();

        context.AchTransactionStateEvents.AddRange(
            new AchTransactionStateEvent
            {
                AchTransactionId = outgoing.Id, FromState = AchTransferStateEnum.Pending, ToState = AchTransferStateEnum.AppliedTacitly,
                Source = AchStateEventSourceEnum.Epr, OccurredAtUtc = new DateTime(2026, 8, 2, 10, 10, 0, DateTimeKind.Utc)
            },
            new AchTransactionStateEvent
            {
                AchTransactionId = outgoing.Id, FromState = AchTransferStateEnum.AppliedTacitly, ToState = AchTransferStateEnum.ReturnedByEpr,
                Source = AchStateEventSourceEnum.Epr, ReasonCode = "R01", ResolvedReasonDescription = "Fondos insuficientes en el contexto de prueba",
                OccurredAtUtc = new DateTime(2026, 8, 2, 11, 0, 0, DateTimeKind.Utc)
            });
        var file1 = File(cycle.Id, house.Id, "SALIDA.001", 1, new DateTime(2026, 8, 2, 10, 20, 0, DateTimeKind.Utc));
        var file2 = File(cycle.Id, house.Id, "SALIDA.002", 2, new DateTime(2026, 8, 2, 10, 30, 0, DateTimeKind.Utc));
        context.AchFileExports.AddRange(file1, file2);
        await context.SaveChangesAsync();
        context.AchFileExportTransactions.AddRange(
            new AchFileExportTransaction { AchFileExportId = file1.Id, AchTransactionId = outgoing.Id, AchCycleId = cycle.Id, AchBatchId = batch.Id, FileSequence = 1, TraceNumber = outgoing.TraceNumber, Amount = outgoing.Amount, IncludedAtUtc = file1.GeneratedAtUtc },
            new AchFileExportTransaction { AchFileExportId = file2.Id, AchTransactionId = outgoing.Id, AchCycleId = cycle.Id, AchBatchId = batch.Id, FileSequence = 1, TraceNumber = outgoing.TraceNumber, Amount = outgoing.Amount, IncludedAtUtc = file2.GeneratedAtUtc });
        await context.SaveChangesAsync();
        return outgoing.Id;
    }

    private static async Task<ScenarioIds> SeedPhase4Async(AchDbContext context)
    {
        var existing = await context.AchTransactions.AsNoTracking()
            .Where(item => item.TransactionExternalId.StartsWith("UAT-F4-MON-SAL-"))
            .Select(item => new { item.Id, item.TransactionExternalId })
            .ToListAsync();
        if (existing.Count > 0)
            return ScenarioIds.From(existing.ToDictionary(item => item.TransactionExternalId, item => item.Id));

        var now = ScenarioNow;
        var configuration = await context.Set<ClearingHouseConfig>().OrderBy(item => item.Id).FirstAsync();
        var house = new ClearingHouse { Name = "ACH Colombia UAT Fase 4", Code = "UATACH", OriginCode = "UATACH", ClearingHouseId = configuration.Id };
        var cenitHouse = new ClearingHouse { Name = "CENIT UAT Fase 4", Code = "UATCENIT", OriginCode = "UATCENIT", ClearingHouseId = configuration.Id };
        var source = Institution("CFA local UAT", true, "10001", "101");
        var destination = Institution("Entidad destino sintetica", false, "10002", "102");
        context.AddRange(house, cenitHouse, source, destination);
        await context.SaveChangesAsync();
        var cycle = new AchCycle
        {
            Id = "UAT-F4-CYCLE-20260802", CycleName = "Ciclo UAT Fase 4", ProcessingDate = new DateTime(2026, 8, 2),
            StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(12), CutoffTime = TimeSpan.FromHours(11),
            ClearingHouseId = house.Id, OperationalStatus = AchCycleOperationalStatus.Open
        };
        var futureCycle = new AchCycle
        {
            Id = "UAT-F4-CYCLE-20260805", CycleName = "Ciclo futuro UAT Fase 4", ProcessingDate = new DateTime(2026, 8, 5),
            StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(12), CutoffTime = TimeSpan.FromHours(11),
            ClearingHouseId = house.Id, OperationalStatus = AchCycleOperationalStatus.Open,
            OriginalProcessingDate = new DateTime(2026, 8, 3), CalendarDeferralReason = "Festivo nacional"
        };
        var achSpecialDateCycle = new AchCycle
        {
            Id = "UAT-F4-ACH-SPECIAL-20260805", CycleName = "Ciclo diferido por fecha especial ACH", ProcessingDate = new DateTime(2026, 8, 5),
            StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(12), CutoffTime = TimeSpan.FromHours(11),
            ClearingHouseId = house.Id, OperationalStatus = AchCycleOperationalStatus.Scheduled,
            OriginalProcessingDate = new DateTime(2026, 8, 4), CalendarDeferralReason = "Fecha especial no operativa de ACH Colombia"
        };
        var cenitSpecialDateCycle = new AchCycle
        {
            Id = "UAT-F4-CENIT-SPECIAL-20260805", CycleName = "Ciclo diferido por fecha especial CENIT", ProcessingDate = new DateTime(2026, 8, 5),
            StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(12), CutoffTime = TimeSpan.FromHours(11),
            ClearingHouseId = cenitHouse.Id, OperationalStatus = AchCycleOperationalStatus.Scheduled,
            OriginalProcessingDate = new DateTime(2026, 8, 4), CalendarDeferralReason = "Fecha especial no operativa de CENIT"
        };
        var batch = new AchBatch
        {
            AchCycleId = cycle.Id, ServiceClassCode = "220", CompanyName = "CFA", CompanyIdentification = "UATF4",
            OriginOrOdfi = "00000001", EffectiveEntryDate = new DateTime(2026, 8, 2), BatchSequenceNumber = 1, CompanyEntryDescriptionId = 1
        };
        context.AddRange(cycle, futureCycle, achSpecialDateCycle, cenitSpecialDateCycle, batch);
        await context.SaveChangesAsync();

        AchTransaction Tx(string suffix, string trace, string cycleId, int minute) => Phase4Transaction(
            $"UAT-F4-MON-SAL-{suffix}", trace, source.Id, destination.Id, cycleId, batch.Id, now.AddMinutes(minute));
        var future = Tx("01-FUTURO", "900000000000001", futureCycle.Id, 0);
        var pending = Tx("02-PENDIENTE", "900000000000002", cycle.Id, 1);
        var accepted = Tx("03-ACEPTADA", "900000000000003", cycle.Id, 2);
        var rejected = Tx("04-RECHAZADA", "900000000000004", cycle.Id, 3);
        var returned = Tx("05-DEVUELTA", "900000000000005", cycle.Id, 4);
        var withoutFile = Tx("06-SIN-ARCHIVO", "900000000000006", cycle.Id, 5);
        var technical = Tx("07-ERROR-TECNICO", "900000000000007", cycle.Id, 6);
        var retry = Tx("08-REINTENTO", "900000000000008", cycle.Id, 7);
        var exactFile = Tx("11-ARCHIVO-EXACTO", "900000000000011", cycle.Id, 8);
        var achSpecialDate = Tx("12-ACH-FECHA-ESPECIAL", "900000000000012", achSpecialDateCycle.Id, 9);
        var cenitSpecialDate = Tx("13-CENIT-FECHA-ESPECIAL", "900000000000013", cenitSpecialDateCycle.Id, 10);
        var historical = Transaction("UAT-F4-MON-SAL-HISTORICA-NO-DETERMINADA", "900000000009999", AchTransactionDirection.Unknown,
            AchTransactionClassificationStatus.Unknown, source.Id, destination.Id, cycle.Id, batch.Id, now.AddMinutes(11));
        var fillers = Enumerable.Range(1, 25).Select(index => Tx($"PAG-{index:00}", $"900000000001{index:00}", cycle.Id, 12 + index)).ToArray();
        context.AchTransactions.AddRange([future, pending, accepted, rejected, returned, withoutFile, technical, retry, exactFile,
            achSpecialDate, cenitSpecialDate, historical, .. fillers]);
        await context.SaveChangesAsync();

        context.AchTransactionStateEvents.AddRange(
            StateEvent(accepted.Id, AchTransferStateEnum.Pending, AchTransferStateEnum.AppliedTacitly, now.UtcDateTime.AddMinutes(20)),
            StateEvent(returned.Id, AchTransferStateEnum.Pending, AchTransferStateEnum.AppliedTacitly, now.UtcDateTime.AddMinutes(21)),
            StateEvent(returned.Id, AchTransferStateEnum.AppliedTacitly, AchTransferStateEnum.ReturnedByEpr, now.UtcDateTime.AddMinutes(22), "R01", "Fondos insuficientes en el contexto UAT"));

        var file1 = File(cycle.Id, house.Id, "UAT-F4-SALIDA.001", 1, now.UtcDateTime.AddMinutes(23));
        var file2 = File(cycle.Id, house.Id, "UAT-F4-SALIDA.002", 2, now.UtcDateTime.AddMinutes(24));
        context.AchFileExports.AddRange(file1, file2);
        await context.SaveChangesAsync();
        context.AchFileExportTransactions.AddRange(
            Membership(file1.Id, exactFile, cycle.Id, batch.Id, file1.GeneratedAtUtc),
            Membership(file2.Id, fillers[0], cycle.Id, batch.Id, file2.GeneratedAtUtc));
        AddDispatch(context, pending, cycle, house, batch, [Attempt(1, success: true, code: "00", started: now.UtcDateTime.AddMinutes(25))]);
        AddDispatch(context, rejected, cycle, house, batch, [Attempt(1, rejection: true, code: "R01", started: now.UtcDateTime.AddMinutes(26))]);
        AddDispatch(context, technical, cycle, house, batch, [Attempt(1, technical: true, code: "TIMEOUT", started: now.UtcDateTime.AddMinutes(27))]);
        AddDispatch(context, retry, cycle, house, batch,
            [Attempt(1, technical: true, code: "TIMEOUT", started: now.UtcDateTime.AddMinutes(28)), Attempt(2, success: true, code: "00", started: now.UtcDateTime.AddMinutes(29))]);
        await context.SaveChangesAsync();
        return new ScenarioIds(future.Id, pending.Id, accepted.Id, rejected.Id, returned.Id, withoutFile.Id, technical.Id, retry.Id,
            exactFile.Id, achSpecialDate.Id, cenitSpecialDate.Id);
    }

    private static AchTransaction Phase4Transaction(string externalId, string trace, int sourceId, int destinationId, string cycleId, int batchId, DateTimeOffset createdAt)
    {
        var transaction = Transaction(externalId, trace, AchTransactionDirection.Outgoing, AchTransactionClassificationStatus.Determined,
            sourceId, destinationId, cycleId, batchId, createdAt);
        transaction.State = AchTransferStateEnum.Pending;
        return transaction;
    }

    private static AchTransactionStateEvent StateEvent(int transactionId, AchTransferStateEnum from, AchTransferStateEnum to,
        DateTime occurred, string? code = null, string? description = null) => new()
        {
            AchTransactionId = transactionId, FromState = from, ToState = to, Source = AchStateEventSourceEnum.Epr,
            ReasonCode = code, ResolvedReasonDescription = description, OccurredAtUtc = occurred
        };

    private static AchFileExportTransaction Membership(int fileId, AchTransaction transaction, string cycleId, int batchId, DateTime includedAt)
        => new() { AchFileExportId = fileId, AchTransactionId = transaction.Id, AchCycleId = cycleId, AchBatchId = batchId,
            FileSequence = 1, TraceNumber = transaction.TraceNumber, Amount = transaction.Amount, IncludedAtUtc = includedAt };

    private static void AddDispatch(AchDbContext context, AchTransaction transaction, AchCycle cycle, ClearingHouse house, AchBatch batch,
        IReadOnlyList<ContrapartidaDispatchAttempt> attempts)
    {
        var item = new ContrapartidaDispatchItem
        {
            AchTransactionId = transaction.Id, AchCycleId = cycle.Id, ClearingHouseId = house.Id, AchBatchId = batch.Id,
            State = attempts.Any(attempt => attempt.IsSuccessful) ? ContrapartidaDispatchItemStateEnum.ReportedToContrapartida : ContrapartidaDispatchItemStateEnum.RetryPending,
            AttemptCount = attempts.Count, LastAttemptAtUtc = attempts.Max(attempt => attempt.StartedAtUtc),
            LastSuccessAtUtc = attempts.Where(attempt => attempt.IsSuccessful).Select(attempt => attempt.FinishedAtUtc).LastOrDefault(),
            LastResponseCode = attempts[^1].ExternalResponseCode, LastErrorCode = attempts[^1].ErrorCode,
            LastErrorMessage = attempts[^1].ErrorMessage, LastCorrelationId = attempts[^1].CorrelationId, LastDispatchedBy = "UAT-F4"
        };
        foreach (var attempt in attempts) item.Attempts.Add(attempt);
        context.ContrapartidaDispatchItems.Add(item);
    }

    private static ContrapartidaDispatchAttempt Attempt(int number, bool success = false, bool rejection = false, bool technical = false,
        string code = "", DateTime started = default) => new()
        {
            AttemptNumber = number, StartedAtUtc = started, FinishedAtUtc = started.AddSeconds(1),
            Result = success ? ContrapartidaDispatchAttemptResultEnum.Success : ContrapartidaDispatchAttemptResultEnum.Failed,
            CorrelationId = $"UAT-F4-ATTEMPT-{number}-{code}", TriggeredBy = "UAT-F4", RetryEligible = technical,
            ExternalResponseCode = code, ExternalResponseMessage = rejection ? "Fondos insuficientes" : success ? "Integracion completada" : "Tiempo de espera agotado",
            ErrorCode = technical ? code : string.Empty, ErrorMessage = technical ? "Falla tecnica controlada y sanitizada" : string.Empty,
            RequestPayloadXml = string.Empty, ResponsePayloadXml = string.Empty, SoapMethodName = "Proc_Contrapartidas",
            SoapEndpoint = "simulador-local-controlado", ExecutionMode = "DryRun", DurationMs = 1000, SoapResponseCode = code,
            SoapResponseDescription = rejection ? "Fondos insuficientes" : string.Empty, SoapTechnicalStatus = technical ? "Error tecnico" : "Completado",
            TransportStatus = technical ? IntegrationTransportStatus.TimedOut : IntegrationTransportStatus.Succeeded,
            BusinessStatus = success ? IntegrationResponseBusinessStatus.Success : rejection ? IntegrationResponseBusinessStatus.Rejected : IntegrationResponseBusinessStatus.Unknown,
            RetryAllowed = technical, ProcessedAtUtc = started.AddSeconds(1), IsSuccessful = success,
            IsFunctionalRejection = rejection, IsTechnicalFailure = technical, TechnicalException = string.Empty
        };

    private sealed record ScenarioIds(int FutureCycle, int PendingResponse, int Accepted, int Rejected, int AcceptedReturned,
        int WithoutFile, int TechnicalFailure, int RetrySucceeded, int ExactFile, int AchSpecialDateCycle, int CenitSpecialDateCycle)
    {
        public static ScenarioIds From(IReadOnlyDictionary<string, int> ids) => new(
            ids["UAT-F4-MON-SAL-01-FUTURO"], ids["UAT-F4-MON-SAL-02-PENDIENTE"], ids["UAT-F4-MON-SAL-03-ACEPTADA"],
            ids["UAT-F4-MON-SAL-04-RECHAZADA"], ids["UAT-F4-MON-SAL-05-DEVUELTA"], ids["UAT-F4-MON-SAL-06-SIN-ARCHIVO"],
            ids["UAT-F4-MON-SAL-07-ERROR-TECNICO"], ids["UAT-F4-MON-SAL-08-REINTENTO"], ids["UAT-F4-MON-SAL-11-ARCHIVO-EXACTO"],
            ids["UAT-F4-MON-SAL-12-ACH-FECHA-ESPECIAL"], ids["UAT-F4-MON-SAL-13-CENIT-FECHA-ESPECIAL"]);
    }

    private static FinancialInstitution Institution(string name, bool source, string routing, string transit)
    {
        var institution = new FinancialInstitution { Name = name, IsDefaultSource = source, RoutingNumber = routing, TransitCode = transit, Status = FinancialInstitutionStatus.Active };
        institution.CalculateCheckDigit();
        return institution;
    }

    private static AchTransaction Transaction(string externalId, string trace, AchTransactionDirection direction,
        AchTransactionClassificationStatus classification, int sourceId, int destinationId, string cycleId, int batchId, DateTimeOffset createdAt)
        => new()
        {
            Amount = 125000.50m, TransactionExternalId = externalId, Reference = externalId, Type = TransactionTypeEnum.Credit,
            TransactionCode = "22", ServiceClassCode = "220", CompanyEntryDescriptionId = 1, CompanyName = "CFA", CompanyIdentification = "MONITOR",
            OriginatingDFI = "00000001", ReceivingDFI = "00000002", TraceNumber = trace, TraceSequenceNumber = 1,
            EffectiveEntryDate = new DateTime(2026, 8, 2), Direction = direction, Origin = AchTransactionOrigin.Cfa,
            MonetaryIntegrationRoute = direction == AchTransactionDirection.Outgoing ? AchMonetaryIntegrationRoute.ProcContrapartidas : AchMonetaryIntegrationRoute.ManualReview,
            ClassificationStatus = classification, SourceInstitutionWasDefaultAtCreation = true, ClassifiedAtUtc = classification == AchTransactionClassificationStatus.Determined ? createdAt.UtcDateTime : null,
            ClassificationVersion = classification == AchTransactionClassificationStatus.Determined ? 1 : 0, State = AchTransferStateEnum.ReturnedByEpr,
            StateChangedAtUtc = createdAt.UtcDateTime, SourceAccountNumber = "0000001111", DestinationAccountNumber = "1234567890",
            SourceInstitutionId = sourceId, DestinationInstitutionId = destinationId, AchCycleId = cycleId, AchBatchId = batchId,
            DiscretionaryData = string.Empty, CreatedAt = createdAt, UpdatedAt = createdAt
        };

    private static AchFileExport File(string cycleId, int houseId, string name, int version, DateTime generated)
        => new()
        {
            AchCycleId = cycleId, ClearingHouseId = houseId, ExportKind = "OUT", FileName = name,
            TotalRecords = 1, TotalTransactions = 1, IsEncrypted = true, GeneratedAtUtc = generated, Version = version,
            LifecycleStatus = AchFileExportLifecycleStatus.Protected
        };

    private static void EnsureConfiguration(DatabaseProvider provider)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(RequiredVariable), "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{RequiredVariable}=true es obligatorio; la prueba no admite omisiones.");
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable(provider))))
            throw new InvalidOperationException($"Falta {ConnectionVariable(provider)}.");
    }

    private static string ConnectionVariable(DatabaseProvider provider) => provider == DatabaseProvider.SqlServer
        ? "OUTGOING_MONITOR_SQLSERVER_CONNECTION_STRING" : "OUTGOING_MONITOR_POSTGRES_CONNECTION_STRING";

    private enum DatabaseProvider { SqlServer, PostgreSql }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider { public override DateTimeOffset GetUtcNow() => now; }

    private sealed class DatabaseFixture : IAsyncDisposable
    {
        private readonly string _databaseName;
        private readonly string _connectionString;
        private readonly string _adminConnectionString;
        private DatabaseFixture(DatabaseProvider provider, string databaseName, string connectionString, string adminConnectionString)
        { Provider = provider; _databaseName = databaseName; _connectionString = connectionString; _adminConnectionString = adminConnectionString; }
        public DatabaseProvider Provider { get; }

        public static async Task<DatabaseFixture> CreateAsync(DatabaseProvider provider)
        {
            var baseConnection = Environment.GetEnvironmentVariable(ConnectionVariable(provider))!;
            var databaseName = $"ach_monitor_{Guid.NewGuid():N}";
            if (provider == DatabaseProvider.SqlServer)
            {
                var admin = new SqlConnectionStringBuilder(baseConnection) { InitialCatalog = "master" };
                await using var connection = new SqlConnection(admin.ConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand(); command.CommandText = $"CREATE DATABASE [{databaseName}]"; await command.ExecuteNonQueryAsync();
                var target = new SqlConnectionStringBuilder(baseConnection) { InitialCatalog = databaseName };
                return new DatabaseFixture(provider, databaseName, target.ConnectionString, admin.ConnectionString);
            }
            var postgresAdmin = new NpgsqlConnectionStringBuilder(baseConnection) { Database = "postgres" };
            await using (var connection = new NpgsqlConnection(postgresAdmin.ConnectionString))
            { await connection.OpenAsync(); await using var command = connection.CreateCommand(); command.CommandText = $"CREATE DATABASE \"{databaseName}\""; await command.ExecuteNonQueryAsync(); }
            var targetPostgres = new NpgsqlConnectionStringBuilder(baseConnection) { Database = databaseName };
            return new DatabaseFixture(provider, databaseName, targetPostgres.ConnectionString, postgresAdmin.ConnectionString);
        }

        public AchDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AchDbContext>();
            if (Provider == DatabaseProvider.SqlServer) options.UseSqlServer(_connectionString, sql => sql.MigrationsAssembly("Cfa.ACHInterbank.Persistence.Migrations.SqlServer"));
            else options.UseNpgsql(_connectionString);
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            return new AchDbContext(options.Options, timeProvider: new FixedTimeProvider(ScenarioNow));
        }

        public async ValueTask DisposeAsync()
        {
            if (Provider == DatabaseProvider.SqlServer)
            {
                await using var connection = new SqlConnection(_adminConnectionString); await connection.OpenAsync();
                await using var command = connection.CreateCommand(); command.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]"; await command.ExecuteNonQueryAsync(); return;
            }
            await using var postgres = new NpgsqlConnection(_adminConnectionString); await postgres.OpenAsync();
            await using var terminate = postgres.CreateCommand(); terminate.CommandText = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname=@database AND pid<>pg_backend_pid()"; terminate.Parameters.AddWithValue("database", _databaseName); await terminate.ExecuteNonQueryAsync();
            await using var drop = postgres.CreateCommand(); drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\""; await drop.ExecuteNonQueryAsync();
        }
    }
}
