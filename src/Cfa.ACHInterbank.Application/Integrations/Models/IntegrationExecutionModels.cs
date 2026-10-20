using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Application.Integrations.Models;

public sealed record IntegrationExecutionContext
{
    public IntegrationExecutionContext(
        string methodCode,
        string correlationId,
        DateTime executionDateUtc,
        int? transactionId = null,
        string? cycleId = null,
        Guid? incomingNachaQueueId = null,
        Guid? differentialResponseId = null)
    {
        MethodCode = RequireText(methodCode, nameof(methodCode));
        CorrelationId = RequireText(correlationId, nameof(correlationId));
        if (executionDateUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("ExecutionDateUtc debe estar expresado en UTC.", nameof(executionDateUtc));
        }

        ExecutionDateUtc = executionDateUtc;
        TransactionId = transactionId;
        CycleId = NormalizeOptional(cycleId);
        IncomingNachaQueueId = incomingNachaQueueId;
        DifferentialResponseId = differentialResponseId;
    }

    public string MethodCode { get; }
    public int? TransactionId { get; }
    public string? CycleId { get; }
    public Guid? IncomingNachaQueueId { get; }
    public Guid? DifferentialResponseId { get; }
    public string CorrelationId { get; }
    public DateTime ExecutionDateUtc { get; }

    private static string RequireText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es obligatorio.", parameterName)
            : value.Trim();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record IntegrationSourceReference
{
    public IntegrationSourceReference(
        IntegrationSourceKindEnum sourceKind,
        string sourceKey,
        string? constantValue = null)
    {
        if (sourceKind == IntegrationSourceKindEnum.Expression)
        {
            throw new NotSupportedException("El motor contract-driven no admite fuentes basadas en expresiones arbitrarias.");
        }

        SourceKind = sourceKind;
        SourceKey = string.IsNullOrWhiteSpace(sourceKey)
            ? throw new ArgumentException("SourceKey es obligatorio.", nameof(sourceKey))
            : sourceKey.Trim();
        ConstantValue = constantValue;
    }

    public IntegrationSourceKindEnum SourceKind { get; }
    public string SourceKey { get; }
    public string? ConstantValue { get; }
}

public sealed record IntegrationExecutionParameterBinding
{
    public IntegrationExecutionParameterBinding(
        long parameterId,
        string parameterPath,
        string xmlElementName,
        string dataType,
        string xsdType,
        bool nillable,
        int minOccurs,
        int? maxOccurs,
        bool required,
        int sortOrder,
        IntegrationSourceReference source,
        string? transformationCode = null,
        string? defaultValue = null,
        string? formatMask = null,
        IntegrationParameterDirectionEnum direction = IntegrationParameterDirectionEnum.Input,
        bool isMapped = true)
    {
        if (parameterId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parameterId));
        }

        if (minOccurs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minOccurs));
        }

        if (maxOccurs is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxOccurs));
        }

        if (maxOccurs.HasValue && maxOccurs.Value < minOccurs)
        {
            throw new ArgumentException("MaxOccurs no puede ser menor que MinOccurs.", nameof(maxOccurs));
        }

        ParameterId = parameterId;
        ParameterPath = RequireText(parameterPath, nameof(parameterPath));
        XmlElementName = RequireText(xmlElementName, nameof(xmlElementName));
        DataType = RequireText(dataType, nameof(dataType));
        XsdType = RequireText(xsdType, nameof(xsdType));
        Nillable = nillable;
        MinOccurs = minOccurs;
        MaxOccurs = maxOccurs;
        Required = required;
        SortOrder = sortOrder;
        Source = source ?? throw new ArgumentNullException(nameof(source));
        TransformationCode = NormalizeOptional(transformationCode);
        DefaultValue = defaultValue;
        FormatMask = NormalizeOptional(formatMask);
        Direction = direction;
        IsMapped = isMapped;
    }

    public long ParameterId { get; }
    public string ParameterPath { get; }
    public string XmlElementName { get; }
    public string DataType { get; }
    public string XsdType { get; }
    public bool Nillable { get; }
    public int MinOccurs { get; }
    public int? MaxOccurs { get; }
    public bool Required { get; }
    public int SortOrder { get; }
    public IntegrationSourceReference Source { get; }
    public string? TransformationCode { get; }
    public string? DefaultValue { get; }
    public string? FormatMask { get; }
    public IntegrationParameterDirectionEnum Direction { get; }
    public bool IsMapped { get; }

    private static string RequireText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es obligatorio.", parameterName)
            : value.Trim();

    private static string? NormalizeOptional(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

public sealed record IntegrationExecutionContract
{
    public IntegrationExecutionContract(
        Guid contractVersionId,
        long contractRevision,
        int methodId,
        string methodCode,
        string operationName,
        string targetNamespace,
        string soapAction,
        Guid mappingSetId,
        int mappingVersion,
        long mappingRevision,
        IEnumerable<IntegrationExecutionParameterBinding> parameters)
    {
        if (contractVersionId == Guid.Empty)
        {
            throw new ArgumentException("ContractVersionId es obligatorio.", nameof(contractVersionId));
        }

        if (contractRevision <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(contractRevision));
        }

        if (methodId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(methodId));
        }

        if (mappingSetId == Guid.Empty)
        {
            throw new ArgumentException("MappingSetId es obligatorio.", nameof(mappingSetId));
        }

        if (mappingVersion <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mappingVersion));
        }

        if (mappingRevision <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mappingRevision));
        }

        var snapshot = (parameters ?? throw new ArgumentNullException(nameof(parameters)))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.ParameterPath, StringComparer.Ordinal)
            .ToArray();

        EnsureUnique(snapshot, item => item.ParameterId, "ParameterId");
        EnsureUnique(snapshot, item => item.ParameterPath, "ParameterPath", StringComparer.Ordinal);

        ContractVersionId = contractVersionId;
        ContractRevision = contractRevision;
        MethodId = methodId;
        MethodCode = RequireText(methodCode, nameof(methodCode));
        OperationName = RequireText(operationName, nameof(operationName));
        TargetNamespace = RequireText(targetNamespace, nameof(targetNamespace));
        SoapAction = RequireText(soapAction, nameof(soapAction));
        MappingSetId = mappingSetId;
        MappingVersion = mappingVersion;
        MappingRevision = mappingRevision;
        Parameters = snapshot;
    }

    public Guid ContractVersionId { get; }
    public long ContractRevision { get; }
    public int MethodId { get; }
    public string MethodCode { get; }
    public string OperationName { get; }
    public string TargetNamespace { get; }
    public string SoapAction { get; }
    public Guid MappingSetId { get; }
    public int MappingVersion { get; }
    public long MappingRevision { get; }
    public IReadOnlyList<IntegrationExecutionParameterBinding> Parameters { get; }

    private static string RequireText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es obligatorio.", parameterName)
            : value.Trim();

    private static void EnsureUnique<TItem, TKey>(
        IEnumerable<TItem> items,
        Func<TItem, TKey> keySelector,
        string keyName,
        IEqualityComparer<TKey>? comparer = null)
        where TKey : notnull
    {
        var keys = new HashSet<TKey>(comparer);
        foreach (var item in items)
        {
            if (!keys.Add(keySelector(item)))
            {
                throw new ArgumentException($"El contrato de ejecución contiene {keyName} duplicado.");
            }
        }
    }
}

