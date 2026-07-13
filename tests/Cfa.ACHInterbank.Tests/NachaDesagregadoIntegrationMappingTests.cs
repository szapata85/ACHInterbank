using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Cfa.ACHInterbank.Persistence.Security.Services;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaDesagregadoIntegrationMappingTests
{
    [Fact]
    public async Task ProcTransaccionesReadiness_CompleteFunctionalSources_IsReady()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishProcTransaccionesMappingAsync();

        var readiness = await new IntegrationMappingReadinessService(fixture.Context, fixture.Catalog)
            .EvaluateAsync(Operation(fixture));

        Assert.True(readiness.IsReady);
        Assert.True(readiness.CanBuildPayload);
        Assert.Empty(readiness.Errors);
    }

    [Fact]
    public async Task EffectiveSettings_HomologatedMapping_IsReadyWithoutBlockingParameters()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishProcTransaccionesMappingAsync();
        var readiness = new IntegrationMappingReadinessService(fixture.Context, fixture.Catalog);
        var service = new SoapIntegrationSettingsService(
            fixture.Context,
            Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }),
            readiness);

        var settings = await service.GetAsync();

        Assert.True(settings.ProcTransaccionesEffectiveSettings!.MappingReady);
        Assert.Null(settings.ProcTransaccionesEffectiveSettings.MappingIssueCode);
        Assert.Empty(settings.ProcTransaccionesEffectiveSettings.BlockingParameters);
    }

    [Theory]
    [InlineData(true, "BCORECEP")]
    [InlineData(false, "BCOORIG")]
    public async Task ProcTransaccionesReadiness_MissingCoreBankCode_Blocks(bool receiver, string parameter)
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishProcTransaccionesMappingAsync();
        var institution = await fixture.Context.FinancialInstitutions.SingleAsync(x => x.Id == (receiver ? 1 : 2));
        institution.CoreBankCode = null;
        await fixture.Context.SaveChangesAsync();

        var readiness = await new IntegrationMappingReadinessService(fixture.Context, fixture.Catalog)
            .EvaluateAsync(Operation(fixture));

        Assert.False(readiness.IsReady);
        Assert.Contains(readiness.Errors, x => x.Contains(parameter, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProcTransacciones_CenitCanonicalCycle_MapsCompensationIdTwo()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 2, Name = "CENIT", Code = "CENIT", OriginCode = "011111111", ClearingHouseId = 1
        });
        fixture.Cycle.ClearingHouseId = 2;
        fixture.Queue.ClearingHouseId = 2;
        await fixture.Context.SaveChangesAsync();
        await fixture.PublishProcTransaccionesMappingAsync();

        var result = await new ProcTransaccionesRequestMapper(fixture.Context).ResolveAsync(
            fixture.Queue, fixture.Ingestion, fixture.Classification, fixture.Transaction, fixture.Cycle, DateTime.UtcNow);

        Assert.Equal("2", result.Contract.Parameters["IDCAMCOMPE"]);
        Assert.Equal("32", result.Contract.Parameters["TIPTRAN"]);
        Assert.Equal("283", result.Contract.Parameters["BCORECEP"]);
        Assert.Equal("999", result.Contract.Parameters["BCOORIG"]);
        Assert.Equal("20260713", result.Contract.Parameters["FECEFEC"]);
        Assert.Equal("V", result.Contract.Parameters["DISCRE"]);
        Assert.Equal("1234567", result.Contract.Parameters["IDTRAN"]);
        Assert.Equal("000001", result.Contract.Parameters["IDLOTE"]);
        Assert.Equal("0", result.Contract.Parameters["IREVER"]);
        Assert.Equal(string.Empty, result.Contract.Parameters["NCTAORIG"]);
        Assert.Equal(77, result.Contract.Parameters["DESTRAN"].Length);
        Assert.Equal(result.Contract.Parameters["DESTRAN"], result.Contract.Parameters["INFPAG"]);
    }

    [Theory]
    [InlineData("0000001", "000001")]
    [InlineData("0000000", "000000")]
    public void FunctionalBatchId_ValidSevenDigitBatch_FormatsD6(string input, string expected)
        => Assert.Equal(expected, ProcTransaccionesRequestMapper.ToFunctionalBatchId(input));

    [Theory]
    [InlineData("00A0001")]
    [InlineData("1000000")]
    public void FunctionalBatchId_InvalidBatch_Blocks(string input)
        => Assert.Throws<InvalidOperationException>(() => ProcTransaccionesRequestMapper.ToFunctionalBatchId(input));

    [Fact]
    public void PaymentInformationBuilder_BuildsSingleSeventySevenCharacterValue()
    {
        var result = ProcTransaccionesPaymentInformationBuilder.Build(
            "E2ECENIT01", "CREDITOE2E", Fixture.PaymentRelatedInformation());

        Assert.Equal(77, result.Length);
        Assert.StartsWith("00000E2ECENIT01  CREDITOE2E", result, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MappingSourceCatalog_ShouldExpose_NachaDesagregadoSources()
    {
        await using var fixture = await Fixture.CreateAsync();

        var source = await fixture.Catalog.GetSourceCatalogAsync(null);
        var entityNames = source.Select(x => x.EntityName).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var fieldPaths = source.Select(x => x.FieldPath).ToList();

        Assert.Contains(nameof(NachaHeader), entityNames);
        Assert.Contains(nameof(BatchHeader), entityNames);
        Assert.Contains(nameof(EntryDetail), entityNames);
        Assert.Contains(nameof(AddendaRecord), entityNames);
        Assert.Contains(nameof(BatchControl), entityNames);
        Assert.Contains(nameof(FileControl), entityNames);
        Assert.Contains("entryDetails.sequenceNumber", fieldPaths);
        Assert.Contains("batchHeaders.companyId", fieldPaths);
        Assert.Contains("nachaHeaders.immediateOrigin", fieldPaths);
        Assert.DoesNotContain(fieldPaths, x => x.Contains("select ", StringComparison.OrdinalIgnoreCase) || x.Contains(" from ", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(fieldPaths, x => x.Contains("password", StringComparison.OrdinalIgnoreCase) || x.Contains("token", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ProcTransacciones_ShouldMapFields_FromNachaDesagregado()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishProcTransaccionesMappingAsync();

        var mapper = new ProcTransaccionesRequestMapper(fixture.Context);

        var resolution = await mapper.ResolveAsync(
            fixture.Queue,
            fixture.Ingestion,
            fixture.Classification,
            fixture.Transaction,
            fixture.Cycle,
            new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc));

        Assert.Equal("32", resolution.Contract.Parameters["TIPTRAN"]);
        Assert.Equal("4321.50", resolution.Contract.Parameters["MONTO"]);
        Assert.Equal("1234567", resolution.Contract.Parameters["IDTRAN"]);
        Assert.Equal("Banco Externo UAT", resolution.Contract.Parameters["NORIG"]);
        Assert.Equal(77, resolution.Contract.Parameters["DESTRAN"].Length);
        Assert.Equal(resolution.Contract.Parameters["DESTRAN"], resolution.Contract.Parameters["INFPAG"]);
        Assert.Equal("283", resolution.Contract.Parameters["BCORECEP"]);
        Assert.Equal("999", resolution.Contract.Parameters["BCOORIG"]);
        Assert.Equal("000001", resolution.Contract.Parameters["IDLOTE"]);
        Assert.Equal("1", resolution.Contract.Parameters["IDCAMCOMPE"]);
        Assert.Equal("32", resolution.Contract.SourceValues!["transaction.transactionCode"]);
        Assert.Equal("1234567", resolution.Contract.SourceValues!["transaction.traceSequenceNumber"]);
    }

    [Fact]
    public async Task ProcTransacciones_ShouldResolveAddenda_BySevenDigitEntrySequenceSuffix()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishProcTransaccionesMappingAsync();

        fixture.Classification.AddendaRecordId = null;
        var addenda = await fixture.Context.AddendaRecords.SingleAsync(x => x.AddendaID == 602);
        addenda.EntryDetailSequenceNumber = "1234567";
        addenda.PaymentRelatedInformation = Fixture.PaymentRelatedInformation();
        await fixture.Context.SaveChangesAsync();

        var mapper = new ProcTransaccionesRequestMapper(fixture.Context);
        var resolution = await mapper.ResolveAsync(
            fixture.Queue,
            fixture.Ingestion,
            fixture.Classification,
            fixture.Transaction,
            fixture.Cycle,
            new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc));

        Assert.Equal(77, resolution.Contract.Parameters["INFPAG"].Length);
    }

    [Fact]
    public async Task ProcTransacciones_ShouldPersistTrace_WithNachaSourceValues()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishProcTransaccionesMappingAsync();

        var mapper = new ProcTransaccionesRequestMapper(fixture.Context);
        var resolution = await mapper.ResolveAsync(
            fixture.Queue,
            fixture.Ingestion,
            fixture.Classification,
            fixture.Transaction,
            fixture.Cycle,
            new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc));

        var writer = new IntegrationMappingTraceWriter(fixture.Context, fixture.Catalog);
        var operation = new TransactionIntegrationOperationResult(
            fixture.Transaction.Id,
            fixture.Transaction.Reference,
            IntegrationGuaranteeConstants.Wscfaach,
            IntegrationGuaranteeConstants.ProcTransacciones,
            IntegrationGuaranteeConstants.MonetaryCreditRequest,
            IntegrationGuaranteeConstants.OutboundRequest,
            "Credito monetario",
            "Entidad financiera externa; CFA receptora",
            true,
            "Credito externo desde NACHA-M desagregado.",
            true,
            []);

        var result = await writer.WriteAsync(operation, resolution.Contract, fixture.Transaction.Id, fixture.Transaction.Reference, "corr-proc-transacciones", true, false);

        Assert.Empty(result.MissingRequiredFields);
        var trace = await fixture.Context.IntegrationMappingTraces.Include(x => x.Entries).SingleAsync(x => x.Id == result.TraceId);
        Assert.False(trace.ExternalTransmission);
        Assert.Contains(trace.Entries, x => x.TargetField == "TIPTRAN" && x.SourceField == "transaction.transactionCode" && x.SourceValueSanitized == "32");
        Assert.Contains(trace.Entries, x => x.TargetField == "IDTRAN" && x.SourceField == "transaction.traceSequenceNumber" && x.SourceValueSanitized == "1234567");
        Assert.Contains(trace.Entries, x => x.TargetField == "INFPAG" && x.SourceField == "procTransacciones.paymentInformation");
    }

    [Fact]
    public async Task ProcTransacciones_DryRun_ShouldGenerateNonEmptySanitizedEnvelope()
    {
        await using var fixture = await Fixture.CreateAsync();
        await fixture.PublishProcTransaccionesMappingAsync();

        var mapper = new ProcTransaccionesRequestMapper(fixture.Context);
        var resolution = await mapper.ResolveAsync(
            fixture.Queue,
            fixture.Ingestion,
            fixture.Classification,
            fixture.Transaction,
            fixture.Cycle,
            new DateTime(2026, 5, 23, 10, 0, 0, DateTimeKind.Utc));

        var envelope = mapper.BuildSoapBody(resolution.Contract);

        Assert.False(string.IsNullOrWhiteSpace(envelope));
        Assert.Contains("Proc_Transacciones", envelope);
        Assert.Contains("1234567", envelope);
        Assert.DoesNotContain("<METODO>", envelope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("NCTAORIG", envelope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ILR", envelope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SEED", envelope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PLACEHOLDER", envelope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Proc_Contrapartidas", envelope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RegistrarRespuestaTransaccion", envelope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PLValidarUsuarioBV", envelope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RTAACH", envelope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RTALOC", envelope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", envelope, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bearer", envelope, StringComparison.OrdinalIgnoreCase);
    }

    private static TransactionIntegrationOperationResult Operation(Fixture fixture)
        => new(
            fixture.Transaction.Id,
            fixture.Transaction.Reference,
            IntegrationGuaranteeConstants.Wscfaach,
            IntegrationGuaranteeConstants.ProcTransacciones,
            IntegrationGuaranteeConstants.MonetaryCreditRequest,
            IntegrationGuaranteeConstants.OutboundRequest,
            "Credito monetario",
            "Entidad externa; CFA receptora",
            true,
            "Crédito entrante CENIT/ACHCOL",
            true,
            []);

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private Fixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
            Catalog = new IntegrationCatalogService(context);
        }

        public AchDbContext Context { get; }
        public IntegrationCatalogService Catalog { get; }
        public IncomingNachaFileIngestion Ingestion { get; private set; } = null!;
        public IncomingNachaEntryClassification Classification { get; private set; } = null!;
        public IncomingNachaDispatchQueue Queue { get; private set; } = null!;
        public AchTransaction Transaction { get; private set; } = null!;
        public AchCycle Cycle { get; private set; } = null!;

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AchDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var fixture = new Fixture(connection, context);
            await fixture.SeedAsync();
            return fixture;
        }

        public async Task PublishProcTransaccionesMappingAsync()
        {
            await new IntegrationMappingBootstrapper(Context).EnsureAsync();
        }

        private async Task SeedAsync()
        {
            await Catalog.GetMethodsAsync();

            Context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
            Context.ClearingHouses.Add(new ClearingHouse { Id = 1, Name = "ACH Colombia", Code = "ACHCOL", OriginCode = "0001283", ClearingHouseId = 1 });
            var cfa = new FinancialInstitution
            {
                Id = 1,
                Name = "Cooperativa Financiera de Antioquia",
                RoutingNumber = "0001",
                TransitCode = "0283",
                IsDefaultSource = true,
                Status = FinancialInstitutionStatus.Active
            };
            cfa.CoreBankCode = "283";
            cfa.CalculateCheckDigit();
            var external = new FinancialInstitution
            {
                Id = 2,
                Name = "Banco Externo UAT",
                RoutingNumber = "9999",
                TransitCode = "0000",
                IsDefaultSource = false,
                Status = FinancialInstitutionStatus.Active
            };
            external.CoreBankCode = "999";
            external.CalculateCheckDigit();
            Context.FinancialInstitutions.AddRange(cfa, external);
            Cycle = new AchCycle
            {
                Id = "NACHA-CYCLE",
                CycleName = "NACHA",
                ClearingHouseId = 1,
                ProcessingDate = new DateTime(2026, 7, 13),
                StartTime = TimeSpan.Zero,
                EndTime = new TimeSpan(23, 59, 0),
                CutoffTime = new TimeSpan(23, 0, 0)
            };
            Context.AchCycles.Add(Cycle);
            Context.AchBatches.Add(new AchBatch { Id = 77, AchCycleId = Cycle.Id, EffectiveEntryDate = Cycle.ProcessingDate, CompanyEntryDescriptionId = 1 });

            Transaction = new AchTransaction
            {
                Id = 701,
                Amount = 4321.50m,
                Type = TransactionTypeEnum.Credit,
                TransactionCode = "32",
                TransactionExternalId = "UAT-NACHA-EXT-701",
                Reference = "UAT-NACHA-EXT-701",
                SourceInstitutionId = 2,
                DestinationInstitutionId = 1,
                SourceAccountNumber = string.Empty,
                DestinationAccountNumber = "0000003202",
                OriginatingDFI = "9999000",
                ReceivingDFI = "0001283",
                TraceNumber = "999900001234567",
                TraceSequenceNumber = 1234567,
                CompanyIdentification = "900999000",
                AchCycleId = Cycle.Id,
                AchBatchId = 77,
                EffectiveEntryDate = Cycle.ProcessingDate,
                State = AchTransferStateEnum.Pending
            };
            Context.AchTransactions.Add(Transaction);

            Ingestion = new IncomingNachaFileIngestion
            {
                Id = Guid.NewGuid(),
                FileName = "9999000.001.1",
                FileHashSha256 = "HASH-UAT",
                FileSize = 1234,
                ContentType = "text/plain",
                UploadedBy = "test",
                CorrelationId = "corr-nacha"
            };
            Context.IncomingNachaFileIngestions.Add(Ingestion);

            Context.NachaHeaders.Add(new NachaHeader
            {
                NachaID = "NACHA-DES-001",
                IncomingNachaFileIngestionId = Ingestion.Id,
                ImmediateOrigin = "9999000",
                ImmediateDestination = "0001283",
                FileIdModifier = "A",
                ReferenceCode = "REF-UAT",
                ClearingHouseId = 1,
                AchCycleId = Cycle.Id
            });
            Context.BatchHeaders.Add(new BatchHeader
            {
                BatchID = 501,
                NachaID = "NACHA-DES-001",
                CompanyId = "900999000",
                CompanyName = "BANCO EXTERNO UAT",
                StandardEntryClassCode = "PPD",
                CompanyEntryDescription = "PAGO UAT",
                EffectiveEntryDate = "260713",
                OriginParticipantEntityCode = "9999000",
                BatchNumber = 1
            });
            Context.EntryDetails.Add(new EntryDetail
            {
                EntryDetailID = 601,
                NachaID = "NACHA-DES-001",
                TransactionCode = "32",
                ReceivingParticipantEntityCode = "0001283",
                AccountNumber = "0000003202",
                Amount = 4321.50m,
                RecipIdNumber = "900003201",
                RecipUserName = "USUARIO UAT",
                SequenceNumber = "999900001234567",
                BatchNumber = 1
            });
            Context.AddendaRecords.Add(new AddendaRecord
            {
                AddendaID = 602,
                NachaID = "NACHA-DES-001",
                CodeTypeAddendumRecord = "05",
                PaymentRelatedInformation = PaymentRelatedInformation(),
                InvoiceOrAccountNumber = "FAC-UAT-001",
                EntryDetailSequenceNumber = "999900001234567"
            });
            Context.BatchControls.Add(new BatchControl
            {
                BatchControlID = 603,
                NachaID = "NACHA-DES-001",
                EntryAddendaCount = 2,
                EntryHash = 1283,
                TotalCreditAmount = 4321.50m,
                TotalDebitAmount = 0m
            });
            Context.FileControls.Add(new FileControl
            {
                FileControlID = 604,
                NachaID = "NACHA-DES-001",
                BatchCount = 1,
                BlockCount = 1,
                EntryAddendaCount = 2,
                EntryHash = 1283,
                TotalCreditAmount = 4321.50m,
                TotalDebitAmount = 0m
            });

            Classification = new IncomingNachaEntryClassification
            {
                Id = Guid.NewGuid(),
                IncomingNachaFileIngestionId = Ingestion.Id,
                EntryDetailId = 601,
                AddendaRecordId = 602,
                FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante,
                EligibilityStatus = IncomingNachaEligibilityStatus.Elegible,
                RequiresLink = true
            };
            Context.IncomingNachaEntryClassifications.Add(Classification);
            var link = new IncomingNachaTransactionLink
            {
                Id = Guid.NewGuid(),
                IncomingNachaFileIngestionId = Ingestion.Id,
                EntryDetailId = 601,
                AddendaRecordId = 602,
                AchTransactionId = Transaction.Id,
                LinkType = IncomingNachaLinkType.ExactTrace15,
                ConfidenceScore = 1,
                LinkedBy = "test",
                IsFinal = true
            };
            Context.IncomingNachaTransactionLinks.Add(link);
            Queue = new IncomingNachaDispatchQueue
            {
                Id = Guid.NewGuid(),
                IncomingNachaFileIngestionId = Ingestion.Id,
                IncomingNachaEntryClassificationId = Classification.Id,
                IncomingNachaTransactionLinkId = link.Id,
                AchTransactionId = Transaction.Id,
                AchCycleId = Cycle.Id,
                ClearingHouseId = 1,
                OperationalDate = Cycle.ProcessingDate,
                QueueStatus = IncomingNachaDispatchQueueStatus.Queued,
                Priority = 1,
                IdempotencyDispatchKey = "queue-uat"
            };
            Context.IncomingNachaDispatchQueue.Add(Queue);
            await Context.SaveChangesAsync();
        }

        public static string PaymentRelatedInformation()
            => "PAGO-SEGMENTO-UAT-0001".PadRight(24)
                + "COMPLEMENTO-UAT-000001".PadRight(24)
                + new string(' ', 32);

        private static string? SourcePathFor(string parameterPath)
            => parameterPath switch
            {
                "TIPTRAN" => "entryDetails.transactionCode",
                "BCORECEP" => "nachaHeaders.immediateDestination",
                "BCOORIG" => "nachaHeaders.immediateOrigin",
                "NORIG" => "batchHeaders.companyName",
                "NCTAORIG" => "batchHeaders.companyId",
                "IDORIG" => "batchHeaders.companyId",
                "DESTRAN" => "batchHeaders.companyEntryDescription",
                "FECEFEC" => "batchHeaders.effectiveEntryDate",
                "NCTARECEP" => "entryDetails.accountNumber",
                "MONTO" => "entryDetails.amount",
                "NRECEP" => "entryDetails.recipUserName",
                "IDRECEP" => "entryDetails.recipIdNumber",
                "INFPAG" => "addendaRecords.infofromOriginator",
                "IDTRAN" => "entryDetails.sequenceNumber",
                "IDLOTE" => "batchHeaders.batchNumber",
                "REGLOTE" => "batchControls.entryAddendaCount",
                "LIBRE1" => "fileControls.blockCount",
                _ => null
            };

        private static IntegrationSourceKindEnum SourceKindFor(string? sourcePath)
        {
            if (sourcePath is null) return IntegrationSourceKindEnum.Constant;
            if (sourcePath.StartsWith("nachaHeaders.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.NachaHeader;
            if (sourcePath.StartsWith("batchHeaders.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.BatchHeader;
            if (sourcePath.StartsWith("entryDetails.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.EntryDetail;
            if (sourcePath.StartsWith("addendaRecords.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.AddendaRecord;
            if (sourcePath.StartsWith("batchControls.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.BatchControl;
            if (sourcePath.StartsWith("fileControls.", StringComparison.OrdinalIgnoreCase)) return IntegrationSourceKindEnum.FileControl;
            return IntegrationSourceKindEnum.Constant;
        }

        private static string DefaultFor(IntegrationMethodParameter parameter)
            => parameter.ParameterPath switch
            {
                "TREG" => "6",
                "DISCRE" => "UAT",
                "CONV" => "CNV-UAT",
                "PROD" => "ACH",
                "IREVER" => "0",
                "LIBRE" => "UAT",
                "IDCAMCOMPE" => "1",
                "DIRECCIONIP" => "127.0.0.1",
                "RTAACH" => "PENDING",
                "RTALOC" => "PENDING",
                _ => parameter.DataType.ToLowerInvariant() switch
                {
                    "int" or "long" => "1",
                    "decimal" or "double" or "float" => "1.00",
                    _ => "UAT"
                }
            };

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
