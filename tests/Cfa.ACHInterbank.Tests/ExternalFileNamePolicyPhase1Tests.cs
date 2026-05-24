using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class ExternalFileNamePolicyPhase1Tests
{
    [Fact]
    public async Task AchBuilder_Generates_RRRRTTT_ZZZ_1()
    {
        await using var harness = await CreateHarnessAsync();
        var sequence = CreateSequenceService(harness.Context);
        var map = new FakeIdentifierMapService();
        var builder = new ExternalFileNameBuilder(sequence, map);

        var name = await builder.BuildAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ClearingHouseOriginCode = "1234567",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        });

        Assert.Equal("1234567.001.1", name.FullName);
    }

    [Fact]
    public async Task NachaOutBuilder_Uses_Last7Digits_From_DefaultOrigin_For_Cenit()
    {
        await using var harness = await CreateHarnessAsync();
        var sequence = CreateSequenceService(harness.Context);
        var map = new FakeIdentifierMapService();
        var builder = new ExternalFileNameBuilder(sequence, map);

        var name = await builder.BuildAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 2,
            ClearingHouseCode = "CENIT",
            ClearingHouseOriginCode = "00001283",
            ProcessingDate = new DateTime(2026, 05, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        });

        Assert.Equal("0001283.001.1", name.FullName);
        Assert.Equal('A', name.FileIdModifier);
    }

    [Theory]
    [InlineData("1234567.026.1", 'Z')]
    [InlineData("1234567.027.1", '0')]
    [InlineData("1234567.036.1", '9')]
    public async Task NachaOutValidator_Accepts_Zzz_To_Record1_Field7_Mapping(string fileName, char fileId)
    {
        var validator = CreateValidator();

        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACHCOL",
            ProcessingDate = new DateTime(2026, 05, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            NachaContent = BuildNachaHeader(fileId)
        }, new ExternalFileNameComponents { FullName = fileName });

        Assert.False(result.IsHardBlocked);
    }

    [Fact]
    public async Task AchValidator_HardBlocks_When_ZZZ_DoesNotMatch_Record1_Field7()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            NachaContent = BuildNachaHeader('C')
        }, new ExternalFileNameComponents { FullName = "1234567.001.1" });

        Assert.Equal(ExternalFileValidationDisposition.HardBlock, result.Disposition);
        Assert.Contains(result.Issues, x => x.RuleCode == "ACH_ZZZ_R1");
    }

    [Fact]
    public async Task AchValidator_Applies_DailyLimit_36()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            NachaContent = BuildNachaHeader('A')
        }, new ExternalFileNameComponents { FullName = "1234567.037.1" });

        Assert.Equal(ExternalFileValidationDisposition.HardBlock, result.Disposition);
        Assert.Contains(result.Issues, x => x.RuleCode == "ACH_DAILY_LIMIT");
    }

    [Fact]
    public async Task AchValidator_Enforces_Pse_Range_WhenApplicable()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ProcessingDate = new DateTime(2026, 04, 20),
            IsPse = true,
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            NachaContent = BuildNachaHeader('A')
        }, new ExternalFileNameComponents { FullName = "1234567.001.1" });

        Assert.Equal(ExternalFileValidationDisposition.HardBlock, result.Disposition);
        Assert.Contains(result.Issues, x => x.RuleCode == "ACH_PSE_RANGE");
    }

    [Fact]
    public async Task CenitReject_HardBlock_When_Field6_CountMismatch_D05()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 2,
            ClearingHouseCode = "CENIT",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.StaReject,
            Flow = ExternalFileFlow.Rechazo,
            Direction = ExternalFileDirection.Inbound,
            DeclaredDetailCount = 10,
            NachaContent = "6record\n6record\n"
        }, new ExternalFileNameComponents { FullName = "STA.REJECT.000010.txt", DeclaredDetailCount = 10 });

        Assert.Equal(ExternalFileValidationDisposition.HardBlock, result.Disposition);
        Assert.Contains(result.Issues, x => x.RuleCode == "STA_D05");
    }

    [Fact]
    public async Task CenitReject_HardBlock_D04_Duplicate()
    {
        await using var harness = await CreateHarnessAsync();
        harness.Context.ExternalFileNameRegistry.Add(new Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames.ExternalFileNameRegistry
        {
            ClearingHouseId = 2,
            FlowCode = "Rechazo",
            Direction = "Inbound",
            ExternalFileName = "STA.REJECT.000002.txt",
            ExternalFileType = "StaReject",
            ProcessingDate = new DateTime(2026, 04, 20),
            ValidationDisposition = "Passed",
            ValidationResult = "Accepted",
            CreatedBy = "test"
        });
        await harness.Context.SaveChangesAsync();

        var validator = CreateValidator(harness.Context);
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 2,
            ClearingHouseCode = "CENIT",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.StaReject,
            Flow = ExternalFileFlow.Rechazo,
            Direction = ExternalFileDirection.Inbound,
            DeclaredDetailCount = 2,
            NachaContent = "6a\n6b"
        }, new ExternalFileNameComponents { FullName = "STA.REJECT.000002.txt", DeclaredDetailCount = 2 });

        Assert.Equal(ExternalFileValidationDisposition.HardBlock, result.Disposition);
        Assert.Contains(result.Issues, x => x.RuleCode == "STA_D04");
    }

    [Fact]
    public async Task WarningRule_DoesNotBlock_ForStaOutsideReject()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 2,
            ClearingHouseCode = "CENIT",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.StaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        }, new ExternalFileNameComponents { FullName = "STA-UNKNOWN.TXT" });

        Assert.Equal(ExternalFileValidationDisposition.Warning, result.Disposition);
    }

    [Fact]
    public async Task ExternalFileNamePolicy_ShouldAcceptReturnOutFlowAsProvisional()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.ReturnOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        }, new ExternalFileNameComponents { FullName = "RET_cycle-1_20260515120000.RET" });

        Assert.Equal(ExternalFileValidationDisposition.Warning, result.Disposition);
        Assert.Contains(result.Issues, x => x.RuleCode == "RETURN_NAMING_PROVISIONAL");
    }

    [Fact]
    public async Task ExternalFileNamePolicy_Golden_ReturnOut_ProvisionalWarning_NotHardBlock()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ProcessingDate = new DateTime(2026, 05, 15),
            ExternalFileType = ExternalFileType.ReturnOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        }, new ExternalFileNameComponents { FullName = "RET_CYCLE_20260515120000.RET" });

        Assert.Equal(ExternalFileValidationDisposition.Warning, result.Disposition);
        Assert.False(result.IsHardBlocked);
        Assert.Contains(result.Issues, x => x.RuleCode == "RETURN_NAMING_PROVISIONAL");
    }

    [Fact]
    public async Task ExternalFileNamePolicy_ShouldAcceptReturnOfReturnOutFlowAsProvisional()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 2,
            ClearingHouseCode = "CENIT",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.ReturnOfReturnOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        }, new ExternalFileNameComponents { FullName = "RORNACHA_7001_20260515120000.ach" });

        Assert.Equal(ExternalFileValidationDisposition.Warning, result.Disposition);
        Assert.Contains(result.Issues, x => x.RuleCode == "RETURN_NAMING_PROVISIONAL");
    }

    [Fact]
    public async Task ExternalFileNamePolicy_Golden_ReturnOfReturnOut_ProvisionalWarning_NotHardBlock()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 2,
            ClearingHouseCode = "CENIT",
            ProcessingDate = new DateTime(2026, 05, 15),
            ExternalFileType = ExternalFileType.ReturnOfReturnOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        }, new ExternalFileNameComponents { FullName = "RORNACHA_7001_20260515120000.ach" });

        Assert.Equal(ExternalFileValidationDisposition.Warning, result.Disposition);
        Assert.False(result.IsHardBlocked);
        Assert.Contains(result.Issues, x => x.RuleCode == "RETURN_NAMING_PROVISIONAL");
    }

    [Fact]
    public async Task ExternalFileNamePolicy_Golden_ReturnOut_DuplicateName_WarningOnly()
    {
        await using var harness = await CreateHarnessAsync();
        harness.Context.ExternalFileNameRegistry.Add(new Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames.ExternalFileNameRegistry
        {
            ClearingHouseId = 1,
            FlowCode = ExternalFileFlow.Originacion.ToString(),
            Direction = ExternalFileDirection.Outbound.ToString(),
            ExternalFileName = "RET_CYCLE_20260515120000.RET",
            ExternalFileType = ExternalFileType.ReturnOut.ToString(),
            ProcessingDate = new DateTime(2026, 05, 15),
            ValidationDisposition = ExternalFileValidationDisposition.Warning.ToString(),
            ValidationResult = "Provisional",
            CreatedBy = "test"
        });
        await harness.Context.SaveChangesAsync();

        var validator = CreateValidator(harness.Context);
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ProcessingDate = new DateTime(2026, 05, 15),
            ExternalFileType = ExternalFileType.ReturnOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        }, new ExternalFileNameComponents { FullName = "RET_CYCLE_20260515120000.RET" });

        Assert.Equal(ExternalFileValidationDisposition.Warning, result.Disposition);
        Assert.False(result.IsHardBlocked);
        Assert.Contains(result.Issues, x => x.RuleCode == "RETURN_DUPLICATE_NAME");
    }

    [Fact]
    public async Task ExternalFileNamePolicy_ShouldNotHardBlockUnconfirmedReturnFlows()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.RejectionOut,
            Flow = ExternalFileFlow.Rechazo,
            Direction = ExternalFileDirection.Outbound
        }, new ExternalFileNameComponents { FullName = "PROVISIONAL_REJECT_01.txt" });

        Assert.NotEqual(ExternalFileValidationDisposition.HardBlock, result.Disposition);
        Assert.Contains(result.Issues, x => x.RuleCode == "RETURN_NAMING_PROVISIONAL");
    }

    [Fact]
    public async Task AuditOnlyRule_DoesNotBlock_ForUnmappedChamber()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 9,
            ClearingHouseCode = "OTRA",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaIn,
            Flow = ExternalFileFlow.Recepcion,
            Direction = ExternalFileDirection.Inbound
        }, new ExternalFileNameComponents { FullName = "x.txt" });

        Assert.Equal(ExternalFileValidationDisposition.AuditOnly, result.Disposition);
    }

    [Fact]
    public async Task AuditService_Persists_Registry_And_ValidationLog()
    {
        await using var harness = await CreateHarnessAsync();
        var audit = new ExternalFileNameAuditService(harness.Context);

        var result = new ExternalFileNamePolicyResult
        {
            ExternalFileName = "1234567.001.1",
            Components = new ExternalFileNameComponents { FullName = "1234567.001.1", ExternalSequence = 1 },
            CorrelationEvidence = new ExternalFileNameCorrelationEvidence { ParsedSequence = 1, HeaderFileIdModifier = 'A' },
            Validation = new ExternalFileNameValidationResult
            {
                Disposition = ExternalFileValidationDisposition.Warning,
                Issues = [new ExternalFileNameValidationIssue { RuleCode = "W1", IssueCode = "W", Message = "warn", Disposition = ExternalFileValidationDisposition.Warning }]
            }
        };

        await audit.RegisterAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            ExternalFileType = ExternalFileType.NachaOut,
            ProcessingDate = new DateTime(2026, 04, 20),
            RequestedBy = "tester"
        }, result);

        Assert.Equal(1, await harness.Context.ExternalFileNameRegistry.CountAsync());
        Assert.Equal(1, await harness.Context.ExternalFileNameValidationLog.CountAsync());
    }

    [Fact]
    public async Task SequenceService_Persists_ByScope()
    {
        await using var harness = await CreateHarnessAsync();
        var sequence = CreateSequenceService(harness.Context);
        var context = new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        };

        var first = await sequence.ReserveNextSequenceAsync(context);
        var second = await sequence.ReserveNextSequenceAsync(context);

        Assert.Equal(1, first);
        Assert.Equal(2, second);
    }

    private static ExternalFileNameSequenceService CreateSequenceService(AchDbContext context)
    {
        var providers = new IExternalFileNameSequenceProvider[]
        {
            new EfGenericExternalFileNameSequenceService(context)
        };
        var resolver = new ExternalFileNameSequenceProviderResolver(providers);
        return new ExternalFileNameSequenceService(context, resolver);
    }

    private static ExternalFileNameValidator CreateValidator(AchDbContext? context = null)
    {
        var db = context ?? new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var duplicate = new ExternalFileDuplicateGuard(db);
        var correlation = new ExternalFileNameCorrelationService(new FakeIdentifierMapService());
        return new ExternalFileNameValidator(duplicate, correlation, new FakeIdentifierMapService());
    }

    private static string BuildNachaHeader(char fileId)
    {
        var chars = Enumerable.Repeat('1', 106).ToArray();
        chars[35] = fileId;
        return new string(chars);
    }

    private static async Task<SqliteHarness> CreateHarnessAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return new SqliteHarness(connection, context);
    }

    private sealed class FakeIdentifierMapService : Application.ACH.Interfaces.INachaFileIdentifierMapService
    {
        public Task<char> ResolveIdentifierAsync(int sequence, CancellationToken ct = default)
        {
            if (sequence is < 1 or > 36)
            {
                throw new InvalidOperationException("out");
            }

            return Task.FromResult(sequence <= 26 ? (char)('A' + (sequence - 1)) : (char)('0' + (sequence - 27)));
        }
    }

    private sealed class SqliteHarness(SqliteConnection connection, AchDbContext context) : IAsyncDisposable
    {
        public SqliteConnection Connection { get; } = connection;
        public AchDbContext Context { get; } = context;

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
