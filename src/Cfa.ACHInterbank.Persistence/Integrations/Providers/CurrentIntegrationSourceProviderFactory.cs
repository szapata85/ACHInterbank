using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.Integrations.Providers;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Providers;

/// <summary>
/// Creates only the source strategies currently required by the three CFA SOAP operations.
/// P3.1 wires Proc_Transacciones to these strategies behind its service feature flag;
/// the other operations keep their existing runtime paths.
/// </summary>
public sealed class CurrentIntegrationSourceProviderFactory
{
    public const string ProcContrapartidasMethod = "WSCFAACH.Proc_Contrapartidas";
    public const string ProcTransaccionesMethod = "WSCFAACH.Proc_Transacciones";
    public const string RegistrarRespuestaMethod = "WSAXON.RegistrarRespuestaTransaccion";

    private readonly AchDbContext _context;
    private readonly IProcTransaccionesFunctionalSourceResolver _procTransaccionesFunctionalSourceResolver;

    public CurrentIntegrationSourceProviderFactory(
        AchDbContext context,
        IProcTransaccionesFunctionalSourceResolver? procTransaccionesFunctionalSourceResolver = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _procTransaccionesFunctionalSourceResolver = procTransaccionesFunctionalSourceResolver
            ?? new Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ProcTransaccionesFunctionalSourceResolver(context);
    }

    public IReadOnlyCollection<IIntegrationSourceValueProvider> Create()
    {
        var providers = new List<IIntegrationSourceValueProvider>
        {
            new ConstantIntegrationSourceValueProvider(
                [ProcContrapartidasMethod, ProcTransaccionesMethod, RegistrarRespuestaMethod])
        };

        AddTransactionProviders(providers);
        AddProcTransaccionesProviders(providers);
        AddContrapartidasProviders(providers);
        AddDifferentialResponseProviders(providers);
        return providers;
    }

