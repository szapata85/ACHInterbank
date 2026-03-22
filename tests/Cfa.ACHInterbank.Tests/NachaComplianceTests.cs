using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaComplianceTests
{
    [Fact]
    public void ResolveBatchDescription_ForPseCredit_UsesMulticredit()
    {
        var service = new NachaBatchRuleService();
        var batch = new AchBatch { CompanyEntryDescription = "PAGOS PSE", EffectiveEntryDate = new DateTime(2026, 3, 22) };
        var transactions = new[]
        {
            new AchTransaction { Type = TransactionTypeEnum.Credit, Reference = "PSE-123" }
        };

        var description = service.ResolveBatchDescription(batch, transactions);

        Assert.Equal("MULTICREDIT", description);
    }

    [Fact]
    public void ResolveDescriptiveDate_ForCreditBatch_IsMandatory()
    {
        var service = new NachaBatchRuleService();
        var batch = new AchBatch { EffectiveEntryDate = new DateTime(2026, 3, 22) };
        var transactions = new[]
        {
            new AchTransaction { Type = TransactionTypeEnum.Credit, IsPrenotification = true }
        };

        var descriptiveDate = service.ResolveDescriptiveDate(batch, transactions);

        Assert.Equal(new DateTime(2026, 3, 22), descriptiveDate);
    }

    [Fact]
    public void ValidateOrThrow_ForCreditType5WithoutDescriptiveDate_Throws()
    {
        var validator = new NachaFormatValidator();
        var header = "1" + " ".PadRight(105, ' ');
        var batch = BuildType5Record("220", "MULTICREDI", string.Empty);
        var fileControl = "9" + " ".PadRight(105, ' ');

        Assert.Throws<InvalidOperationException>(() => validator.ValidateOrThrow(
            header + batch + fileControl,
            new NachaValidationContext(
                new AchCycleDto { Id = "cycle", ClearingHouseId = 2, CycleName = "Ciclo 3" },
                new ClearingHouseDto { Id = 2, Code = "ACH", OriginCode = "9999999999" })));
    }

    [Fact]
    public void BuildNachaFileName_ForCenit_UsesOriginCodeAndCycleNumber()
    {
        var fileName = NachaExportService.BuildNachaFileName(
            new ClearingHouseDto { Id = 2, Code = "CENIT", OriginCode = "999" },
            new AchCycleDto { Id = "cycle", CycleName = "CICLO 3", ClearingHouseId = 2 });

        Assert.Equal("999.003.1", fileName);
    }

    private static string BuildType5Record(string serviceClassCode, string companyEntryDescription, string descriptiveDate)
    {
        var buffer = Enumerable.Repeat(' ', 106).ToArray();
        buffer[0] = '5';
        serviceClassCode.CopyTo(0, buffer, 1, serviceClassCode.Length);
        companyEntryDescription.PadRight(10, ' ').CopyTo(0, buffer, 53, 10);
        descriptiveDate.PadRight(8, ' ').CopyTo(0, buffer, 63, 8);
        return new string(buffer);
    }
}
