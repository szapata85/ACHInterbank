using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ProcTransaccionesRequestMapperTests
{
    [Fact]
    public async Task ResolveAsync_FailFast_WhenPublishedMappingDoesNotExist()
    {
        await using var context = BuildContext();
        context.IntegrationMethods.Add(new IntegrationMethod
        {
            Id = 10,
            Code = "WSCFAACH.Proc_Transacciones",
            DisplayName = "Proc_Transacciones",
            SoapClientCode = "WSCFAACH",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var sut = new ProcTransaccionesRequestMapper(context);
        var queue = BuildQueue();
        var ingestion = new IncomingNachaFileIngestion { Id = queue.IncomingNachaFileIngestionId, FileName = "in.ach", FileHashSha256 = "h", ContentType = "txt", CorrelationId = "c", UploadedBy = "u", Notes = "n" };
        var classification = new IncomingNachaEntryClassification { Id = queue.IncomingNachaEntryClassificationId };
        var transaction = new AchTransaction { Id = queue.AchTransactionId, Amount = 100m, TransactionCode = "22", TraceNumber = "1", TransactionExternalId = "ext", AchCycleId = "C1", Reference = "r", CompanyName = "c", CompanyIdentification = "i", SourceAccountNumber = "s", DestinationAccountNumber = "d", OriginatingDFI = "o", ReceivingDFI = "r", SourceInstitutionId = 1, DestinationInstitutionId = 1, AchBatchId = 1, Type = Domain.Entities.Transactions.Enums.TransactionTypeEnum.Credit, EffectiveEntryDate = DateTime.Today };
        var cycle = new AchCycle { Id = "C1", ProcessingDate = DateTime.Today, StartTime = TimeSpan.Zero, EndTime = new TimeSpan(23, 59, 0), ClearingHouseId = 1, CycleName = "c1" };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ResolveAsync(queue, ingestion, classification, transaction, cycle, DateTime.Now));
    }

    [Fact]
    public async Task ResolveAsync_BuildsContractWithPublishedMapping_UsingProcTransaccionesTechnicalNames()
    {
        await using var context = BuildContext();
        var method = new IntegrationMethod
        {
            Id = 11,
            Code = "WSCFAACH.Proc_Transacciones",
            DisplayName = "Proc_Transacciones",
            SoapClientCode = "WSCFAACH",
            IsActive = true
        };
        context.IntegrationMethods.Add(method);

        var pTreg = new IntegrationMethodParameter { Id = 1, MethodId = method.Id, ParameterPath = "TREG", Required = true, Direction = IntegrationParameterDirectionEnum.Input, DisplayName = "TREG" };
        var pTiptran = new IntegrationMethodParameter { Id = 2, MethodId = method.Id, ParameterPath = "TIPTRAN", Required = true, Direction = IntegrationParameterDirectionEnum.Input, DisplayName = "TIPTRAN" };
        var pMonto = new IntegrationMethodParameter { Id = 3, MethodId = method.Id, ParameterPath = "MONTO", Required = true, Direction = IntegrationParameterDirectionEnum.Input, DisplayName = "MONTO" };
        var pIdTran = new IntegrationMethodParameter { Id = 4, MethodId = method.Id, ParameterPath = "IDTRAN", Required = true, Direction = IntegrationParameterDirectionEnum.Input, DisplayName = "IDTRAN" };
        var pIdCam = new IntegrationMethodParameter { Id = 5, MethodId = method.Id, ParameterPath = "IDCAMCOMPE", Required = true, Direction = IntegrationParameterDirectionEnum.Input, DisplayName = "IDCAMCOMPE" };
        context.IntegrationMethodParameters.AddRange(pTreg, pTiptran, pMonto, pIdTran, pIdCam);

        var set = new IntegrationMappingSet { Id = Guid.NewGuid(), MethodId = method.Id, Name = "pub", Version = 1, Status = IntegrationMappingSetStatusEnum.Published };
        context.IntegrationMappingSets.Add(set);
        context.IntegrationMappingRules.AddRange(
            new IntegrationMappingRule { Id = 1, MappingSetId = set.Id, MethodId = method.Id, ParameterId = pTreg.Id, SourceKind = IntegrationSourceKindEnum.Constant, FixedValue = "6", Priority = 1, Enabled = true },
            new IntegrationMappingRule { Id = 2, MappingSetId = set.Id, MethodId = method.Id, ParameterId = pTiptran.Id, SourceKind = IntegrationSourceKindEnum.Transaction, SourceFieldPath = "transaction.transactionCode", Priority = 1, Enabled = true },
            new IntegrationMappingRule { Id = 3, MappingSetId = set.Id, MethodId = method.Id, ParameterId = pMonto.Id, SourceKind = IntegrationSourceKindEnum.Transaction, SourceFieldPath = "transaction.amount", Priority = 1, Enabled = true },
            new IntegrationMappingRule { Id = 4, MappingSetId = set.Id, MethodId = method.Id, ParameterId = pIdTran.Id, SourceKind = IntegrationSourceKindEnum.Transaction, SourceFieldPath = "transaction.id", Priority = 1, Enabled = true },
            new IntegrationMappingRule { Id = 5, MappingSetId = set.Id, MethodId = method.Id, ParameterId = pIdCam.Id, SourceKind = IntegrationSourceKindEnum.Cycle, SourceFieldPath = "cycle.clearingHouseId", Priority = 1, Enabled = true });
        context.IntegrationMappingSetHistory.Add(new IntegrationMappingSetHistory { MappingSetId = set.Id, MethodId = method.Id, Version = 1, Status = IntegrationMappingSetStatusEnum.Published, Action = "Publish", PerformedBy = "tester", SnapshotHash = "snap-1", SnapshotJson = "{}" });
        await context.SaveChangesAsync();

        var sut = new ProcTransaccionesRequestMapper(context);
        var queue = BuildQueue();
        var ingestion = new IncomingNachaFileIngestion { Id = queue.IncomingNachaFileIngestionId, FileName = "in.ach", FileHashSha256 = "h", ContentType = "txt", CorrelationId = "c", UploadedBy = "u", Notes = "n" };
        var classification = new IncomingNachaEntryClassification { Id = queue.IncomingNachaEntryClassificationId, FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante };
        var transaction = new AchTransaction { Id = queue.AchTransactionId, Amount = 100m, TransactionCode = "22", TraceNumber = "1", TransactionExternalId = "ext", AchCycleId = "C1", Reference = "r", CompanyName = "c", CompanyIdentification = "i", SourceAccountNumber = "s", DestinationAccountNumber = "d", OriginatingDFI = "o", ReceivingDFI = "r", SourceInstitutionId = 1, DestinationInstitutionId = 1, AchBatchId = 1, Type = Domain.Entities.Transactions.Enums.TransactionTypeEnum.Credit, EffectiveEntryDate = DateTime.Today };
        var cycle = new AchCycle { Id = "C1", ProcessingDate = DateTime.Today, StartTime = TimeSpan.Zero, EndTime = new TimeSpan(23, 59, 0), ClearingHouseId = 1, CycleName = "c1" };

        var resolution = await sut.ResolveAsync(queue, ingestion, classification, transaction, cycle, DateTime.Now);
        var xml = sut.BuildSoapBody(resolution.Contract);

        Assert.Equal("6", resolution.Contract.Parameters["TREG"]);
        Assert.Contains("<tem:TREG>6</tem:TREG>", xml);
        Assert.Contains("<tem:TIPTRAN>22</tem:TIPTRAN>", xml);
        Assert.Contains("<tem:IDCAMCOMPE>1</tem:IDCAMCOMPE>", xml);
        Assert.DoesNotContain("<METODO>", xml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Proc_Contrapartidas", xml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RegistrarRespuestaTransaccion", xml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PLValidarUsuarioBV", xml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RTAACH", xml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RTALOC", xml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProcTransaccionesCatalog_ShouldKeepObservedOptionalAndResponseFieldsOutOfRequiredInputs()
    {
        await using var context = BuildContext();
        await new IntegrationCatalogBootstrapper(context).EnsureAsync();

        var method = await context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Transacciones");
        var parameters = await context.IntegrationMethodParameters
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .ToDictionaryAsync(x => x.ParameterPath, StringComparer.OrdinalIgnoreCase);

        Assert.False(parameters["NCTAORIG"].Required);
        Assert.Equal(IntegrationParameterDirectionEnum.Input, parameters["NCTAORIG"].Direction);
        Assert.False(parameters["DISCRE"].Required);
        Assert.Equal(IntegrationParameterDirectionEnum.Input, parameters["DISCRE"].Direction);
        Assert.True(parameters["MONTO"].Required);
        Assert.Equal(IntegrationParameterDirectionEnum.Input, parameters["MONTO"].Direction);
        Assert.False(parameters["ILR"].Required);
        Assert.Equal(IntegrationParameterDirectionEnum.Input, parameters["ILR"].Direction);
        Assert.Equal(IntegrationParameterDirectionEnum.Output, parameters["RTAACH"].Direction);
        Assert.Equal(IntegrationParameterDirectionEnum.Output, parameters["RTALOC"].Direction);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("B")]
    public async Task BuildSoapBody_AllowsObservedIlrValues_WhenMapped(string ilr)
    {
        await using var context = BuildContext();
        var method = new IntegrationMethod
        {
            Id = 12,
            Code = "WSCFAACH.Proc_Transacciones",
            DisplayName = "Proc_Transacciones",
            SoapClientCode = "WSCFAACH",
            IsActive = true
        };
        context.IntegrationMethods.Add(method);

        var pTreg = new IntegrationMethodParameter { Id = 11, MethodId = method.Id, ParameterPath = "TREG", Required = true, Direction = IntegrationParameterDirectionEnum.Input, DisplayName = "TREG" };
        var pTiptran = new IntegrationMethodParameter { Id = 12, MethodId = method.Id, ParameterPath = "TIPTRAN", Required = true, Direction = IntegrationParameterDirectionEnum.Input, DisplayName = "TIPTRAN" };
        var pMonto = new IntegrationMethodParameter { Id = 13, MethodId = method.Id, ParameterPath = "MONTO", Required = true, Direction = IntegrationParameterDirectionEnum.Input, DisplayName = "MONTO" };
        var pIdTran = new IntegrationMethodParameter { Id = 14, MethodId = method.Id, ParameterPath = "IDTRAN", Required = true, Direction = IntegrationParameterDirectionEnum.Input, DisplayName = "IDTRAN" };
        var pIdCam = new IntegrationMethodParameter { Id = 15, MethodId = method.Id, ParameterPath = "IDCAMCOMPE", Required = true, Direction = IntegrationParameterDirectionEnum.Input, DisplayName = "IDCAMCOMPE" };
        var pIlr = new IntegrationMethodParameter { Id = 16, MethodId = method.Id, ParameterPath = "ILR", Required = false, Direction = IntegrationParameterDirectionEnum.Input, DisplayName = "ILR" };
        context.IntegrationMethodParameters.AddRange(pTreg, pTiptran, pMonto, pIdTran, pIdCam, pIlr);

        var set = new IntegrationMappingSet { Id = Guid.NewGuid(), MethodId = method.Id, Name = "pub", Version = 1, Status = IntegrationMappingSetStatusEnum.Published };
        context.IntegrationMappingSets.Add(set);
        context.IntegrationMappingRules.AddRange(
            new IntegrationMappingRule { Id = 11, MappingSetId = set.Id, MethodId = method.Id, ParameterId = pTreg.Id, SourceKind = IntegrationSourceKindEnum.Constant, FixedValue = "6", Priority = 1, Enabled = true },
            new IntegrationMappingRule { Id = 12, MappingSetId = set.Id, MethodId = method.Id, ParameterId = pTiptran.Id, SourceKind = IntegrationSourceKindEnum.Transaction, SourceFieldPath = "transaction.transactionCode", Priority = 1, Enabled = true },
            new IntegrationMappingRule { Id = 13, MappingSetId = set.Id, MethodId = method.Id, ParameterId = pMonto.Id, SourceKind = IntegrationSourceKindEnum.Transaction, SourceFieldPath = "transaction.amount", Priority = 1, Enabled = true },
            new IntegrationMappingRule { Id = 14, MappingSetId = set.Id, MethodId = method.Id, ParameterId = pIdTran.Id, SourceKind = IntegrationSourceKindEnum.Transaction, SourceFieldPath = "transaction.id", Priority = 1, Enabled = true },
            new IntegrationMappingRule { Id = 15, MappingSetId = set.Id, MethodId = method.Id, ParameterId = pIdCam.Id, SourceKind = IntegrationSourceKindEnum.Cycle, SourceFieldPath = "cycle.clearingHouseId", Priority = 1, Enabled = true },
            new IntegrationMappingRule { Id = 16, MappingSetId = set.Id, MethodId = method.Id, ParameterId = pIlr.Id, SourceKind = IntegrationSourceKindEnum.Constant, FixedValue = ilr, Priority = 1, Enabled = true });
        await context.SaveChangesAsync();

        var sut = new ProcTransaccionesRequestMapper(context);
        var queue = BuildQueue();
        var ingestion = new IncomingNachaFileIngestion { Id = queue.IncomingNachaFileIngestionId, FileName = "in.ach", FileHashSha256 = "h", ContentType = "txt", CorrelationId = "c", UploadedBy = "u", Notes = "n" };
        var classification = new IncomingNachaEntryClassification { Id = queue.IncomingNachaEntryClassificationId, FunctionalClass = IncomingNachaFunctionalClass.CreditoEntrante };
        var transaction = new AchTransaction { Id = queue.AchTransactionId, Amount = 0m, TransactionCode = "33", TraceNumber = "1", TransactionExternalId = "ext", AchCycleId = "C1", Reference = "r", CompanyName = "c", CompanyIdentification = "i", SourceAccountNumber = string.Empty, DestinationAccountNumber = "d", OriginatingDFI = "o", ReceivingDFI = "r", SourceInstitutionId = 1, DestinationInstitutionId = 1, AchBatchId = 1, Type = Domain.Entities.Transactions.Enums.TransactionTypeEnum.Credit, EffectiveEntryDate = DateTime.Today };
        var cycle = new AchCycle { Id = "C1", ProcessingDate = DateTime.Today, StartTime = TimeSpan.Zero, EndTime = new TimeSpan(23, 59, 0), ClearingHouseId = 1, CycleName = "c1" };

        var resolution = await sut.ResolveAsync(queue, ingestion, classification, transaction, cycle, DateTime.Now);
        var xml = sut.BuildSoapBody(resolution.Contract);

        Assert.Equal("0", resolution.Contract.Parameters["MONTO"]);
        Assert.Equal(ilr, resolution.Contract.Parameters["ILR"]);
        Assert.Contains($"<tem:ILR>{ilr}</tem:ILR>", xml);
    }

    private static IncomingNachaDispatchQueue BuildQueue()
    {
        return new IncomingNachaDispatchQueue
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = Guid.NewGuid(),
            IncomingNachaEntryClassificationId = Guid.NewGuid(),
            IncomingNachaTransactionLinkId = Guid.NewGuid(),
            AchTransactionId = 100,
            AchCycleId = "C1",
            ClearingHouseId = 1,
            OperationalDate = DateTime.Today
        };
    }

    private static AchDbContext BuildContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AchDbContext(options);
    }
}
