using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class ExternalFileNamePolicyPhase1Tests
{
    [Fact]
    public async Task AchBuilder_Uses_DefaultSource_AndBuilds_RRRRTTT_ZZZ_N()
    {
        await using var harness = await CreateHarnessAsync();
        await SeedDynamicNamingFixtureAsync(harness.Context);

        var sequence = CreateSequenceService(harness.Context);
        var map = new FakeIdentifierMapService();
        var namingRuleService = new NachaFileNamingRuleService(harness.Context);
        var builder = new ExternalFileNameBuilder(sequence, map, namingRuleService);

        var name = await builder.BuildAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ClearingHouseOriginCode = "1111111",
            CycleNumber = 6,
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        });

        Assert.Equal("8765321.001.6", name.FullName);
        Assert.Equal("8765321", name.Prefix);
        Assert.Equal(1, name.ExternalSequence);
        Assert.Equal(6, name.CycleNumber);
        Assert.Equal('A', name.FileIdModifier);
    }

    [Theory]
    [InlineData("Ciclo 1 UAT", 1, "8765321.001.1")]
    [InlineData("Ciclo 6 UAT", 6, "8765321.001.6")]
    public async Task AchBuilder_Uses_CycleNumber_From_Context(string cycleName, int expectedCycleNumber, string expectedFullName)
    {
        await using var harness = await CreateHarnessAsync();
        await SeedDynamicNamingFixtureAsync(harness.Context);

        var sequence = CreateSequenceService(harness.Context);
        var map = new FakeIdentifierMapService();
        var namingRuleService = new NachaFileNamingRuleService(harness.Context);
        var builder = new ExternalFileNameBuilder(sequence, map, namingRuleService);

        var name = await builder.BuildAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ClearingHouseOriginCode = "1111111",
            CycleName = cycleName,
            CycleNumber = expectedCycleNumber,
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        });

        Assert.Equal(expectedFullName, name.FullName);
        Assert.Equal(1, name.ExternalSequence);
        Assert.Equal(expectedCycleNumber, name.CycleNumber);
    }

    [Theory]
    [InlineData("Ciclo 6", "8765321.001.6")]
    [InlineData("Cycle 6", "8765321.001.6")]
    [InlineData("Ventana 6", "8765321.001.6")]
    [InlineData("Sesion 6", "8765321.001.6")]
    [InlineData("Compensación 6", "8765321.001.6")]
    [InlineData("6", "8765321.001.6")]
    public async Task AchBuilder_Parses_UniquePositiveCycleNumber_From_CycleName(string cycleName, string expectedFullName)
    {
        await using var harness = await CreateHarnessAsync();
        await SeedDynamicNamingFixtureAsync(harness.Context);

        var sequence = CreateSequenceService(harness.Context);
        var map = new FakeIdentifierMapService();
        var namingRuleService = new NachaFileNamingRuleService(harness.Context);
        var builder = new ExternalFileNameBuilder(sequence, map, namingRuleService);

        var name = await builder.BuildAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ClearingHouseOriginCode = "1111111",
            CycleName = cycleName,
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        });

        Assert.Equal(expectedFullName, name.FullName);
        Assert.Equal(1, name.ExternalSequence);
        Assert.Equal(6, name.CycleNumber);
    }

    [Theory]
    [InlineData("Ciclo 6 2026")]
    [InlineData("ACH 2026 Ciclo 6")]
    [InlineData("Ventana 1 Grupo 2")]
    [InlineData("CYCLE-ACH-20260524-6")]
    public async Task AchBuilder_Throws_When_CycleNameIsAmbiguous(string cycleName)
    {
        await using var harness = await CreateHarnessAsync();
        await SeedDynamicNamingFixtureAsync(harness.Context);

        var sequence = CreateSequenceService(harness.Context);
        var map = new FakeIdentifierMapService();
        var namingRuleService = new NachaFileNamingRuleService(harness.Context);
        var builder = new ExternalFileNameBuilder(sequence, map, namingRuleService);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ClearingHouseOriginCode = "1111111",
            CycleName = cycleName,
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        }));

        Assert.Contains("CycleName", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AchBuilder_Throws_When_CycleNameIsMissing_And_CycleIdMustNotBeUsed()
    {
        await using var harness = await CreateHarnessAsync();
        await SeedDynamicNamingFixtureAsync(harness.Context);

        var sequence = CreateSequenceService(harness.Context);
        var map = new FakeIdentifierMapService();
        var namingRuleService = new NachaFileNamingRuleService(harness.Context);
        var builder = new ExternalFileNameBuilder(sequence, map, namingRuleService);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ClearingHouseOriginCode = "1111111",
            CycleName = "Ciclo 6 2026",
            CycleId = "CYCLE-ACH-20260524-6",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        }));

        Assert.Contains("CycleName", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AchBuilder_Throws_When_CycleNameHasNoPositiveNumber()
    {
        await using var harness = await CreateHarnessAsync();
        await SeedDynamicNamingFixtureAsync(harness.Context);

        var sequence = CreateSequenceService(harness.Context);
        var map = new FakeIdentifierMapService();
        var namingRuleService = new NachaFileNamingRuleService(harness.Context);
        var builder = new ExternalFileNameBuilder(sequence, map, namingRuleService);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ClearingHouseOriginCode = "1111111",
            CycleName = "Ciclo",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        }));

        Assert.Contains("CycleName", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CenitBuilder_Uses_CurrentDocumentedPattern_WithoutAlternateNaming()
    {
        await using var harness = await CreateHarnessAsync();
        await SeedDynamicNamingFixtureAsync(harness.Context);

        var sequence = CreateSequenceService(harness.Context);
        var map = new FakeIdentifierMapService();
        var namingRuleService = new NachaFileNamingRuleService(harness.Context);
        var builder = new ExternalFileNameBuilder(sequence, map, namingRuleService);

        var name = await builder.BuildAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 2,
            ClearingHouseCode = "CENIT",
            ClearingHouseOriginCode = "0000128",
            CycleNumber = 6,
            ProcessingDate = new DateTime(2026, 05, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        });

        Assert.Equal("8765321.006.20260520.1", name.FullName);
        Assert.Equal("8765321", name.Prefix);
        Assert.Equal(1, name.ExternalSequence);
        Assert.Equal(6, name.CycleNumber);
        Assert.Null(name.FileIdModifier);
    }

    [Fact]
    public async Task CenitValidator_AcceptsConfiguredOriginCycleAndProcessingDate()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 2,
            ClearingHouseCode = "CENIT",
            ClearingHouseOriginCode = "00001007",
            CycleNumber = 6,
            ProcessingDate = new DateTime(2026, 05, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        }, new ExternalFileNameComponents
        {
            FullName = "8765321.006.20260520.1",
            Prefix = "8765321",
            ExternalSequence = 1,
            CycleNumber = 6
        });

        Assert.False(result.IsHardBlocked);
    }

    [Fact]
    public async Task AchBuilder_Throws_When_CycleCannotBeResolved()
    {
        await using var harness = await CreateHarnessAsync();
        await SeedDynamicNamingFixtureAsync(harness.Context);

        var sequence = CreateSequenceService(harness.Context);
        var map = new FakeIdentifierMapService();
        var namingRuleService = new NachaFileNamingRuleService(harness.Context);
        var builder = new ExternalFileNameBuilder(sequence, map, namingRuleService);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.BuildAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ClearingHouseOriginCode = "1111111",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        }));

        Assert.Contains("numero de ciclo", ex.Message, StringComparison.OrdinalIgnoreCase);
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
            CycleNumber = 1,
            ProcessingDate = new DateTime(2026, 05, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            NachaContent = BuildNachaHeader(fileId)
        }, new ExternalFileNameComponents { FullName = fileName });

        Assert.False(result.IsHardBlocked);
    }

    [Fact]
    public async Task NachaOutValidator_Accepts_CycleNumber_GreaterThanFive()
    {
        var validator = CreateValidator();

        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACHCOL",
            CycleNumber = 6,
            ProcessingDate = new DateTime(2026, 05, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            NachaContent = BuildNachaHeader('Z')
        }, new ExternalFileNameComponents { FullName = "1234567.026.6", CycleNumber = 6 });

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
            CycleNumber = 1,
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            NachaContent = BuildNachaHeader('C')
        }, new ExternalFileNameComponents { FullName = "1234567.001.1", CycleNumber = 1 });

        Assert.Equal(ExternalFileValidationDisposition.HardBlock, result.Disposition);
        Assert.Contains(result.Issues, x => x.RuleCode == "ACH_ZZZ_R1");
    }

    [Fact]
    public async Task AchValidator_HardBlocks_When_Sequence_OutOfRange()
    {
        var validator = CreateValidator();
        var result = await validator.ValidateAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            CycleNumber = 1,
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            NachaContent = BuildNachaHeader('A')
        }, new ExternalFileNameComponents { FullName = "1234567.037.1", CycleNumber = 1 });

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
            CycleNumber = 1,
            ProcessingDate = new DateTime(2026, 04, 20),
            IsPse = true,
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            NachaContent = BuildNachaHeader('A')
        }, new ExternalFileNameComponents { FullName = "1234567.001.1", CycleNumber = 1 });

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
    public async Task ReturnOutBuilder_ShouldUse_RRRRTTT_ZZZ_RET_WithSequenceAndFileId()
    {
        await using var harness = await CreateHarnessAsync();
        await SeedDynamicNamingFixtureAsync(harness.Context);

        var sequence = CreateSequenceService(harness.Context);
        var map = new FakeIdentifierMapService();
        var namingRuleService = new NachaFileNamingRuleService(harness.Context);
        var builder = new ExternalFileNameBuilder(sequence, map, namingRuleService);

        var name = await builder.BuildAsync(new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ClearingHouseOriginCode = "1111111",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.ReturnOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        });

        Assert.Equal("8765321.001.RET", name.FullName);
        Assert.Equal("8765321", name.Prefix);
        Assert.Equal(1, name.ExternalSequence);
        Assert.Equal('A', name.FileIdModifier);
    }

    [Fact]
    public async Task ReturnOutValidator_ShouldPass_ForNormativeName()
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
        }, new ExternalFileNameComponents { FullName = "0101006.001.RET" });

        Assert.NotEqual(ExternalFileValidationDisposition.HardBlock, result.Disposition);
        Assert.DoesNotContain(result.Issues, x => x.RuleCode.StartsWith("RETURN_", StringComparison.Ordinal));
    }

    [Fact]
    public async Task ReturnOutValidator_ShouldHardBlock_WhenPatternIsInvalid()
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

        Assert.Equal(ExternalFileValidationDisposition.HardBlock, result.Disposition);
        Assert.Contains(result.Issues, x => x.RuleCode == "RETURN_NAME_PATTERN");
    }

    [Fact]
    public async Task ReturnOutValidator_ShouldHardBlock_WhenSequenceIsOutOfRange()
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
        }, new ExternalFileNameComponents { FullName = "0101006.037.RET" });

        Assert.Equal(ExternalFileValidationDisposition.HardBlock, result.Disposition);
        Assert.Contains(result.Issues, x => x.RuleCode == "RETURN_DAILY_LIMIT");
    }

    [Fact]
    public async Task ExternalFileNamePolicy_ShouldAcceptReturnOfReturnOutFlowAsProvisional()
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
    public async Task ReturnOutValidator_ShouldHardBlock_WhenDuplicateNameExists()
    {
        await using var harness = await CreateHarnessAsync();
        harness.Context.ExternalFileNameRegistry.Add(new Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames.ExternalFileNameRegistry
        {
            ClearingHouseId = 1,
            FlowCode = ExternalFileFlow.Originacion.ToString(),
            Direction = ExternalFileDirection.Outbound.ToString(),
            ExternalFileName = "0101006.001.RET",
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
        }, new ExternalFileNameComponents { FullName = "0101006.001.RET" });

        Assert.Equal(ExternalFileValidationDisposition.HardBlock, result.Disposition);
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

    [Fact]
    public async Task SequenceService_Increments_SameDay_As_001_Then_002()
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

    [Fact]
    public async Task SequenceService_Resets_NextDay_To_001()
    {
        await using var harness = await CreateHarnessAsync();
        var sequence = CreateSequenceService(harness.Context);

        var firstDay = new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        };

        var nextDay = new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ProcessingDate = new DateTime(2026, 04, 21),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        };

        var first = await sequence.ReserveNextSequenceAsync(firstDay);
        var reset = await sequence.ReserveNextSequenceAsync(nextDay);

        Assert.Equal(1, first);
        Assert.Equal(1, reset);
    }

    [Fact]
    public async Task SequenceService_Is_Isolated_By_ClearingHouse()
    {
        await using var harness = await CreateHarnessAsync();
        var sequence = CreateSequenceService(harness.Context);

        var ach = new ExternalFileNameContext
        {
            ClearingHouseId = 1,
            ClearingHouseCode = "ACH",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        };

        var cenit = new ExternalFileNameContext
        {
            ClearingHouseId = 2,
            ClearingHouseCode = "CENIT",
            ProcessingDate = new DateTime(2026, 04, 20),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound
        };

        var achFirst = await sequence.ReserveNextSequenceAsync(ach);
        var cenitFirst = await sequence.ReserveNextSequenceAsync(cenit);
        var achSecond = await sequence.ReserveNextSequenceAsync(ach);
        var cenitSecond = await sequence.ReserveNextSequenceAsync(cenit);

        Assert.Equal(1, achFirst);
        Assert.Equal(1, cenitFirst);
        Assert.Equal(2, achSecond);
        Assert.Equal(2, cenitSecond);
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

    private static async Task SeedDynamicNamingFixtureAsync(AchDbContext context)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 1,
            ClearingHouseId = 1,
            HolidayStrategy = "Colombian"
        });

        context.ClearingHouses.AddRange(
            new ClearingHouse
            {
                Id = 1,
                Name = "ACH Colombia",
                Code = "ACH",
                OriginCode = "0000000",
                ClearingHouseId = 1
            },
            new ClearingHouse
            {
                Id = 2,
                Name = "CENIT",
                Code = "CENIT",
                OriginCode = "0000000",
                ClearingHouseId = 1
            });

        context.FinancialInstitutions.Add(new FinancialInstitution
        {
            Id = 1,
            Name = "Origen dinamico UAT",
            RoutingNumber = "98765",
            TransitCode = "321",
            IsDefaultSource = true,
            Status = FinancialInstitutionStatus.Active
        });
        context.FinancialInstitutions.Local.Single().CalculateCheckDigit();

        context.NachaFileNamingRules.AddRange(
            new NachaFileNamingRule
            {
                Id = 1,
                ClearingHouseId = 1,
                FileDirection = NachaFileDirection.Outbound,
                NamePattern = "RRRRTTT.ZZZ.N",
                Extension = ".ach",
                DailySequenceMin = 1,
                DailySequenceMax = 36,
                InternalFileIdMappingMode = InternalFileIdMappingMode.Alphanumeric36,
                RequiresNameHeaderEntityMatch = true,
                IsActive = true,
                EffectiveFrom = new DateTime(2026, 01, 01),
                NormativeSource = "MAN-004",
                NormativeReference = "V32"
            },
            new NachaFileNamingRule
            {
                Id = 2,
                ClearingHouseId = 2,
                FileDirection = NachaFileDirection.Outbound,
                NamePattern = "RRRRTTT.ZZZ.N",
                Extension = ".ach",
                DailySequenceMin = 1,
                DailySequenceMax = 36,
                InternalFileIdMappingMode = InternalFileIdMappingMode.Alphanumeric36,
                RequiresNameHeaderEntityMatch = true,
                IsActive = true,
                EffectiveFrom = new DateTime(2026, 01, 01),
                NormativeSource = "MAN-004",
                NormativeReference = "V32"
            });

        await context.SaveChangesAsync();
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
