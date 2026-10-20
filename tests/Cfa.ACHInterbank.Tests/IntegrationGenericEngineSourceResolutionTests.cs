using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.Integrations.Providers;
using Cfa.ACHInterbank.Application.Integrations.Registries;
using Cfa.ACHInterbank.Application.Integrations.Services;
using Cfa.ACHInterbank.Application.Integrations.Transformations;
using Cfa.ACHInterbank.Domain.Entities.Integrations;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationGenericEngineSourceResolutionTests
{
    private const string MethodCode = "WSCFAACH.Proc_Transacciones";

    [Fact]
    public async Task Fixed_ShouldResolveFromConstantProviderBeforeDefault()
    {
        var resolver = Resolver(
        [
            new ConstantIntegrationSourceValueProvider([MethodCode])
        ]);
        var binding = Binding(
            new IntegrationSourceReference(IntegrationSourceKindEnum.Constant, "constant.value", "000123"),
            dataType: "long",
            defaultValue: "999");

        var value = await resolver.ResolveAsync(binding, Context());

        Assert.Equal("000123", value);
    }

    [Fact]
    public async Task Default_ShouldBeUsedWhenSourceReturnsNull()
    {
        var resolver = Resolver(
        [
            StubProvider("transaction.reference", null, "string", true, [MethodCode])
        ]);
        var binding = Binding(
            new IntegrationSourceReference(IntegrationSourceKindEnum.Transaction, "transaction.reference"),
            dataType: "int",
            defaultValue: "42");

        var value = await resolver.ResolveAsync(binding, Context());

        Assert.Equal("42", value);
    }

    [Fact]
    public async Task Source_ShouldResolveRegisteredProvider()
    {
        var resolver = Resolver(
        [
            StubProvider("transaction.reference", "REF-001", "string", false, [MethodCode])
        ]);
        var binding = Binding(
            new IntegrationSourceReference(IntegrationSourceKindEnum.Transaction, "transaction.reference"));

        var value = await resolver.ResolveAsync(binding, Context());

        Assert.Equal("REF-001", value);
    }

    [Fact]
    public async Task Null_ShouldRemainNullForOptionalNillableParameter()
    {
        var resolver = Resolver(
        [
            StubProvider("transaction.reference", null, "string", true, [MethodCode])
        ]);
        var binding = Binding(
            new IntegrationSourceReference(IntegrationSourceKindEnum.Transaction, "transaction.reference"),
            required: false,
            nillable: true);

        var value = await resolver.ResolveAsync(binding, Context());

        Assert.Null(value);
    }

    [Fact]
    public async Task Null_ShouldFailForRequiredNonNillableParameter()
    {
        var resolver = Resolver(
        [
            StubProvider("transaction.reference", null, "string", true, [MethodCode])
        ]);
        var binding = Binding(
            new IntegrationSourceReference(IntegrationSourceKindEnum.Transaction, "transaction.reference"),
            required: true,
            nillable: false);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await resolver.ResolveAsync(binding, Context()));

        Assert.Contains("SOURCE_VALUE_REQUIRED", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Transform_ShouldUseRegisteredStrategy()
    {
        var resolver = Resolver(
        [
            StubProvider("transaction.reference", "  ref-001  ", "string", false, [MethodCode])
        ]);
        var binding = Binding(
            new IntegrationSourceReference(IntegrationSourceKindEnum.Transaction, "transaction.reference"),
            transformationCode: "Trim");

        var value = await resolver.ResolveAsync(binding, Context());

        Assert.Equal("ref-001", value);
    }

    [Theory]
    [InlineData("int", "NOT-A-NUMBER")]
    [InlineData("decimal", "12,34,56")]
    [InlineData("boolean", "not-a-bool")]
    public async Task Type_ShouldRejectNonConvertibleResolvedValue(string targetType, string rawValue)
    {
        var resolver = Resolver(
        [
            StubProvider("transaction.reference", rawValue, "string", false, [MethodCode])
        ]);
        var binding = Binding(
            new IntegrationSourceReference(IntegrationSourceKindEnum.Transaction, "transaction.reference"),
            dataType: targetType);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await resolver.ResolveAsync(binding, Context()));

        Assert.Contains("SOURCE_TYPE_INCOMPATIBLE", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceRegistry_ShouldRejectSourceMethodIncompatibility()
    {
        var provider = StubProvider(
            "transaction.reference",
            "REF-001",
            "string",
            false,
            ["WSCFAACH.Proc_Contrapartidas"]);
        var registry = new IntegrationSourceProviderRegistry([provider]);

        var error = Assert.Throws<InvalidOperationException>(() =>
            registry.GetRequired(
                IntegrationSourceKindEnum.Transaction,
                "transaction.reference",
                MethodCode));

        Assert.Contains("SOURCE_METHOD_INCOMPATIBLE", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void SourceRegistry_ShouldRejectCaseVariantDuplicateKeys()
    {
        var first = StubProvider("transaction.reference", "A", "string", false, [MethodCode]);
        var second = StubProvider("TRANSACTION.REFERENCE", "B", "string", false, [MethodCode]);

        var error = Assert.Throws<InvalidOperationException>(() =>
            new IntegrationSourceProviderRegistry([first, second]));

        Assert.Contains("más de un source provider", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StandardTransformations_ShouldExposeCompleteExistingCatalog()
    {
        var registry = new IntegrationTransformationRegistry(StandardIntegrationTransformationCatalog.Create());

        var codes = registry.Descriptors.Select(x => x.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.True(codes.SetEquals(
        [
            "Trim", "Uppercase", "Lowercase", "PadLeft", "PadRight", "Substring", "Concat",
            "DateFormat", "NumericFormat", "NullIfEmpty", "DefaultIfNull"
        ]));
    }

    private static IntegrationParameterValueResolver Resolver(IEnumerable<IIntegrationSourceValueProvider> providers)
        => new(
            new IntegrationSourceProviderRegistry(providers),
            new IntegrationTransformationRegistry(StandardIntegrationTransformationCatalog.Create()));

    private static IntegrationExecutionContext Context()
        => new(
            MethodCode,
            "corr-p2.2",
            new DateTime(2026, 10, 13, 0, 30, 0, DateTimeKind.Utc),
            transactionId: 123,
            cycleId: "cycle-1",
            incomingNachaQueueId: Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            differentialResponseId: Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

    private static IntegrationExecutionParameterBinding Binding(
        IntegrationSourceReference source,
        string dataType = "string",
        bool required = true,
        bool nillable = false,
        string? transformationCode = null,
        string? defaultValue = null,
        string? formatMask = null)
        => new(
            1,
            "PARAM",
            "PARAM",
            dataType,
            dataType,
            nillable,
            required ? 1 : 0,
            1,
            required,
            1,
            source,
            transformationCode,
            defaultValue,
            formatMask);

    private static IIntegrationSourceValueProvider StubProvider(
        string sourceKey,
        object? value,
        string dataType,
        bool nullable,
        IEnumerable<string> supportedMethods)
        => new StubSourceProvider(
            new IntegrationSourceProviderDescriptor(
                IntegrationSourceKindEnum.Transaction,
                sourceKey,
                "Fuente",
                "Fuente de prueba.",
                dataType,
                nullable,
                "Prueba",
                1,
                IntegrationSensitiveClassification.None,
                supportedMethods),
            value);

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
}