    private void AddTransactionProviders(ICollection<IIntegrationSourceValueProvider> providers)
    {
        Add(providers, IntegrationSourceKindEnum.Transaction, "transaction.companyIdentification", "Identificación originador", "Identificación de empresa de la transacción.", "string", false, "Transacción", 10, IntegrationSensitiveClassification.Personal,
            [ProcContrapartidasMethod, ProcTransaccionesMethod],
            async (context, ct) => (await RequireTransactionAsync(context, ct)).CompanyIdentification);
        Add(providers, IntegrationSourceKindEnum.Transaction, "transaction.sourceAccountNumber", "Cuenta origen", "Número de cuenta origen de la transacción.", "string", false, "Transacción", 20, IntegrationSensitiveClassification.Account,
            [ProcContrapartidasMethod],
            async (context, ct) => (await RequireTransactionAsync(context, ct)).SourceAccountNumber);
        Add(providers, IntegrationSourceKindEnum.Transaction, "transaction.debitCreditIndicator", "Indicador débito/crédito", "Indicador D/C derivado del tipo transaccional.", "string", false, "Transacción", 30, IntegrationSensitiveClassification.Financial,
            [ProcContrapartidasMethod],
            async (context, ct) => DebitCreditIndicator(await RequireTransactionAsync(context, ct)));
        Add(providers, IntegrationSourceKindEnum.Transaction, "transaction.effectiveEntryDate", "Fecha efectiva", "Fecha efectiva en forma léxica yyyyMMdd.", "datetime", false, "Transacción", 40, IntegrationSensitiveClassification.Financial,
            [ProcContrapartidasMethod, ProcTransaccionesMethod],
            async (context, ct) => (await RequireTransactionAsync(context, ct)).EffectiveEntryDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture));
        Add(providers, IntegrationSourceKindEnum.Transaction, "transaction.amount", "Monto", "Monto de la transacción en formato invariante.", "decimal", false, "Transacción", 50, IntegrationSensitiveClassification.Financial,
            [ProcContrapartidasMethod, ProcTransaccionesMethod],
            async (context, ct) => (await RequireTransactionAsync(context, ct)).Amount.ToString(CultureInfo.InvariantCulture));
        Add(providers, IntegrationSourceKindEnum.Transaction, "transaction.reference", "Referencia", "Referencia funcional de la transacción.", "string", true, "Transacción", 60, IntegrationSensitiveClassification.Technical,
            [ProcContrapartidasMethod],
            async (context, ct) => (await RequireTransactionAsync(context, ct)).Reference);
        Add(providers, IntegrationSourceKindEnum.Transaction, "transaction.id", "Id transacción", "Identificador interno de la transacción.", "int", false, "Transacción", 70, IntegrationSensitiveClassification.Technical,
            [ProcContrapartidasMethod],
            async (context, ct) => (await RequireTransactionAsync(context, ct)).Id.ToString(CultureInfo.InvariantCulture));
        Add(providers, IntegrationSourceKindEnum.Transaction, "transaction.transactionCode", "Código transacción", "Código transaccional persistido.", "int", false, "Transacción", 80, IntegrationSensitiveClassification.Technical,
            [ProcTransaccionesMethod],
            async (context, ct) => (await RequireTransactionAsync(context, ct)).TransactionCode);
        Add(providers, IntegrationSourceKindEnum.Transaction, "transaction.destinationAccountNumber", "Cuenta receptora", "Número de cuenta receptora de la transacción.", "string", false, "Transacción", 90, IntegrationSensitiveClassification.Account,
            [ProcTransaccionesMethod],
            async (context, ct) => (await RequireTransactionAsync(context, ct)).DestinationAccountNumber);
        Add(providers, IntegrationSourceKindEnum.Transaction, "transaction.traceSequenceNumber", "Secuencia transacción", "Secuencia de trace normalizada a siete dígitos.", "long", false, "Transacción", 100, IntegrationSensitiveClassification.Technical,
            [ProcTransaccionesMethod],
            async (context, ct) => (await RequireTransactionAsync(context, ct)).TraceSequenceNumber.ToString("D7", CultureInfo.InvariantCulture));
    }

    private void AddContrapartidasProviders(ICollection<IIntegrationSourceValueProvider> providers)
    {
        Add(providers, IntegrationSourceKindEnum.ClearingHouse, "clearinghouse.code", "Código cámara", "Código de la cámara asociada al ciclo.", "string", false, "Ciclo y cámara", 200, IntegrationSensitiveClassification.Technical,
            [ProcContrapartidasMethod],
            async (context, ct) => (await RequireClearingHouseAsync(context, ct)).Code);
        Add(providers, IntegrationSourceKindEnum.ClearingHouse, "clearinghouse.id", "Id cámara", "Identificador de la cámara asociada al ciclo.", "int", false, "Ciclo y cámara", 210, IntegrationSensitiveClassification.Technical,
            [ProcContrapartidasMethod],
            async (context, ct) => (await RequireClearingHouseAsync(context, ct)).Id.ToString(CultureInfo.InvariantCulture));
        Add(providers, IntegrationSourceKindEnum.Batch, "batch.id", "Id lote", "Identificador del lote persistido de la transacción.", "int", false, "Lote", 220, IntegrationSensitiveClassification.Technical,
            [ProcContrapartidasMethod],
            async (context, ct) => (await RequireTransactionAsync(context, ct)).AchBatchId.ToString(CultureInfo.InvariantCulture));
    }

    private void AddProcTransaccionesProviders(ICollection<IIntegrationSourceValueProvider> providers)
    {
        Add(providers, IntegrationSourceKindEnum.Transaction, "sourceInstitution.transitCodeNormalized", "Código banco originador", "TransitCode originador numérico sin ceros de presentación.", "int", false, "Institución", 300, IntegrationSensitiveClassification.Technical,
            [ProcTransaccionesMethod],
            async (context, ct) => (await _procTransaccionesFunctionalSourceResolver.ResolveAsync(context, ct)).SourceTransitCode);
        Add(providers, IntegrationSourceKindEnum.Transaction, "destinationInstitution.transitCodeNormalized", "Código banco receptor", "TransitCode receptor numérico sin ceros de presentación.", "int", false, "Institución", 310, IntegrationSensitiveClassification.Technical,
            [ProcTransaccionesMethod],
            async (context, ct) => (await _procTransaccionesFunctionalSourceResolver.ResolveAsync(context, ct)).DestinationTransitCode);
        Add(providers, IntegrationSourceKindEnum.Transaction, "sourceInstitution.name", "Nombre originador", "Nombre de la institución originadora.", "string", false, "Institución", 320, IntegrationSensitiveClassification.Personal,
            [ProcTransaccionesMethod],
            async (context, ct) => (await _procTransaccionesFunctionalSourceResolver.ResolveAsync(context, ct)).SourceInstitutionName);
        Add(providers, IntegrationSourceKindEnum.EntryDetail, "entrydetails.accountnumber", "Cuenta detalle entrada", "Número de cuenta del registro de detalle NACHA-M clasificado.", "string", true, "NACHA-M", 330, IntegrationSensitiveClassification.Account,
            [ProcTransaccionesMethod],
            async (context, ct) => (await _procTransaccionesFunctionalSourceResolver.ResolveAsync(context, ct)).EntryAccountNumber);
        Add(providers, IntegrationSourceKindEnum.EntryDetail, "entryDetails.recipUserName", "Nombre receptor", "Nombre del receptor del registro de detalle NACHA-M.", "string", true, "NACHA-M", 340, IntegrationSensitiveClassification.Personal,
            [ProcTransaccionesMethod],
            async (context, ct) => (await _procTransaccionesFunctionalSourceResolver.ResolveAsync(context, ct)).EntryRecipientName);
        Add(providers, IntegrationSourceKindEnum.EntryDetail, "entryDetails.recipIdNumber", "Identificación receptor", "Identificación del receptor del registro de detalle NACHA-M.", "string", true, "NACHA-M", 350, IntegrationSensitiveClassification.Personal,
            [ProcTransaccionesMethod],
            async (context, ct) => (await _procTransaccionesFunctionalSourceResolver.ResolveAsync(context, ct)).EntryRecipientId);
        Add(providers, IntegrationSourceKindEnum.Transaction, "procTransacciones.functionalBatchId", "Lote funcional", "BatchNumber NACHA-M homologado de siete a seis dígitos.", "string", false, "Proc_Transacciones", 360, IntegrationSensitiveClassification.Technical,
            [ProcTransaccionesMethod],
            async (context, ct) => (await _procTransaccionesFunctionalSourceResolver.ResolveAsync(context, ct)).FunctionalBatchId);
        Add(providers, IntegrationSourceKindEnum.Transaction, "procTransacciones.paymentInformation", "Información de pago", "Información de pago de 77 posiciones derivada de transacción, lote y addenda 05.", "string", false, "Proc_Transacciones", 370, IntegrationSensitiveClassification.Financial,
            [ProcTransaccionesMethod],
            async (context, ct) => (await _procTransaccionesFunctionalSourceResolver.ResolveAsync(context, ct)).PaymentInformation);
        Add(providers, IntegrationSourceKindEnum.Cycle, "cycle.clearingHouseId", "Id cámara del ciclo", "Identificador de cámara persistido en el ciclo.", "int", false, "Ciclo y cámara", 380, IntegrationSensitiveClassification.Technical,
            [ProcTransaccionesMethod],
            async (context, ct) => (await _procTransaccionesFunctionalSourceResolver.ResolveAsync(context, ct)).ClearingHouseId);
    }

    private void AddDifferentialResponseProviders(ICollection<IIntegrationSourceValueProvider> providers)
    {
        AddDifferential(providers, "differentialResponse.idCanal", "Id canal", "int", false, 500, attempt => attempt.IdCanal.ToString(CultureInfo.InvariantCulture));
        AddDifferential(providers, "differentialResponse.nombreCanal", "Nombre canal", "string", false, 510, attempt => attempt.NombreCanal);
        AddDifferential(providers, "differentialResponse.idTransaccion", "Id transacción", "string", false, 520, attempt => attempt.IdTransaccion);
        AddDifferential(providers, "differentialResponse.idEstado", "Id estado", "int", false, 530, attempt => attempt.IdEstado.ToString(CultureInfo.InvariantCulture));
        AddDifferential(providers, "differentialResponse.codigoCausalExterna", "Causal externa", "string", true, 540, attempt => attempt.Causal);
        AddDifferential(providers, "differentialResponse.idTransaccionServicioExterno", "Id transacción servicio externo", "int", false, 550, attempt => attempt.IdTransaccionServicioExterno.ToString(CultureInfo.InvariantCulture));
        AddDifferential(providers, "differentialResponse.descripcionCausalExterna", "Descripción causal", "string", true, 560, attempt => attempt.DescripcionCausal);
    }

    private void AddDifferential(
        ICollection<IIntegrationSourceValueProvider> providers,
        string key,
        string displayName,
        string dataType,
        bool nullable,
        int sortOrder,
        Func<AchResponseNotificationAttempt, object?> selector)
        => Add(providers, IntegrationSourceKindEnum.DifferentialResponse, key, displayName, "Valor de la notificación diferencial vigente.", dataType, nullable, "Respuesta diferencial", sortOrder, IntegrationSensitiveClassification.Technical,
            [RegistrarRespuestaMethod],
            async (context, ct) => selector(await RequireDifferentialAttemptAsync(context, ct)));

    private void Add(
        ICollection<IIntegrationSourceValueProvider> providers,
        IntegrationSourceKindEnum sourceKind,
        string sourceKey,
        string displayName,
        string description,
        string dataType,
        bool nullable,
        string functionalGroup,
        int sortOrder,
        IntegrationSensitiveClassification classification,
        IEnumerable<string> supportedMethods,
        Func<IntegrationExecutionContext, CancellationToken, Task<object?>> resolver)
    {
        providers.Add(new DelegatingSourceProvider(
            new IntegrationSourceProviderDescriptor(
                sourceKind,
                sourceKey,
                displayName,
                description,
                dataType,
                nullable,
                functionalGroup,
                sortOrder,
                classification,
                supportedMethods),
            resolver));
    }

    private async Task<AchTransaction> RequireTransactionAsync(IntegrationExecutionContext context, CancellationToken ct)
    {
        if (!context.TransactionId.HasValue)
        {
            throw new InvalidOperationException($"SOURCE_CONTEXT_REQUIRED: {context.MethodCode} requiere TransactionId.");
        }

        return await _context.AchTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == context.TransactionId.Value, ct)
            ?? throw new KeyNotFoundException($"SOURCE_TRANSACTION_NOT_FOUND: {context.TransactionId.Value}.");
    }

    private async Task<AchCycle> RequireCycleAsync(IntegrationExecutionContext context, CancellationToken ct)
    {
        var cycleId = context.CycleId;
        if (string.IsNullOrWhiteSpace(cycleId) && context.TransactionId.HasValue)
        {
            cycleId = await _context.AchTransactions
                .AsNoTracking()
                .Where(x => x.Id == context.TransactionId.Value)
                .Select(x => x.AchCycleId)
                .SingleOrDefaultAsync(ct);
        }

        if (string.IsNullOrWhiteSpace(cycleId))
        {
            throw new InvalidOperationException($"SOURCE_CONTEXT_REQUIRED: {context.MethodCode} requiere CycleId.");
        }

        return await _context.AchCycles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == cycleId, ct)
            ?? throw new KeyNotFoundException($"SOURCE_CYCLE_NOT_FOUND: {cycleId}.");
    }

    private async Task<ClearingHouse> RequireClearingHouseAsync(IntegrationExecutionContext context, CancellationToken ct)
    {
        var cycle = await RequireCycleAsync(context, ct);
        return await _context.ClearingHouses
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == cycle.ClearingHouseId, ct)
            ?? throw new KeyNotFoundException($"SOURCE_CLEARING_HOUSE_NOT_FOUND: {cycle.ClearingHouseId}.");
    }

    private async Task<AchResponseNotificationAttempt> RequireDifferentialAttemptAsync(IntegrationExecutionContext context, CancellationToken ct)
    {
        if (!context.DifferentialResponseId.HasValue)
        {
            throw new InvalidOperationException($"SOURCE_CONTEXT_REQUIRED: {context.MethodCode} requiere DifferentialResponseId.");
        }

        return await _context.AchResponseNotificationAttempts
            .AsNoTracking()
            .Where(x => x.AchResponseId == context.DifferentialResponseId.Value)
            .OrderByDescending(x => x.NumeroIntento)
            .ThenByDescending(x => x.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException($"SOURCE_DIFFERENTIAL_RESPONSE_NOT_FOUND: {context.DifferentialResponseId.Value}.");
    }

    private static readonly IReadOnlyDictionary<Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.TransactionTypeEnum, string> DebitCreditIndicators =
        new Dictionary<Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.TransactionTypeEnum, string>
        {
            [Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.TransactionTypeEnum.Debit] = "D",
            [Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.TransactionTypeEnum.Credit] = "C"
        };

    private static string DebitCreditIndicator(AchTransaction transaction)
        => DebitCreditIndicators.TryGetValue(transaction.Type, out var indicator)
            ? indicator
            : throw new InvalidOperationException("SOURCE_TRANSACTION_TYPE_UNSUPPORTED: no existe indicador D/C para el tipo transaccional.");

    private sealed class DelegatingSourceProvider : IIntegrationSourceValueProvider
    {
        private readonly Func<IntegrationExecutionContext, CancellationToken, Task<object?>> _resolver;

        public DelegatingSourceProvider(
            IntegrationSourceProviderDescriptor descriptor,
            Func<IntegrationExecutionContext, CancellationToken, Task<object?>> resolver)
        {
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        public IntegrationSourceProviderDescriptor Descriptor { get; }

        public async ValueTask<object?> ResolveAsync(
            IntegrationSourceReference source,
            IntegrationExecutionContext context,
            CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(context);
            if (source.SourceKind != Descriptor.SourceKind
                || !string.Equals(source.SourceKey, Descriptor.SourceKey, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"SOURCE_PROVIDER_MISMATCH: {Descriptor.SourceKind}:{Descriptor.SourceKey} no puede resolver {source.SourceKind}:{source.SourceKey}.");
            }

            if (!Descriptor.SupportsMethod(context.MethodCode))
            {
                throw new InvalidOperationException(
                    $"SOURCE_METHOD_INCOMPATIBLE: {Descriptor.SourceKind}:{Descriptor.SourceKey} no está habilitado para {context.MethodCode}.");
            }

            return await _resolver(context, ct);
        }
    }
}
