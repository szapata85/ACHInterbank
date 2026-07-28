using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Reflection;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class DifferentialPrenotificationResponseProcessorTests
{
    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldApprovePendingPrenotification_WhenSuccessfulResponseReceived()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishRegistrarRespuestaMappingAsync();

        var result = await fixture.Processor.ProcessAsync(
            fixture.SuccessCommand(),
            fixture.BuildResponse("00", null),
            HomologarRespuestaAchResult.Success(true, 1, 1, "Aprobada", null, null));

        var prenote = await fixture.Context.AchTransactions.Include(x => x.StateEvents).SingleAsync(x => x.Id == fixture.Prenotification.Id);
        var trace = await fixture.Context.IntegrationMappingTraces.Include(x => x.Entries).SingleAsync(x => x.Id == result.TraceId);

        Assert.True(result.Success);
        Assert.Equal(AchTransferStateEnum.Certified, prenote.State);
        Assert.Single(prenote.StateEvents);
        Assert.Contains(trace.Entries, x => x.SourceField == "differentialResponse.idTransaccion" && x.SourceValueSanitized == fixture.TraceNumber);
        Assert.False(result.MonetaryMovementCreated);
        Assert.False(result.BalancesAffected);
        var thirdParty = await fixture.Context.CustomerThirdParties.SingleAsync();
        Assert.Equal(CustomerThirdPartyStatusEnum.Active, thirdParty.Status);
        Assert.Equal("CYCLE", thirdParty.ValidationCycleId);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldRejectPendingPrenotification_WhenRejectedResponseReceived()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishRegistrarRespuestaMappingAsync();

        var result = await fixture.Processor.ProcessAsync(
            fixture.RejectedCommand(),
            fixture.BuildResponse("RJ", "R03"),
            HomologarRespuestaAchResult.Success(true, 2, 2, "Rechazada", "R03", "Cuenta no localizada"));

        var prenote = await fixture.Context.AchTransactions.Include(x => x.StateEvents).SingleAsync(x => x.Id == fixture.Prenotification.Id);
        var stateEvent = Assert.Single(prenote.StateEvents);

        Assert.True(result.Success);
        Assert.Equal(AchTransferStateEnum.ReturnedByEpr, prenote.State);
        Assert.Equal("R03", prenote.ReturnReasonCode);
        Assert.Equal("R03", stateEvent.ReasonCode);
        Assert.False(result.MonetaryMovementCreated);
        Assert.False(result.BalancesAffected);
        var thirdParty = await fixture.Context.CustomerThirdParties.SingleAsync();
        Assert.Equal(CustomerThirdPartyStatusEnum.Rejected, thirdParty.Status);
        Assert.Contains("R03", thirdParty.ValidationMessage);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldNotResolvePrenotification_FromAnotherClearingHouse()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishRegistrarRespuestaMappingAsync();
        var response = fixture.BuildResponse("00", null);
        response.ClearingHouseId = 2;

        var result = await fixture.Processor.ProcessAsync(
            fixture.SuccessCommand(),
            response,
            HomologarRespuestaAchResult.Success(true, 1, 1, "Aprobada", null, null));

        Assert.False(result.Success);
        Assert.Equal("DIFFERENTIAL_RESPONSE_CLEARING_HOUSE_MISMATCH", result.ErrorCode);
        Assert.Equal(AchTransferStateEnum.Pending, fixture.Prenotification.State);
        Assert.Equal(CustomerThirdPartyStatusEnum.Pending,
            (await fixture.Context.CustomerThirdParties.SingleAsync()).Status);
    }

    [Theory]
    [InlineData("00", null, CustomerThirdPartyStatusEnum.Active)]
    [InlineData("RJ", "R03", CustomerThirdPartyStatusEnum.Rejected)]
    public async Task RegistrarRespuestaTransaccion_ShouldResolveCenitPrenotification_WithCenitEvidenceOnly(
        string externalStatus,
        string? reason,
        CustomerThirdPartyStatusEnum expectedStatus)
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishRegistrarRespuestaMappingAsync();
        await fixture.UseCenitAsync();
        var command = (expectedStatus == CustomerThirdPartyStatusEnum.Active
            ? fixture.SuccessCommand()
            : fixture.RejectedCommand()) with { CodigoCamaraCompensacion = "CENIT" };
        var response = fixture.BuildResponse(externalStatus, reason);
        response.CodigoCamaraCompensacion = "CENIT";
        var homologation = expectedStatus == CustomerThirdPartyStatusEnum.Active
            ? HomologarRespuestaAchResult.Success(true, 1, 1, "Aprobada", null, null)
            : HomologarRespuestaAchResult.Success(true, 2, 2, "Rechazada", "R03", "Cuenta no localizada");

        var result = await fixture.Processor.ProcessAsync(command, response, homologation);

        Assert.True(result.Success);
        Assert.Equal(expectedStatus, (await fixture.Context.CustomerThirdParties.SingleAsync()).Status);
        Assert.Equal(1, response.ClearingHouseId);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldPersistTraceEntries_WhenPrenotificationResponseProcessed()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishRegistrarRespuestaMappingAsync();

        var result = await fixture.Processor.ProcessAsync(
            fixture.SuccessCommand(),
            fixture.BuildResponse("00", null),
            HomologarRespuestaAchResult.Success(true, 1, 1, "Aprobada", null, null));

        var trace = await fixture.Context.IntegrationMappingTraces.Include(x => x.Entries).SingleAsync(x => x.Id == result.TraceId);

        Assert.Equal(IntegrationGuaranteeConstants.WsAxon, trace.IntegrationKey);
        Assert.Equal(IntegrationGuaranteeConstants.RegistrarRespuestaTransaccion, trace.OperationKey);
        Assert.Equal(IntegrationGuaranteeConstants.DifferentialResponseNotification, trace.MappingPurpose);
        Assert.False(trace.MonetaryMovementCreated);
        Assert.False(trace.ExternalTransmission);
        Assert.Contains(trace.Entries, x => x.TargetField == "idTransaccion" && x.SourceField == "differentialResponse.idTransaccion");
        Assert.Contains(trace.Entries, x => x.TargetField == "idCanal" && x.SourceField == "differentialResponse.idCanal");
        Assert.DoesNotContain(trace.Entries, x => x.TargetField.StartsWith("ANS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldUseDifferentialResponseFields_ForWsdlTrace()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishRegistrarRespuestaMappingAsync();

        await fixture.Processor.ProcessAsync(
            fixture.SuccessCommand(),
            fixture.BuildResponse("00", null),
            HomologarRespuestaAchResult.Success(true, 1, 1, "Aprobada", null, null));

        var trace = await fixture.Context.IntegrationMappingTraces.Include(x => x.Entries).SingleAsync();

        Assert.Contains(trace.Entries, x => x.SourceField == "differentialResponse.idTransaccion" && x.SourceValueSanitized == fixture.TraceNumber);
        Assert.Contains(trace.Entries, x => x.SourceField == "differentialResponse.idTransaccionServicioExterno" && x.SourceValueSanitized == "1001");
        Assert.Contains(trace.Entries, x => x.SourceField == "differentialResponse.idEstado" && x.SourceValueSanitized == "1");
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldFailControlled_WhenPendingPrenotificationNotFound()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishRegistrarRespuestaMappingAsync();

        var command = fixture.SuccessCommand() with { IdTransaccion = "NO-EXISTE" };
        var response = fixture.BuildResponse("00", null);
        response.IdTransaccion = "NO-EXISTE";
        var result = await fixture.Processor.ProcessAsync(
            command,
            response,
            HomologarRespuestaAchResult.Success(true, 1, 1, "Aprobada", null, null));

        Assert.False(result.Success);
        Assert.Equal("DIFFERENTIAL_RESPONSE_PRENOTIFICATION_NOT_FOUND", result.ErrorCode);
        Assert.True(await fixture.Context.IntegrationMappingTraces.AnyAsync());
        Assert.Equal(AchTransferStateEnum.Pending, (await fixture.Context.AchTransactions.SingleAsync(x => x.Id == fixture.Prenotification.Id)).State);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldHandleDuplicatePrenotificationResponseControlled()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishRegistrarRespuestaMappingAsync();
        fixture.Prenotification.State = AchTransferStateEnum.Certified;
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Processor.ProcessAsync(
            fixture.SuccessCommand(),
            fixture.BuildResponse("00", null),
            HomologarRespuestaAchResult.Success(true, 1, 1, "Aprobada", null, null));

        Assert.False(result.Success);
        Assert.True(result.Duplicate);
        Assert.Equal("DIFFERENTIAL_RESPONSE_ALREADY_PROCESSED", result.ErrorCode);
        Assert.Empty(await fixture.Context.AchTransactionStateEvents.ToListAsync());
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldFailControlled_WhenRequiredMappingMissing()
    {
        await using var fixture = await Fixture.CreateAsync();
        var readiness = new Mock<IIntegrationMappingReadinessService>();
        readiness
            .Setup(x => x.EvaluateAsync(It.IsAny<TransactionIntegrationOperationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegrationMappingReadinessResult(
                IsReady: false,
                Status: "Failed",
                Code: "INTEGRATION_MAPPING_REQUIRED",
                IntegrationKey: IntegrationGuaranteeConstants.WsAxon,
                OperationKey: IntegrationGuaranteeConstants.RegistrarRespuestaTransaccion,
                MappingPurpose: IntegrationGuaranteeConstants.DifferentialResponseNotification,
                MappingDirection: IntegrationGuaranteeConstants.InboundResponse,
                RequiredMappings: 0,
                ActiveMappings: 0,
                MissingRequiredMappings: [],
                InactiveRequiredMappings: [],
                FallbackFields: [],
                RequiredFallbackFields: [],
                UsesFallback: false,
                CanBuildPayload: false,
                Errors: ["No existe IntegrationMappingSet publicado para WSAXON.RegistrarRespuestaTransaccion; no se permite fallback para campos requeridos."],
                Warnings: []));

        var traceWriter = new Mock<IIntegrationMappingTraceWriter>(MockBehavior.Strict);
        var sut = new DifferentialPrenotificationResponseProcessor(
            fixture.Context,
            fixture.OperationResolver,
            readiness.Object,
            traceWriter.Object);

        var result = await sut.ProcessAsync(
            fixture.SuccessCommand(),
            fixture.BuildResponse("00", null),
            HomologarRespuestaAchResult.Success(true, 1, 1, "Aprobada", null, null));

        Assert.False(result.Success);
        Assert.Equal("INTEGRATION_MAPPING_REQUIRED", result.ErrorCode);
        Assert.False(await fixture.Context.IntegrationMappingTraces.AnyAsync());
        Assert.Equal(AchTransferStateEnum.Pending, (await fixture.Context.AchTransactions.SingleAsync(x => x.Id == fixture.Prenotification.Id)).State);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldNotMoveMoney_WhenPrenotificationResponseReceived()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishRegistrarRespuestaMappingAsync();

        var result = await fixture.Processor.ProcessAsync(
            fixture.SuccessCommand(),
            fixture.BuildResponse("00", null),
            HomologarRespuestaAchResult.Success(true, 1, 1, "Aprobada", null, null));

        var prenote = await fixture.Context.AchTransactions.SingleAsync(x => x.Id == fixture.Prenotification.Id);
        Assert.Equal(0m, prenote.Amount);
        Assert.False(result.MonetaryMovementCreated);
        Assert.False(result.BalancesAffected);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldNotAffectBalances_WhenPrenotificationResponseReceived()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishRegistrarRespuestaMappingAsync();

        var result = await fixture.Processor.ProcessAsync(
            fixture.SuccessCommand(),
            fixture.BuildResponse("00", null),
            HomologarRespuestaAchResult.Success(true, 1, 1, "Aprobada", null, null));

        Assert.True(result.Success);
        Assert.False(result.BalancesAffected);
        await fixture.Context.SaveChangesAsync();
        var stateEvent = Assert.Single(await fixture.Context.AchTransactionStateEvents.ToListAsync());
        Assert.Contains("\"monetaryMovementCreated\":false", stateEvent.PayloadJson);
        Assert.Contains("\"balancesAffected\":false", stateEvent.PayloadJson);
    }

    [Fact]
    public void RegistrarRespuestaTransaccion_ShouldNotInvokeIWscfaachSoapClient_WhenPrenotificationResponseReceived()
        => AssertProcessorDoesNotDependOn("Wscfaach");

    [Fact]
    public void RegistrarRespuestaTransaccion_ShouldNotInvokeProcContrapartidas_WhenPrenotificationResponseReceived()
        => AssertProcessorDoesNotDependOn("ProcContrapartidas");

    [Fact]
    public void RegistrarRespuestaTransaccion_ShouldNotInvokeProcTransacciones_WhenPrenotificationResponseReceived()
        => AssertProcessorDoesNotDependOn("ProcTransacciones");

    private static void AssertProcessorDoesNotDependOn(string forbiddenName)
    {
        var processorType = typeof(DifferentialPrenotificationResponseProcessor);
        var dependencyNames = processorType
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .SelectMany(x => x.GetParameters())
            .Select(x => x.ParameterType.FullName ?? x.ParameterType.Name)
            .Concat(processorType
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(x => x.FieldType.FullName ?? x.FieldType.Name));

        Assert.DoesNotContain(dependencyNames, x => x.Contains(forbiddenName, StringComparison.OrdinalIgnoreCase));
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private Fixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
            Catalog = new IntegrationCatalogService(context);
            OperationResolver = new TransactionIntegrationOperationResolver(context);
            Processor = new DifferentialPrenotificationResponseProcessor(
                context,
                OperationResolver,
                new IntegrationMappingReadinessService(context, Catalog),
                new IntegrationMappingTraceWriter(context, Catalog));
        }

        public string TraceNumber => "000128300012345";
        public AchDbContext Context { get; }
        public IntegrationCatalogService Catalog { get; }
        public TransactionIntegrationOperationResolver OperationResolver { get; }
        public DifferentialPrenotificationResponseProcessor Processor { get; }
        public AchTransaction Prenotification { get; private set; } = null!;

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
            var context = new AchDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var fixture = new Fixture(connection, context);
            await fixture.SeedAsync();
            return fixture;
        }

        public ProcesarRespuestaAchCommand SuccessCommand()
            => new(TipoRespuestaAch.Prenota, TraceNumber, "ACH", "0001283", "9999000", "00", null, null, 1, "Canal UAT", 1001, new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc), "corr-prenote-approved");

        public ProcesarRespuestaAchCommand RejectedCommand()
            => SuccessCommand() with { CodigoEstadoExterno = "RJ", CodigoCausalExterna = "R03", DescripcionCausalExterna = "Cuenta no localizada", CorrelationId = "corr-prenote-rejected" };

        public AchResponse BuildResponse(string status, string? reason)
            => new()
            {
                Id = Guid.NewGuid(),
                ClearingHouseId = 1,
                TipoRespuesta = TipoRespuestaAch.Prenota,
                IdTransaccion = TraceNumber,
                CodigoCamaraCompensacion = "ACH",
                CodigoEntidadOrigen = "0001283",
                CodigoEntidadDestino = "9999000",
                CodigoEstadoExterno = status,
                CodigoCausalExterna = reason,
                IdTransaccionServicioExterno = 1001,
                CorrelationId = "corr-prenote",
                FechaRecepcion = new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc),
                FechaCreacion = new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc)
            };

        public async Task PublishRegistrarRespuestaMappingAsync()
        {
            await Catalog.GetMethodsAsync();
            var method = await Context.IntegrationMethods.SingleAsync(x => x.Code == "WSAXON.RegistrarRespuestaTransaccion");
            var parameters = await Context.IntegrationMethodParameters.Where(x => x.MethodId == method.Id && x.IsActive).ToListAsync();
            var set = new IntegrationMappingSet
            {
                MethodId = method.Id,
                Name = "Registrar respuesta diferencial prenote",
                Version = 1,
                Status = IntegrationMappingSetStatusEnum.Published,
                IsActive = true,
                PublishedAtUtc = DateTime.UtcNow,
                PublishedBy = "test"
            };
            Context.IntegrationMappingSets.Add(set);

            foreach (var parameter in parameters)
            {
                var source = SourceFor(parameter.ParameterPath);
                Context.IntegrationMappingRules.Add(new IntegrationMappingRule
                {
                    MappingSetId = set.Id,
                    MethodId = method.Id,
                    ParameterId = parameter.Id,
                    SourceKind = SourceKindFor(source),
                    SourceFieldPath = source,
                    Priority = 1,
                    Enabled = true
                });
            }

            await Context.SaveChangesAsync();
        }

        public async Task UseCenitAsync()
        {
            var clearingHouse = await Context.ClearingHouses.SingleAsync();
            clearingHouse.Code = "CENIT";
            clearingHouse.Name = "CENIT";
            await Context.SaveChangesAsync();
        }

        private async Task SeedAsync()
        {
            await Catalog.GetMethodsAsync();

            Context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
            Context.ClearingHouses.Add(new ClearingHouse { Id = 1, Name = "ACH Colombia", Code = "ACH", OriginCode = "0001283", ClearingHouseId = 1 });
            var cfa = new FinancialInstitution { Id = 1, Name = "CFA", RoutingNumber = "0001", TransitCode = "0283", IsDefaultSource = true, Status = FinancialInstitutionStatus.Active };
            cfa.CalculateCheckDigit();
            var external = new FinancialInstitution { Id = 2, Name = "Banco Externo", RoutingNumber = "9999", TransitCode = "0000", IsDefaultSource = false, Status = FinancialInstitutionStatus.Active };
            external.CalculateCheckDigit();
            Context.FinancialInstitutions.AddRange(cfa, external);
            var cycle = new AchCycle { Id = "CYCLE", CycleName = "Ciclo UAT", ClearingHouseId = 1, ProcessingDate = new DateTime(2026, 5, 23), StartTime = TimeSpan.Zero, EndTime = new TimeSpan(23, 59, 0), CutoffTime = new TimeSpan(23, 0, 0) };
            Context.AchCycles.Add(cycle);
            Context.AchBatches.Add(new AchBatch { Id = 1, AchCycleId = cycle.Id, EffectiveEntryDate = cycle.ProcessingDate, CompanyEntryDescriptionId = 1 });

            Prenotification = new AchTransaction
            {
                Id = 901,
                Amount = 0m,
                Type = TransactionTypeEnum.Prenotification,
                IsPrenotification = true,
                TransactionCode = "28",
                TransactionExternalId = TraceNumber,
                Reference = TraceNumber,
                SourceInstitutionId = 1,
                DestinationInstitutionId = 2,
                SourceAccountNumber = "0000003101",
                DestinationAccountNumber = "0000003102",
                RecipientIdNumber = "900003101",
                OriginatingDFI = "0001283",
                ReceivingDFI = "9999000",
                TraceNumber = TraceNumber,
                CompanyIdentification = "900003101",
                AchCycleId = cycle.Id,
                AchBatchId = 1,
                EffectiveEntryDate = cycle.ProcessingDate,
                State = AchTransferStateEnum.Pending
            };
            Context.AchTransactions.Add(Prenotification);
            var customer = new Customer
            {
                Id = 1,
                FirstName = "Cliente",
                LastName = "UAT",
                PersonType = "PN",
                DocumentType = "CC",
                DocumentNumber = "10000001"
            };
            Context.Customers.Add(customer);
            Context.CustomerThirdParties.Add(new CustomerThirdParty
            {
                Id = 1,
                CustomerId = customer.Id,
                DestinationInstitutionId = external.Id,
                DestinationAccountNumber = Prenotification.DestinationAccountNumber,
                RecipientIdNumber = Prenotification.RecipientIdNumber,
                PrenotificationTransactionId = Prenotification.Id
            });

            var ingestion = new IncomingNachaFileIngestion { Id = Guid.NewGuid(), FileName = "0001283.001.1", FileHashSha256 = "HASH", FileSize = 200, ContentType = "text/plain", UploadedBy = "test", CorrelationId = "corr", ResolvedClearingHouseId = 1, ResolvedAchCycleId = cycle.Id };
            Context.IncomingNachaFileIngestions.Add(ingestion);
            Context.NachaHeaders.Add(new NachaHeader { NachaID = "NACHA-PRENOTE", IncomingNachaFileIngestionId = ingestion.Id, ImmediateOrigin = "0001283", ImmediateDestination = "9999000", FileIdModifier = "A", ReferenceCode = "PRENOTE", ClearingHouseId = 1, AchCycleId = cycle.Id });
            Context.BatchHeaders.Add(new BatchHeader { BatchID = 10, NachaID = "NACHA-PRENOTE", CompanyId = "900003101", CompanyName = "CFA", StandardEntryClassCode = "PPD", CompanyEntryDescription = "PRENOTE", EffectiveEntryDate = "260523", OriginParticipantEntityCode = "0001283", BatchNumber = 1 });
            Context.EntryDetails.Add(new EntryDetail { EntryDetailID = 11, NachaID = "NACHA-PRENOTE", TransactionCode = "28", ReceivingParticipantEntityCode = "9999000", AccountNumber = "0000003102", Amount = 0m, RecipIdNumber = "900003101", RecipUserName = "USUARIO UAT", SequenceNumber = TraceNumber });
            Context.AddendaRecords.Add(new AddendaRecord { AddendaID = 12, NachaID = "NACHA-PRENOTE", OriginalTraceNumber = TraceNumber, EntryDetailSequenceNumber = TraceNumber, InfofromOriginator = "PRENOTE UAT" });
            Context.BatchControls.Add(new BatchControl { BatchControlID = 13, NachaID = "NACHA-PRENOTE", EntryAddendaCount = 2, EntryHash = 1283, TotalCreditAmount = 0m, TotalDebitAmount = 0m });
            Context.FileControls.Add(new FileControl { FileControlID = 14, NachaID = "NACHA-PRENOTE", BatchCount = 1, BlockCount = 1, EntryAddendaCount = 2, EntryHash = 1283, TotalCreditAmount = 0m, TotalDebitAmount = 0m });
            Context.IncomingNachaTransactionLinks.Add(new IncomingNachaTransactionLink { IncomingNachaFileIngestionId = ingestion.Id, EntryDetailId = 11, AddendaRecordId = 12, AchTransactionId = Prenotification.Id, LinkType = IncomingNachaLinkType.ExactTrace15, ConfidenceScore = 1m, IsFinal = true, LinkedBy = "test" });
            await Context.SaveChangesAsync();
        }

        private static string SourceFor(string parameterPath)
            => parameterPath switch
            {
                "idCanal" => "differentialResponse.idCanal",
                "nombreCanal" => "differentialResponse.nombreCanal",
                "idTransaccion" => "differentialResponse.idTransaccion",
                "idEstado" => "differentialResponse.idEstado",
                "causal" => "differentialResponse.codigoCausalExterna",
                "idTransaccionAxon" => "differentialResponse.idTransaccionServicioExterno",
                "descripcionCausal" => "differentialResponse.descripcionCausalExterna",
                _ => "differentialResponse.idTransaccion"
            };

        private static IntegrationSourceKindEnum SourceKindFor(string sourcePath)
        {
            if (sourcePath.StartsWith("batchHeaders.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.BatchHeader;
            if (sourcePath.StartsWith("addendaRecords.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.AddendaRecord;
            if (sourcePath.StartsWith("differentialResponse.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.DifferentialResponse;
            return IntegrationSourceKindEnum.Constant;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