public sealed record ResolvedIntegrationParameter
{
    public ResolvedIntegrationParameter(
        long parameterId,
        string parameterPath,
        string xmlElementName,
        string dataType,
        string xsdType,
        bool nillable,
        int minOccurs,
        int? maxOccurs,
        bool required,
        int sortOrder,
        object? value,
        IntegrationParameterDirectionEnum direction = IntegrationParameterDirectionEnum.Input,
        bool isPresent = true)
    {
        if (parameterId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(parameterId));
        }

        if (minOccurs < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minOccurs));
        }

        if (maxOccurs is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxOccurs));
        }

        if (maxOccurs.HasValue && maxOccurs.Value < minOccurs)
        {
            throw new ArgumentException("MaxOccurs no puede ser menor que MinOccurs.", nameof(maxOccurs));
        }

        ParameterId = parameterId;
        ParameterPath = RequireText(parameterPath, nameof(parameterPath));
        XmlElementName = RequireText(xmlElementName, nameof(xmlElementName));
        DataType = RequireText(dataType, nameof(dataType));
        XsdType = RequireText(xsdType, nameof(xsdType));
        Nillable = nillable;
        MinOccurs = minOccurs;
        MaxOccurs = maxOccurs;
        Required = required;
        SortOrder = sortOrder;
        Value = value;
        Direction = direction;
        IsPresent = isPresent;
    }

    public long ParameterId { get; }
    public string ParameterPath { get; }
    public string XmlElementName { get; }
    public string DataType { get; }
    public string XsdType { get; }
    public bool Nillable { get; }
    public int MinOccurs { get; }
    public int? MaxOccurs { get; }
    public bool Required { get; }
    public int SortOrder { get; }
    public object? Value { get; }
    public IntegrationParameterDirectionEnum Direction { get; }
    public bool IsPresent { get; }

    private static string RequireText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es obligatorio.", parameterName)
            : value.Trim();
}

public sealed record ResolvedIntegrationRequest
{
    public ResolvedIntegrationRequest(
        IntegrationExecutionContract contract,
        string correlationId,
        IEnumerable<ResolvedIntegrationParameter> parameters)
    {
        Contract = contract ?? throw new ArgumentNullException(nameof(contract));

        var snapshot = (parameters ?? throw new ArgumentNullException(nameof(parameters)))
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.ParameterPath, StringComparer.Ordinal)
            .ToArray();

        var ids = new HashSet<long>();
        var paths = new HashSet<string>(StringComparer.Ordinal);
        foreach (var parameter in snapshot)
        {
            if (!ids.Add(parameter.ParameterId) || !paths.Add(parameter.ParameterPath))
            {
                throw new ArgumentException("La solicitud resuelta contiene parámetros duplicados.", nameof(parameters));
            }
        }

        ContractVersionId = contract.ContractVersionId;
        ContractRevision = contract.ContractRevision;
        MethodCode = contract.MethodCode;
        OperationName = contract.OperationName;
        TargetNamespace = contract.TargetNamespace;
        SoapAction = contract.SoapAction;
        MappingSetId = contract.MappingSetId;
        MappingVersion = contract.MappingVersion;
        MappingRevision = contract.MappingRevision;
        CorrelationId = RequireText(correlationId, nameof(correlationId));
        Parameters = snapshot;
    }

    public IntegrationExecutionContract Contract { get; }
    public Guid ContractVersionId { get; }
    public long ContractRevision { get; }
    public string MethodCode { get; }
    public string OperationName { get; }
    public string TargetNamespace { get; }
    public string SoapAction { get; }
    public Guid MappingSetId { get; }
    public int MappingVersion { get; }
    public long MappingRevision { get; }
    public string CorrelationId { get; }
    public IReadOnlyList<ResolvedIntegrationParameter> Parameters { get; }

    private static string RequireText(string value, string parameterName)
        => string.IsNullOrWhiteSpace(value)
            ? throw new ArgumentException("El valor es obligatorio.", parameterName)
            : value.Trim();
}
