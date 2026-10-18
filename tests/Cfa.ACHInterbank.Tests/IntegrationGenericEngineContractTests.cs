using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.Integrations.Registries;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationGenericEngineContractTests
{
    [Fact]
    public void ExecutionContract_ShouldSnapshotAndOrderBindings()
    {
        var source = new IntegrationSourceReference(IntegrationSourceKindEnum.Transaction, "transaction.reference");
        var first = Binding(1, "first", 10, source);
        var second = Binding(2, "second", 20, source);
        var mutable = new List<IntegrationExecutionParameterBinding> { second, first };

        var contract = Contract(mutable);
        mutable.Clear();

        Assert.Equal(["first", "second"], contract.Parameters.Select(item => item.ParameterPath).ToArray());
        Assert.Equal(2, contract.Parameters.Count);
    }

    [Fact]
    public void ExecutionContract_ShouldRejectDuplicateParameterPath()
    {
        var source = new IntegrationSourceReference(IntegrationSourceKindEnum.Transaction, "transaction.reference");

        var error = Assert.Throws<ArgumentException>(() => Contract(
        [
            Binding(1, "same", 10, source),
            Binding(2, "same", 20, source)
        ]));

        Assert.Contains("ParameterPath duplicado", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceReference_ShouldRejectArbitraryExpressionSource()
    {
        var error = Assert.Throws<NotSupportedException>(() =>
            new IntegrationSourceReference(IntegrationSourceKindEnum.Expression, "transaction.Amount + 1"));

        Assert.Contains("expresiones arbitrarias", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceRegistry_ShouldResolveExactProviderAndRejectDuplicateKeys()
    {
        var descriptor = SourceDescriptor(IntegrationSourceKindEnum.Transaction, "transaction.reference");
        var provider = new StubSourceProvider(descriptor, "ABC");
        var registry = new IntegrationSourceProviderRegistry([provider]);

        Assert.Same(provider, registry.GetRequired(IntegrationSourceKindEnum.Transaction, "transaction.reference"));
        Assert.Single(registry.Descriptors);

        var duplicate = new StubSourceProvider(descriptor, "DEF");
        var error = Assert.Throws<InvalidOperationException>(() =>
            new IntegrationSourceProviderRegistry([provider, duplicate]));
        Assert.Contains("más de un source provider", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TransformationRegistry_ShouldResolveCaseInsensitiveCodeAndRejectDuplicates()
    {
        var descriptor = new IntegrationTransformationDescriptor(
            "trim",
            ["string"],
            "string",
            false,
            true,
            "Recortar",
            "Recorta espacios laterales.");
        var transformation = new StubTransformation(descriptor);
        var registry = new IntegrationTransformationRegistry([transformation]);

        Assert.Same(transformation, registry.GetRequired("TRIM"));
        Assert.Single(registry.Descriptors);

        var duplicate = new StubTransformation(new IntegrationTransformationDescriptor(
            "TRIM",
            ["string"],
            "string",
            false,
            true,
            "Duplicada",
            "Duplicada para probar fail-fast."));
        var error = Assert.Throws<InvalidOperationException>(() =>
            new IntegrationTransformationRegistry([transformation, duplicate]));
        Assert.Contains("más de una transformation", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void TransformationDescriptor_ShouldSnapshotInputTypes()
    {
        var inputTypes = new List<string> { "decimal", "string" };
        var descriptor = new IntegrationTransformationDescriptor(
            "format",
            inputTypes,
            "string",
            true,
            false,
            "Formatear",
            "Aplica una máscara registrada.");

        inputTypes.Clear();

        Assert.Equal(["decimal", "string"], descriptor.InputTypes);
    }

    [Fact]
    public void ResolvedRequest_ShouldSnapshotAndOrderParameters()
    {
        var mutable = new List<ResolvedIntegrationParameter>
        {
            ResolvedParameter(2, "second", 20, "B"),
            ResolvedParameter(1, "first", 10, "A")
        };

        var request = new ResolvedIntegrationRequest(
            Contract(
            [
                Binding(1, "first", 10, new IntegrationSourceReference(IntegrationSourceKindEnum.Transaction, "transaction.reference")),
                Binding(2, "second", 20, new IntegrationSourceReference(IntegrationSourceKindEnum.Transaction, "transaction.reference.2"))
            ]),
            "corr-001",
            mutable);
        mutable.Clear();

        Assert.Equal(["first", "second"], request.Parameters.Select(item => item.ParameterPath).ToArray());
        Assert.Equal(2, request.Parameters.Count);
    }

    private static IntegrationExecutionContract Contract(IEnumerable<IntegrationExecutionParameterBinding> parameters)
        => new(
            Guid.NewGuid(),
            2,
            1,
            "WSCFAACH.Proc_Transacciones",
            "Proc_Transacciones",
            "http://tempuri.org/",
            "http://tempuri.org/IWSCFAACH/Proc_Transacciones",
            Guid.NewGuid(),
            1,
            2,
            parameters);

    private static IntegrationExecutionParameterBinding Binding(
        long id,
        string path,
        int sortOrder,
        IntegrationSourceReference source)
        => new(
            id,
            path,
            path,
            "string",
            "string",
            false,
            1,
            1,
            true,
            sortOrder,
            source);

    private static ResolvedIntegrationParameter ResolvedParameter(
        long id,
        string path,
        int sortOrder,
        object? value)
        => new(id, path, path, "string", "string", false, 1, 1, true, sortOrder, value);

    private static IntegrationSourceProviderDescriptor SourceDescriptor(
        IntegrationSourceKindEnum kind,
        string key)
        => new(
            kind,
            key,
            "Referencia",
            "Referencia funcional registrada.",
            "string",
            false,
            "Transacción",
            10);

    private sealed class StubSourceProvider : IIntegrationSourceValueProvider
    {
        private readonly object? _value;

        public StubSourceProvider(IntegrationSourceProviderDescriptor descriptor, object? value)
        {
            Descriptor = descriptor;
            _value = value;
        }

        public IntegrationSourceProviderDescriptor Descriptor { get; }

        public ValueTask<object?> ResolveAsync(
            IntegrationSourceReference source,
            IntegrationExecutionContext context,
            CancellationToken ct = default)
            => ValueTask.FromResult(_value);
    }

    private sealed class StubTransformation : IIntegrationTransformation
    {
        public StubTransformation(IntegrationTransformationDescriptor descriptor)
        {
            Descriptor = descriptor;
        }

        public IntegrationTransformationDescriptor Descriptor { get; }

        public object? Apply(object? input, IntegrationTransformationContext context) => input;
    }
}
