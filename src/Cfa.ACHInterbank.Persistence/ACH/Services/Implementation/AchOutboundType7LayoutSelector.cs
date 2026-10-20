using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal static class AchOutboundType7LayoutSelector
{
    public static CfgLayoutVariant Select(
        IReadOnlyList<CfgLayoutVariant> variants,
        AchAddendaBusinessType businessType,
        bool isPrenotification)
    {
        var businessTypeValue = businessType switch
        {
            AchAddendaBusinessType.Credit => "CREDIT",
            AchAddendaBusinessType.Debit => "DEBIT",
            _ => throw new NachaGenerationException(
                "ACHCOL-T7-ADDENDA-TYPE",
                "El tipo de negocio de la adenda no está soportado para ACHCOL.")
        };
        var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["BusinessType"] = businessTypeValue
        };
        if (isPrenotification)
        {
            context["TransactionFamily"] = "PRENOTIFICATION";
        }

        var predicateMatches = new List<CfgLayoutVariant>();
        var defaults = new List<CfgLayoutVariant>();
        foreach (var variant in variants)
        {
            if (string.IsNullOrWhiteSpace(variant.SelectionPredicateJson))
            {
                if (variant.IsDefaultForRecord)
                {
                    defaults.Add(variant);
                }

                continue;
            }

            if (variant.IsDefaultForRecord)
            {
                throw new NachaGenerationException(
                    "NACHA_T7_SELECTION_DEFAULT_INVALID",
                    "La variante T7 default publicada debe declarar un selector vacío.");
            }

            var predicate = ParsePredicate(variant.SelectionPredicateJson);
            if (predicate.All(expected => context.TryGetValue(expected.Key, out var actual)
                                              && string.Equals(actual, expected.Value, StringComparison.OrdinalIgnoreCase)))
            {
                predicateMatches.Add(variant);
            }
        }

        if (predicateMatches.Count == 1)
        {
            return predicateMatches[0];
        }

        if (predicateMatches.Count > 1)
        {
            throw new NachaGenerationException(
                "NACHA_T7_SELECTION_AMBIGUOUS",
                "Más de una variante T7 publicada coincide con el contexto semántico.");
        }

        return defaults.Count switch
        {
            1 => defaults[0],
            0 => throw new NachaGenerationException(
                "NACHA_T7_SELECTION_DEFAULT_MISSING",
                "Ninguna variante T7 publicada coincide y no existe un default válido."),
            _ => throw new NachaGenerationException(
                "NACHA_T7_SELECTION_DEFAULT_AMBIGUOUS",
                "Ninguna variante T7 publicada coincide y existe más de un default válido.")
        };
    }

    private static IReadOnlyDictionary<string, string> ParsePredicate(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return InvalidPredicate();
            }

            var predicate = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.IsNullOrWhiteSpace(property.Name)
                    || property.Value.ValueKind != JsonValueKind.String
                    || string.IsNullOrWhiteSpace(property.Value.GetString())
                    || !predicate.TryAdd(property.Name, property.Value.GetString()!))
                {
                    return InvalidPredicate();
                }
            }

            return predicate.Count > 0 ? predicate : InvalidPredicate();
        }
        catch (JsonException)
        {
            return InvalidPredicate();
        }
    }

    private static IReadOnlyDictionary<string, string> InvalidPredicate()
        => throw new NachaGenerationException(
            "NACHA_T7_SELECTION_PREDICATE_INVALID",
            "SelectionPredicateJson T7 debe ser un diccionario JSON no vacío de strings no vacíos.");
}
