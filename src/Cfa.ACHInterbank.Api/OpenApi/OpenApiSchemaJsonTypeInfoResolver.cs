using System.Text.Json.Serialization.Metadata;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Api.OpenApi;

internal static class OpenApiSchemaJsonTypeInfoResolver
{
    private static readonly HashSet<string> NachaHeaderNavigationProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(NachaHeader.ClearingHouse),
        nameof(NachaHeader.AchCycle),
        nameof(NachaHeader.IncomingNachaFileIngestion),
        nameof(NachaHeader.Batches),
        nameof(NachaHeader.EntryDetails),
        nameof(NachaHeader.AddendaRecords),
        nameof(NachaHeader.BatchControls),
        nameof(NachaHeader.FileControls)
    };

    private static readonly HashSet<string> AchTransactionNavigationProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(AchTransaction.SourceInstitution),
        nameof(AchTransaction.DestinationInstitution),
        nameof(AchTransaction.AchCycle),
        nameof(AchTransaction.AchBatch),
        nameof(AchTransaction.Addendas),
        nameof(AchTransaction.StateEvents),
        nameof(AchTransaction.ContrapartidaDispatchItem)
    };

    public static IJsonTypeInfoResolver Create()
    {
        var resolver = new DefaultJsonTypeInfoResolver();
        resolver.Modifiers.Add(RemoveNachaHeaderNavigationProperties);
        return resolver;
    }

    private static void RemoveNachaHeaderNavigationProperties(JsonTypeInfo typeInfo)
    {
        var navigationProperties = typeInfo.Type switch
        {
            var type when type == typeof(NachaHeader) => NachaHeaderNavigationProperties,
            var type when type == typeof(AchTransaction) => AchTransactionNavigationProperties,
            _ => null
        };
        if (navigationProperties is null)
        {
            return;
        }

        for (var index = typeInfo.Properties.Count - 1; index >= 0; index--)
        {
            if (navigationProperties.Contains(typeInfo.Properties[index].Name))
            {
                typeInfo.Properties.RemoveAt(index);
            }
        }
    }
}
