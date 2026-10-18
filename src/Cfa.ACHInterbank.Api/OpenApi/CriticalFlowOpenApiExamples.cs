using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Api.OpenApi;

/// <summary>
/// Ejemplo ficticio de alta individual usado únicamente como metadata OpenAPI de SC-505.
/// </summary>
public sealed class TransactionCreateRequestExampleProvider : IOpenApiExampleProvider
{
    public object GetExample() => new AchTransactionRequest
    {
        Amount = 125000m,
        TransactionExternalId = "TX-DEMO-001",
        Reference = "REF-DEMO-001",
        Type = TransactionTypeEnum.Credit,
        AccountType = AccountTypeEnum.Checking,
        IsPrenotification = false,
        DestinationInstitutionId = 1001,
        SourceAccountNumber = "000000000001",
        DestinationAccountNumber = "000000000002",
        RequiresIdentityValidation = false,
        CompanyName = "EMPRESA DEMO",
        CompanyIdentification = "900000001",
        CompanyEntryDescriptionId = 1001,
        SourcePersonType = "PJ"
    };
}

/// <summary>
/// Ejemplo ficticio de registro masivo inline usado por las dos operaciones bulk de Transactions.
/// </summary>
public sealed class BulkTransactionRequestExampleProvider : IOpenApiExampleProvider
{
    public object GetExample() => CriticalFlowExampleFactory.CreateBulkRequest();
}

/// <summary>
/// Ejemplo ficticio del resultado agregado de un registro masivo inline.
/// </summary>
public sealed class BulkTransactionSuccessResponseExampleProvider : IOpenApiExampleProvider
{
    public object GetExample() => CriticalFlowExampleFactory.CreateBulkResponse();
}

/// <summary>
/// Ejemplo ficticio de ingestión masiva síncrona con transacciones inline.
/// </summary>
public sealed class BulkIngestionRequestExampleProvider : IOpenApiExampleProvider
{
    public object GetExample()
    {
        var bulk = CriticalFlowExampleFactory.CreateBulkRequest();
        return new BulkIngestionRequest
        {
            BatchReference = bulk.BatchReference,
            SourceType = BulkIngestionSourceType.InlineTransactions,
            ProcessingMode = BulkIngestionProcessingMode.Synchronous,
            Transactions = bulk.Transactions
        };
    }
}

/// <summary>
/// Ejemplo ficticio del resultado síncrono de ingestión masiva inline.
/// </summary>
public sealed class BulkIngestionSuccessResponseExampleProvider : IOpenApiExampleProvider
{
    public object GetExample() => new BulkIngestionResponse
    {
        ProcessingMode = BulkIngestionProcessingMode.Synchronous,
        Status = "COMPLETED",
        ImmediateResult = CriticalFlowExampleFactory.CreateBulkResponse()
    };
}

/// <summary>
/// Ejemplo ficticio para una ejecución manual saliente de ACH Colombia.
/// </summary>
public sealed class AchManagedFileExecuteOutboundRequestExampleProvider : IOpenApiExampleProvider
{
    public object GetExample() => new AchColombiaFileExchangeController.ExecuteOutboundRequest(
        "CYCLE-DEMO-001",
        null);
}

/// <summary>
/// Ejemplo ficticio y determinístico del resumen devuelto por una ejecución administrada de archivos.
/// </summary>
public sealed class AchManagedFileExecutionSuccessResponseExampleProvider : IOpenApiExampleProvider
{
    public object GetExample() => new AchManagedFileExecutionResult(
        Processed: 1,
        Succeeded: 1,
        Failed: 0,
        TransferIds: [Guid.Parse("11111111-1111-4111-8111-111111111111")]);
}

/// <summary>
/// Ejemplo ficticio del request compartido por las generaciones NACHA-M plana y cifrada.
/// </summary>
public sealed class NachaGenerateRequestExampleProvider : IOpenApiExampleProvider
{
    public object GetExample() => new NachaSecurityOperationsController.NachaGenerateApiRequest
    {
        CycleId = "CYCLE-DEMO-001"
    };
}

internal static class CriticalFlowExampleFactory
{
    public static BulkAchTransactionRequest CreateBulkRequest() => new()
    {
        BatchReference = "BATCH-DEMO-001",
        Transactions =
        [
            new BulkAchTransactionItemRequest
            {
                Amount = 125000m,
                TransactionExternalId = "TX-DEMO-001",
                Reference = "REF-DEMO-001",
                Type = TransactionTypeEnum.Credit,
                AccountType = AccountTypeEnum.Checking,
                IsPrenotification = false,
                DestinationInstitutionId = 1001,
                SourceAccountNumber = "000000000001",
                DestinationAccountNumber = "000000000002",
                RequiresIdentityValidation = false,
                CompanyName = "EMPRESA DEMO",
                CompanyIdentification = "900000001",
                CompanyEntryDescriptionId = 1001,
                SourcePersonType = "PJ"
            }
        ]
    };

    public static BulkAchTransactionResponse CreateBulkResponse() => new()
    {
        BatchReference = "BATCH-DEMO-001",
        TotalReceived = 1,
        TotalProcessed = 1,
        TotalSucceeded = 1,
        TotalFailed = 0,
        CreatedTransactionIds = [1001],
        ItemResults =
        [
            new BulkAchTransactionItemResult
            {
                Index = 0,
                TransactionExternalId = "TX-DEMO-001",
                Reference = "REF-DEMO-001",
                Succeeded = true,
                TransactionId = 1001
            }
        ]
    };
}
