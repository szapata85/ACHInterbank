using System.Text;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Implementation;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchPreproductionCertificationTests
{
    [Fact]
    public async Task BatchResolver_RejectsTransactionsWhenResolvedCycleIsClosed()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedReferenceData(context);
        SeedInstitutions(context);
        var companyEntryDescriptionId = SeedCompanyEntryDescription(context, "PAGOS PSE");
        var fixedNow = new DateTimeOffset(2026, 03, 23, 12, 00, 00, TimeSpan.Zero);

        context.AchCycles.Add(new AchCycle
        {
            Id = "cycle-closed",
            CycleName = "CICLO-1",
            ProcessingDate = fixedNow.Date.AddDays(-1),
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(10, 0, 0),
            CutoffTime = new TimeSpan(10, 0, 0),
            ClearingHouseId = 1
        });
        await context.SaveChangesAsync();

        var routing = new Mock<IRoutingStrategyService>();
        routing
            .Setup(x => x.ResolveClearingHouseForTransactionAsync(2, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("cycle-closed");

        var batchRepo = new AchBatchRepository(context);
        var resolver = new BatchResolver(context, batchRepo, routing.Object, new FixedTimeProvider(fixedNow));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveAsync(new AchTransactionRequestData
        {
            Amount = 1000m,
            Reference = "REF-CERRADO",
            Type = TransactionTypeEnum.Credit,
            AccountType = AccountTypeEnum.Checking,
            DestinationInstitutionId = 2,
            SourceAccountNumber = "111122223333",
            DestinationAccountNumber = "999988887777",
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = companyEntryDescriptionId
        }, CancellationToken.None));

        Assert.Contains("está cerrado", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(TransactionTypeEnum.Credit, "22", false, 1500, "900000001", "CLIENTE CREDITO", "PAGOS PSE", "123456780000001")]
    [InlineData(TransactionTypeEnum.Debit, "27", false, 2500, "900000002", "CLIENTE DEBITO", "RECAUDOS", "123456780000002")]
    [InlineData(TransactionTypeEnum.Prenotification, "23", true, 0, "", "CLIENTE PRENOTE", "PAGOS PSE", "123456780000003")]
    [InlineData(TransactionTypeEnum.Reversal, "27", false, 4100, "900000004", "CLIENTE REVERSO", "REVERSO", "123456780000004")]
    public async Task LegacyGoldenScenario_ShouldBeBlockedForLiveAndNotCountAsCertification(
        TransactionTypeEnum type,
        string transactionCode,
        bool isPrenotification,
        decimal amount,
        string recipientIdNumber,
        string receiverName,
        string batchDescription,
        string traceNumber)
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        await SeedNachaCertificationScenarioAsync(context, type, transactionCode, isPrenotification, amount, recipientIdNumber, receiverName, batchDescription, traceNumber);

        var holidayService = new Mock<IBankHoliday>();
        holidayService.Setup(x => x.GetHolidays(It.IsAny<int>())).Returns([]);
        var loader = new NachaDataLoader(context);
        var validation = new NachaTransactionValidationService(context, holidayService.Object, CreatePermissivePrerequisitePolicy());
        var renderer = new NachaFixedWidthRecordRenderer();
        var recordDataProvider = new NachaRecordDataProvider(context);
        var builder = new NachaFileBuilder(
            context,
            holidayService.Object,
            loader,
            validation,
            renderer,
            recordDataProvider,
            new NachaSemanticValidator(),
            generationOptions: Options.Create(new NachaGenerationOptions { Mode = "LEGACY", ExecutionScope = "LIVE" }));

        var exception = await Assert.ThrowsAsync<NachaGenerationException>(
            () => builder.BuildNachaFileAsync([100], CancellationToken.None));

        Assert.Equal("NACHA_LIVE_OFFICIAL_MODE_REQUIRED", exception.Code);
        Assert.Equal("ACHCOL-GENERATION-FAIL-CLOSED", exception.RuleId);
    }

    [Fact]
    public async Task GenerateReturnsFileAsync_MatchesGoldenMasterForDev14Return()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        await SeedReturnCertificationScenarioAsync(context);

        var fixedNow = new DateTimeOffset(2026, 03, 23, 11, 45, 00, TimeSpan.Zero);
        var eligibility = new Mock<IAchReturnEligibilityService>();
        eligibility.Setup(x => x.EvaluateOutgoingReturnAsync(It.IsAny<AchReturnEligibilityRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AchReturnEligibilityRequest req, CancellationToken _) => new AchReturnEligibilityResult(true, req.ReturnReasonCode.Trim().ToUpperInvariant(), 1, "Debit", "Pending", []));
        var service = new AchReturnsService(context, new FixedTimeProvider(fixedNow), new AchRegulatoryCatalogService(context), eligibility.Object, new TestReturnGenerationLockService(), externalFileNamePolicy: ReturnOutExternalFileNamePolicyFactory.Create("2345678.001.RET"));

        var response = await service.GenerateReturnsFileAsync(
            new GenerateReturnsFileRequest("cycle-ret", [new ReturnSelectionItemDto(501, "DEV14")]),
            CancellationToken.None);

        var expected = BuildExpectedReturnGoldenMaster(fixedNow.UtcDateTime);
        Assert.Contains("A094101ACH-RET", expected, StringComparison.Ordinal);

        Assert.Equal("2345678.001.RET", response.FileName);
        Assert.Equal(expected, Encoding.UTF8.GetString(response.Content));
    }

    [Fact]
    public void ClearingHouseStrategies_KeepSensitiveRulesSeparatedBetweenAchAndCenit()
    {
        var achStrategy = new AchClearingHouseStrategy();
        var cenitStrategy = new CenitClearingHouseStrategy();

        var reversal = new AchTransaction
        {
            Type = TransactionTypeEnum.Reversal,
            Amount = 4100m,
            OriginalTraceRef = "123456780000111"
        };

        var lowValueDebit = new AchTransaction
        {
            Type = TransactionTypeEnum.Debit,
            Amount = 900m
        };

        Assert.True(achStrategy.ValidateTransaction(reversal));
        Assert.False(cenitStrategy.ValidateTransaction(reversal));
        Assert.True(achStrategy.ValidateTransaction(lowValueDebit));
        Assert.False(cenitStrategy.ValidateTransaction(lowValueDebit));
        Assert.Equal("12345678.3.1", cenitStrategy.BuildFileName(new AchCycle
        {
            CycleName = "CICLO-3",
            ProcessingDate = new DateTime(2026, 03, 23),
            ClearingHouse = new ClearingHouse { OriginCode = "12345678" }
        }, new DateTime(2026, 03, 23, 12, 00, 00, DateTimeKind.Utc)));
    }

    [Fact]
    public void AddExternal_RequiresJwtSecretFromSecureConfigurationSource()
    {
        const string envKey = "appSettings__tokenManager__secretKetJwt";
        var previous = Environment.GetEnvironmentVariable(envKey);
        Environment.SetEnvironmentVariable(envKey, null);

        try
        {
            var services = new ServiceCollection();
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["appSettings:tokenManager:issuerJwt"] = "issuer.test",
                    ["appSettings:tokenManager:audienceJwt"] = "audience.test",
                    ["appSettings:tokenManager:secretKetJwt"] = ""
                })
                .Build();

            var ex = Assert.Throws<InvalidOperationException>(() => services.AddExternal(configuration));
            Assert.Contains("secretKetJwt", ex.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envKey, previous);
        }
    }

    private static async Task SeedNachaCertificationScenarioAsync(
        AchDbContext context,
        TransactionTypeEnum type,
        string transactionCode,
        bool isPrenotification,
        decimal amount,
        string recipientIdNumber,
        string receiverName,
        string batchDescription,
        string traceNumber)
    {
        SeedReferenceData(context);
        SeedInstitutions(context);
        var companyEntryDescriptionId = SeedCompanyEntryDescription(context, batchDescription);
        await new NachaLayoutSeeder(context).SeedAsync();

        var cycle = new AchCycle
        {
            Id = "cycle-cert",
            CycleName = "CICLO-1",
            ProcessingDate = new DateTime(2026, 03, 23),
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 59, 0),
            ClearingHouseId = 1
        };

        context.AchCycles.Add(cycle);
        context.NachaHeaders.Add(new NachaHeader
        {
            NachaID = "header-cert",
            AchCycleId = cycle.Id,
            PriorityCode = "01",
            ImmediateDestination = "000101006",
            ImmediateOrigin = "123456780",
            FileCreationDate = "2026-03-23",
            FileCreationTime = "10:15",
            FileIdModifier = "A",
            RecordSize = "106",
            BlockingFactor = "10",
            FormatCode = "1",
            ImmediateDestinationName = "ACH COLOMBIA",
            ImmediateOriginName = "BANCO ORIGEN",
            ReferenceCode = "1"
        });

        SeedCustomer(context, receiverName, recipientIdNumber, "999988887777");

        if (!isPrenotification)
        {
            context.AchCycles.Add(new AchCycle
            {
                Id = "cycle-prenote",
                CycleName = "CICLO-0",
                ProcessingDate = new DateTime(2026, 03, 18),
                StartTime = TimeSpan.Zero,
                EndTime = new TimeSpan(23, 59, 0),
                CutoffTime = new TimeSpan(23, 59, 0),
                ClearingHouseId = 1
            });

            context.AchBatches.Add(new AchBatch
            {
                Id = 99,
                AchCycleId = "cycle-prenote",
                ServiceClassCode = transactionCode is "27" or "37" or "55" ? "225" : "220",
                CompanyName = "EMPRESA DEMO",
                CompanyIdentification = "900123456",
                CompanyEntryDescription = batchDescription,
                CompanyEntryDescriptionId = companyEntryDescriptionId,
                EffectiveEntryDate = new DateTime(2026, 03, 18),
                OriginOrOdfi = "12345678"
            });

            context.AchTransactions.Add(new AchTransaction
            {
                Id = 900,
                Amount = 0m,
                Reference = "PRENOTE-BASE",
                Type = TransactionTypeEnum.Prenotification,
                TransactionCode = transactionCode is "27" or "37" or "55" ? "28" : "23",
                ServiceClassCode = transactionCode is "27" or "37" or "55" ? "225" : "220",
                CompanyEntryDescriptionId = companyEntryDescriptionId,
                CompanyName = "EMPRESA DEMO",
                CompanyIdentification = "900123456",
                OriginatingDFI = "12345678",
                ReceivingDFI = "76543210",
                TraceNumber = "123456780009999",
                TraceSequenceNumber = 9999,
                EffectiveEntryDate = new DateTime(2026, 03, 18),
                AddendaRecordIndicator = true,
                IsPrenotification = true,
                SourceAccountNumber = "111122223333",
                DestinationAccountNumber = "999988887777",
                RecipientIdNumber = recipientIdNumber,
                SourceInstitutionId = 1,
                DestinationInstitutionId = 2,
                AchCycleId = "cycle-prenote",
                AchBatchId = 99,
                Addendas =
                [
                    new AchTransactionAddenda
                    {
                        AddendaType = "05",
                        BusinessType = AchAddendaBusinessType.Credit,
                        Purpose = batchDescription,
                        SequenceNumber = 1
                    }
                ]
            });
        }

        context.AchBatches.Add(new AchBatch
        {
            Id = 100,
            AchCycleId = cycle.Id,
            ServiceClassCode = type is TransactionTypeEnum.Debit or TransactionTypeEnum.Reversal or TransactionTypeEnum.Return ? "225" : "220",
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            CompanyEntryDescription = batchDescription,
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            EffectiveEntryDate = cycle.ProcessingDate,
            OriginOrOdfi = "12345678"
        });

        context.AchTransactions.Add(new AchTransaction
        {
            Id = 500,
            Amount = amount,
            Reference = type == TransactionTypeEnum.Reversal ? "REVERSO-001" : type == TransactionTypeEnum.Debit ? "RECAUDO-001" : type == TransactionTypeEnum.Prenotification ? "PRENOTE-001" : "PAGO-001",
            Type = type,
            TransactionCode = transactionCode,
            ServiceClassCode = type is TransactionTypeEnum.Debit or TransactionTypeEnum.Reversal or TransactionTypeEnum.Return ? "225" : "220",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            OriginatingDFI = "12345678",
            ReceivingDFI = "76543210",
            TraceNumber = traceNumber,
            TraceSequenceNumber = int.Parse(traceNumber[^7..]),
            EffectiveEntryDate = cycle.ProcessingDate,
            AddendaRecordIndicator = true,
            IsPrenotification = isPrenotification,
            OriginalTraceRef = type == TransactionTypeEnum.Reversal ? "123456780000000" : string.Empty,
            RecipientIdNumber = recipientIdNumber,
            SourceAccountNumber = "111122223333",
            DestinationAccountNumber = "999988887777",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchCycleId = cycle.Id,
            AchBatchId = 100,
            Addendas = type switch
            {
                TransactionTypeEnum.Debit => [
                    new AchTransactionAddenda
                    {
                        AddendaType = "05",
                        BusinessType = AchAddendaBusinessType.Debit,
                        CollectorId = "0000000000001",
                        ReceiverCustomerCode = receiverName,
                        ServiceDescription = batchDescription,
                        SequenceNumber = 1
                    }
                ],
                TransactionTypeEnum.Reversal => [
                    new AchTransactionAddenda
                    {
                        AddendaType = "99",
                        BusinessType = AchAddendaBusinessType.Return,
                        ReturnReasonCode = "R01",
                        OriginalTraceNumber = "123456780000000",
                        NewTraceNumber = traceNumber,
                        SequenceNumber = 1
                    }
                ],
                _ => [
                    new AchTransactionAddenda
                    {
                        AddendaType = "05",
                        BusinessType = AchAddendaBusinessType.Credit,
                        Purpose = batchDescription,
                        SequenceNumber = 1
                    }
                ]
            }
        });

        await context.SaveChangesAsync();
    }

    private static async Task SeedReturnCertificationScenarioAsync(AchDbContext context)
    {
        SeedReferenceData(context);
        SeedInstitutions(context);
        SeedCustomer(context, "CLIENTE DEV14", "900000014", "999988887777");
        var companyEntryDescriptionId = SeedCompanyEntryDescription(context, "RECAUDOS");

        context.AchCycles.Add(new AchCycle
        {
            Id = "cycle-ret",
            CycleName = "CICLO-RET-1",
            ProcessingDate = new DateTime(2026, 03, 23),
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 59, 0),
            ClearingHouseId = 1
        });

        context.AchTransactions.Add(new AchTransaction
        {
            Id = 501,
            Amount = 3200m,
            Reference = "PAGO DEV14",
            Type = TransactionTypeEnum.Debit,
            TransactionCode = "27",
            ServiceClassCode = "225",
            CompanyName = "EMPRESA DEMO",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            OriginatingDFI = "12345678",
            ReceivingDFI = "76543210",
            TraceNumber = "123456780000501",
            TraceSequenceNumber = 501,
            EffectiveEntryDate = new DateTime(2026, 03, 23),
            AddendaRecordIndicator = true,
            RecipientIdNumber = "900000014",
            SourceAccountNumber = "111122223333",
            DestinationAccountNumber = "999988887777",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchCycleId = "cycle-ret",
            AchBatch = new AchBatch
            {
                Id = 501,
                AchCycleId = "cycle-ret",
                ServiceClassCode = "225",
                CompanyName = "EMPRESA DEMO",
                CompanyIdentification = "900123456",
                CompanyEntryDescription = "RECAUDOS",
                CompanyEntryDescriptionId = companyEntryDescriptionId,
                EffectiveEntryDate = new DateTime(2026, 03, 23),
                OriginOrOdfi = "12345678"
            }
        });

        var returnClearingHouseId = context.ClearingHouses.Select(x => x.Id).First();
        context.AchReturnCodes.Add(new AchReturnCode
        {
            ClearingHouseId = returnClearingHouseId,
            Code = "DEV14",
            Description = "No consentimiento",
            AppliesToDebit = true,
            AppliesToReturn = true,
            RequiresAddenda = true,
            MaxDaysAllowed = 60,
            IsActive = true
        });
        context.AchReturnPolicies.Add(new AchReturnPolicy
        {
            ClearingHouseId = returnClearingHouseId,
            TransactionType = "Debit",
            AllowedReturnCodesCsv = "DEV14",
            MaxDays = 60,
            RequiredOriginalTransactionState = "Pending",
            RequiresAddenda = true,
            IsActive = true
        });

        await context.SaveChangesAsync();
    }

    private static string BuildExpectedNachaGoldenMaster(
        TransactionTypeEnum type,
        string transactionCode,
        bool isPrenotification,
        decimal amount,
        string recipientIdNumber,
        string receiverName,
        string batchDescription,
        string traceNumber)
    {
        var records = new List<string>
        {
            BuildNachaFileHeader(),
            BuildNachaBatchHeader(type, batchDescription),
            BuildNachaEntryDetail(transactionCode, amount, recipientIdNumber, receiverName, traceNumber),
            BuildNachaAddenda(type, batchDescription, traceNumber, recipientIdNumber, receiverName),
            BuildNachaBatchControl(type, amount),
            BuildNachaFileControl(type, amount)
        };

        while (records.Count < 10)
        {
            records.Add(new string('9', 106));
        }

        return string.Concat(records);
    }

    private static string BuildExpectedReturnGoldenMaster(DateTime nowUtc)
    {
        const string newSequence = "765432100000001";
        const string originalTrace = "123456780000501";
        var records = new List<string>
        {
            BuildReturnHeader(nowUtc),
            BuildReturnBatchHeader(nowUtc),
            BuildReturnEntryDetail(),
            BuildReturnAddenda(originalTrace, newSequence),
            BuildReturnBatchControl(),
            BuildReturnFileControl()
        };

        while (records.Count < 10)
        {
            records.Add(new string('9', 106));
        }

        return string.Concat(records);
    }

    private static string BuildNachaFileHeader()
    {
        var buffer = CreateRecord('1');
        Write(buffer, 2, Num("01", 2));
        Write(buffer, 4, Num("000101006", 10));
        Write(buffer, 14, Num("123456780", 10));
        Write(buffer, 24, "20260323");
        Write(buffer, 32, "1015");
        Write(buffer, 36, "A");
        Write(buffer, 37, "106");
        Write(buffer, 40, "10");
        Write(buffer, 42, "1");
        Write(buffer, 43, Alpha("ACH Colombia", 23));
        Write(buffer, 66, Alpha("BANCO ORIGEN", 23));
        Write(buffer, 89, Alpha("1", 8));
        return new string(buffer);
    }

    private static string BuildNachaBatchHeader(TransactionTypeEnum type, string batchDescription)
    {
        var buffer = CreateRecord('5');
        Write(buffer, 2, type is TransactionTypeEnum.Debit or TransactionTypeEnum.Reversal or TransactionTypeEnum.Return ? "225" : "220");
        Write(buffer, 5, Alpha("EMPRESA DEMO", 16));
        Write(buffer, 21, Alpha(string.Empty, 20));
        Write(buffer, 41, Alpha("900123456", 10));
        var secCode = type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification ? "CCD" : "PPD";
        Write(buffer, 51, Alpha(secCode, 3));
        Write(buffer, 54, Alpha(batchDescription == "PAGOS PSE" ? "PAGOS PSE" : batchDescription, 10));
        Write(buffer, 64, "20260323");
        Write(buffer, 72, "20260323");
        Write(buffer, 80, "   ");
        Write(buffer, 83, "1");
        Write(buffer, 84, Num("12345678", 8));
        Write(buffer, 92, Num("1", 7));
        return new string(buffer);
    }

    private static string BuildNachaEntryDetail(string transactionCode, decimal amount, string recipientIdNumber, string receiverName, string traceNumber)
    {
        var buffer = CreateRecord('6');
        Write(buffer, 2, Num(transactionCode, 2));
        Write(buffer, 4, Num("76543210", 8));
        Write(buffer, 12, DigitoChequeoHelper.CalcularDigitoChequeo("76543210"));
        Write(buffer, 13, Alpha("999988887777", 17));
        Write(buffer, 30, Num(((long)(amount * 100)).ToString(), 18));
        Write(buffer, 48, Alpha(recipientIdNumber, 15));
        Write(buffer, 63, Alpha(receiverName, 22));
        Write(buffer, 85, Alpha(string.Empty, 2));
        Write(buffer, 87, "1");
        Write(buffer, 88, Num(traceNumber, 15));
        return new string(buffer);
    }

    private static string BuildNachaAddenda(TransactionTypeEnum type, string batchDescription, string traceNumber, string recipientIdNumber, string receiverName)
    {
        var buffer = CreateRecord('7');
        if (type is TransactionTypeEnum.Reversal or TransactionTypeEnum.Return)
        {
            Write(buffer, 2, "99");
            Write(buffer, 4, Alpha("R01", 5));
            Write(buffer, 9, Num("123456780000000", 15));
            Write(buffer, 82, Num(traceNumber, 15));
            Write(buffer, 100, Num(traceNumber[^7..], 7));
            return new string(buffer);
        }
        else if (type is TransactionTypeEnum.Debit)
        {
            Write(buffer, 2, "05");
            Write(buffer, 4, Num("0000000000001", 13));
            Write(buffer, 17, Alpha(receiverName, 30));
            Write(buffer, 47, Alpha("RECAUDOS", 15));
        }
        else
        {
            Write(buffer, 2, "05");
            Write(buffer, 21, Alpha(batchDescription, 10));
            Write(buffer, 31, new string('0', 53));
        }

        Write(buffer, 84, "0001");
        Write(buffer, 88, Num(traceNumber[^7..], 7));
        return new string(buffer);
    }

    private static string BuildNachaBatchControl(TransactionTypeEnum type, decimal amount)
    {
        var buffer = CreateRecord('8');
        Write(buffer, 2, type is TransactionTypeEnum.Debit or TransactionTypeEnum.Reversal or TransactionTypeEnum.Return ? "225" : "220");
        Write(buffer, 5, "000002");
        Write(buffer, 11, Num("76543210", 10));
        Write(buffer, 21, type is TransactionTypeEnum.Debit or TransactionTypeEnum.Reversal or TransactionTypeEnum.Return ? Num(((long)(amount * 100)).ToString(), 18) : new string('0', 18));
        Write(buffer, 39, type is TransactionTypeEnum.Debit or TransactionTypeEnum.Reversal or TransactionTypeEnum.Return ? new string('0', 18) : Num(((long)(amount * 100)).ToString(), 18));
        Write(buffer, 57, Alpha("900123456", 10));
        Write(buffer, 67, Alpha(string.Empty, 19));
        Write(buffer, 92, Num("12345678", 8));
        Write(buffer, 100, Num("1", 7));
        return new string(buffer);
    }

    private static string BuildNachaFileControl(TransactionTypeEnum type, decimal amount)
    {
        var buffer = CreateRecord('9');
        Write(buffer, 2, "000001");
        Write(buffer, 8, "000001");
        Write(buffer, 14, "00000002");
        Write(buffer, 22, Num("76543210", 10));
        Write(buffer, 32, type is TransactionTypeEnum.Debit or TransactionTypeEnum.Reversal or TransactionTypeEnum.Return ? Num(((long)(amount * 100)).ToString(), 18) : new string('0', 18));
        Write(buffer, 50, type is TransactionTypeEnum.Debit or TransactionTypeEnum.Reversal or TransactionTypeEnum.Return ? new string('0', 18) : Num(((long)(amount * 100)).ToString(), 18));
        return new string(buffer);
    }

    private static string BuildReturnHeader(DateTime nowUtc)
    {
        var buffer = CreateRecord('1');
        Write(buffer, 2, Num("01", 2));
        Write(buffer, 4, Num("000101006", 9));
        Write(buffer, 13, Num("12345678", 9));
        Write(buffer, 22, nowUtc.ToString("yyMMdd"));
        Write(buffer, 28, nowUtc.ToString("HHmm"));
        Write(buffer, 32, "A");
        Write(buffer, 33, "094101");
        Write(buffer, 39, Alpha("ACH-RET", 23));
        Write(buffer, 62, "ACH Colombia".PadRight(23));
        Write(buffer, 85, Alpha("RET", 22));
        return new string(buffer);
    }

    private static string BuildReturnBatchHeader(DateTime nowUtc)
    {
        var buffer = CreateRecord('5');
        Write(buffer, 2, "225");
        Write(buffer, 5, Alpha("DEVOLUCIONES", 16));
        Write(buffer, 21, Alpha(string.Empty, 20));
        Write(buffer, 41, Alpha("BANCORET", 10));
        Write(buffer, 51, "PPD");
        Write(buffer, 54, Alpha("RETORNO", 10));
        Write(buffer, 64, nowUtc.ToString("yyyyMMdd"));
        Write(buffer, 72, nowUtc.ToString("yyyyMMdd"));
        Write(buffer, 80, "000");
        Write(buffer, 83, "1");
        Write(buffer, 84, Num("12345678", 8));
        Write(buffer, 92, "0000001");
        return new string(buffer);
    }

    private static string BuildReturnEntryDetail()
    {
        var buffer = CreateRecord('6');
        Write(buffer, 2, "27");
        Write(buffer, 4, "12345678");
        Write(buffer, 12, "0");
        Write(buffer, 13, Alpha("111122223333", 17));
        Write(buffer, 30, Num("320000", 18));
        Write(buffer, 48, Alpha("900123456", 15));
        Write(buffer, 63, Alpha("EMPRESA DEMO", 22));
        Write(buffer, 85, Alpha("R", 2));
        Write(buffer, 87, "1");
        Write(buffer, 88, "765432100000001");
        return new string(buffer);
    }

    private static string BuildReturnAddenda(string originalTrace, string newSequence)
    {
        var buffer = CreateRecord('7');
        Write(buffer, 2, "99");
        Write(buffer, 4, Alpha("DEV14", 5));
        Write(buffer, 9, Num(originalTrace, 15));
        Write(buffer, 82, Num(newSequence, 15));
        Write(buffer, 100, "0000001");
        return new string(buffer);
    }

    private static string BuildReturnBatchControl()
    {
        var buffer = CreateRecord('8');
        Write(buffer, 2, "225");
        Write(buffer, 5, "000002");
        Write(buffer, 11, Num("12345678", 10));
        Write(buffer, 21, Num("320000", 18));
        Write(buffer, 39, new string('0', 18));
        Write(buffer, 57, Alpha("BANCORET", 10));
        Write(buffer, 92, Num("12345678", 8));
        Write(buffer, 100, "0000001");
        return new string(buffer);
    }

    private static string BuildReturnFileControl()
    {
        var buffer = CreateRecord('9');
        Write(buffer, 2, "000001");
        Write(buffer, 8, "000001");
        Write(buffer, 14, "00000002");
        Write(buffer, 22, Num("12345678", 10));
        Write(buffer, 32, Num("320000", 18));
        Write(buffer, 50, new string('0', 18));
        return new string(buffer);
    }

    private static char[] CreateRecord(char recordType)
    {
        var buffer = new string(' ', 106).ToCharArray();
        buffer[0] = recordType;
        return buffer;
    }

    private static void Write(char[] buffer, int startPosition, string value)
    {
        value.CopyTo(0, buffer, startPosition - 1, value.Length);
    }

    private static string Alpha(string? value, int length)
    {
        var normalized = new string((value ?? string.Empty)
            .Trim()
            .ToUpperInvariant()
            .Where(ch => char.IsLetterOrDigit(ch) || ch == ' ' || ch is '.' or ',' or '-' or '/' or '&')
            .ToArray());

        if (normalized.Length > length)
        {
            normalized = normalized[..length];
        }

        return normalized.PadRight(length, ' ');
    }

    private static string Num(string? value, int length)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length > length)
        {
            digits = digits[^length..];
        }

        return digits.PadLeft(length, '0');
    }

    private static void SeedReferenceData(AchDbContext context)
    {
        if (!context.DocumentTypes.Any())
        {
            context.DocumentTypes.Add(new DocumentTypeCatalog { Code = "CC", Name = "Cédula" });
            context.PersonTypes.AddRange(
                new PersonTypeCatalog { Code = "PJ", Name = "Jurídica" },
                new PersonTypeCatalog { Code = "PN", Name = "Natural" });
            context.GenderTypes.Add(new GenderCatalog { Code = "M", Name = "Masculino" });
        }

        if (!context.ClearingHouseConfigs.Any())
        {
            context.ClearingHouseConfigs.Add(new ClearingHouseConfig
            {
                Id = 1,
                ClearingHouseId = 1,
                                                                                HolidayStrategy = "TEST"
            });
        }

        if (!context.ClearingHouses.Any())
        {
            context.ClearingHouses.Add(new ClearingHouse
            {
                Id = 1,
                Name = "ACH Colombia",
                Code = "ACHCOL",
                OriginCode = "12345678",
                ClearingHouseId = 1
            });
        }

        context.SaveChanges();
    }

    private static void SeedInstitutions(AchDbContext context)
    {
        if (context.FinancialInstitutions.Any())
        {
            return;
        }

        var source = new FinancialInstitution
        {
            Id = 1,
            Name = "Banco Origen",
            RoutingNumber = "1234567",
            TransitCode = "8",
            Status = FinancialInstitutionStatus.Active,
            IsDefaultSource = true
        };
        source.CalculateCheckDigit();

        var destination = new FinancialInstitution
        {
            Id = 2,
            Name = "Banco Destino",
            RoutingNumber = "7654321",
            TransitCode = "0",
            Status = FinancialInstitutionStatus.Active,
            IsDefaultSource = false
        };
        destination.CalculateCheckDigit();

        context.FinancialInstitutions.AddRange(source, destination);
        context.SaveChanges();
    }

    private static int SeedCompanyEntryDescription(AchDbContext context, string term)
    {
        var existingId = context.CompanyEntryDescriptionCatalogs
            .Where(x => x.Term == term)
            .Select(x => x.Id)
            .FirstOrDefault();
        if (existingId != 0)
        {
            return existingId;
        }

        var entry = new CompanyEntryDescriptionCatalog
        {
            Term = term,
            Description = term,
            StandardEntryClassCode = "PPD",
            IsActive = true
        };
        context.CompanyEntryDescriptionCatalogs.Add(entry);
        context.SaveChanges();
        return entry.Id;
    }

    private static void SeedCustomer(AchDbContext context, string receiverName, string recipientIdNumber, string accountNumber)
    {
        if (context.Customers.Any(x => x.DocumentNumber == (string.IsNullOrWhiteSpace(recipientIdNumber) ? accountNumber : recipientIdNumber)))
        {
            return;
        }

        context.Customers.Add(new Customer
        {
            FirstName = receiverName,
            LastName = "",
            PersonType = string.IsNullOrWhiteSpace(recipientIdNumber) ? "PJ" : "PN",
            CompanyName = receiverName,
            DocumentType = "CC",
            DocumentNumber = string.IsNullOrWhiteSpace(recipientIdNumber) ? accountNumber : recipientIdNumber,
            Accounts = [new CustomerAccount { AccountNumber = accountNumber }]
        });
        context.SaveChanges();
    }

    private static SqliteConnection CreateOpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static ITransactionPrerequisitePolicyService CreatePermissivePrerequisitePolicy()
    {
        var policy = new Mock<ITransactionPrerequisitePolicyService>();
        policy
            .Setup(x => x.ValidateForNachaExportAsync(
                It.IsAny<AchTransaction>(),
                It.IsAny<DateTime?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TransactionPrerequisiteValidationResult(true, "OK", "Política satisfecha.", null));
        return policy.Object;
    }

    private static AchDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
        public override long GetTimestamp() => now.UtcDateTime.Ticks;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
