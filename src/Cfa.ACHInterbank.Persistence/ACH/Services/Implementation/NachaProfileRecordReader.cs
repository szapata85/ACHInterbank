using System.Text.Json;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Immutable reader for the physical layout selected by NachaConfig. It keeps the parser
/// independent from chamber-specific offsets and chooses record variants declaratively.
/// </summary>
internal sealed class NachaProfileRecordReader
{
    private readonly IReadOnlyDictionary<string, IReadOnlyList<VariantSnapshot>> _variantsByRecord;

    private NachaProfileRecordReader(
        int profileId,
        string profileCode,
        IReadOnlyDictionary<string, IReadOnlyList<VariantSnapshot>> variantsByRecord,
        int recordLength)
    {
        ProfileId = profileId;
        ProfileCode = profileCode;
        _variantsByRecord = variantsByRecord;
        RecordLength = recordLength;
    }

    public int ProfileId { get; }
    public string ProfileCode { get; }
    public int RecordLength { get; }

    public static async Task<NachaProfileRecordReader> LoadAsync(
        AchDbContext context,
        int profileId,
        CancellationToken cancellationToken)
    {
        var profileCode = await context.CfgProfiles
            .AsNoTracking()
            .Where(profile => profile.Id == profileId)
            .Select(profile => profile.ProfileCode)
            .SingleOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(profileCode))
        {
            throw new InvalidOperationException($"NACHA_PROFILE_SNAPSHOT_NOT_FOUND: no existe el perfil seleccionado {profileId}.");
        }

        var variants = await context.CfgLayoutVariants
            .AsNoTracking()
            .Include(variant => variant.RecordCode)
            .Include(variant => variant.Fields)
            .Where(variant => variant.ProfileId == profileId)
            .OrderBy(variant => variant.Priority)
            .ThenBy(variant => variant.Id)
            .ToListAsync(cancellationToken);

        if (variants.Count == 0)
        {
            throw new InvalidOperationException($"NACHA_PROFILE_LAYOUT_EMPTY: el perfil {profileCode} no contiene variantes.");
        }

        var recordLengths = variants.Select(variant => variant.TotalLength).Distinct().ToArray();
        if (recordLengths.Length != 1 || recordLengths[0] <= 0)
        {
            throw new InvalidOperationException($"NACHA_PROFILE_RECORD_LENGTH_AMBIGUOUS: el perfil {profileCode} no define una longitud física única.");
        }

        var snapshots = variants
            .GroupBy(variant => variant.RecordCode.Code, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<VariantSnapshot>)group.Select(variant => new VariantSnapshot(
                        variant.VariantCode,
                        variant.IsDefaultForRecord,
                        variant.Priority,
                        variant.SelectionPredicateJson,
                        variant.Fields
                            .Where(field => field.IsEnabled)
                            .ToDictionary(
                                field => field.FieldCode,
                                field => new FieldSnapshot(field.StartPosition, field.Length),
                                StringComparer.OrdinalIgnoreCase)))
                    .ToList(),
                StringComparer.OrdinalIgnoreCase);

        return new NachaProfileRecordReader(profileId, profileCode, snapshots, recordLengths[0]);
    }

    public string Read(
        string record,
        string recordCode,
        string fieldCode,
        IReadOnlyDictionary<string, string>? selectionContext = null)
    {
        if (record.Length != RecordLength)
        {
            throw new InvalidOperationException(
                $"NACHA_PROFILE_RECORD_LENGTH: el registro tipo {recordCode} no tiene {RecordLength} caracteres.");
        }

        if (!_variantsByRecord.TryGetValue(recordCode, out var variants))
        {
            throw new InvalidOperationException(
                $"NACHA_PROFILE_RECORD_NOT_CONFIGURED: el perfil {ProfileCode} no configura el registro {recordCode}.");
        }

        var selected = SelectVariant(record, recordCode, variants, selectionContext);

        return ReadField(record, selected, fieldCode);
    }

    public bool IsReturnAddenda(string record)
    {
        const string recordCode = "7";
        if (record.Length != RecordLength || record[0] != recordCode[0])
        {
            return false;
        }

        if (!_variantsByRecord.TryGetValue(recordCode, out var variants))
        {
            return false;
        }

        var selected = SelectVariant(record, recordCode, variants, selectionContext: null);
        return selected.Fields.ContainsKey("RETURNREASONCODE")
               && selected.Fields.ContainsKey("ORIGINALTRACENUMBER");
    }

    private VariantSnapshot SelectVariant(
        string record,
        string recordCode,
        IReadOnlyList<VariantSnapshot> variants,
        IReadOnlyDictionary<string, string>? selectionContext)
        => variants
               .Where(variant => !variant.IsDefault && PredicateMatches(variant, record, selectionContext))
               .OrderBy(variant => variant.Priority)
               .FirstOrDefault()
           ?? variants.FirstOrDefault(variant => variant.IsDefault)
           ?? throw new InvalidOperationException(
               $"NACHA_PROFILE_VARIANT_NOT_FOUND: el perfil {ProfileCode} no resolvió variante para el registro {recordCode}.");

    private static bool PredicateMatches(
        VariantSnapshot variant,
        string record,
        IReadOnlyDictionary<string, string>? selectionContext)
    {
        if (string.IsNullOrWhiteSpace(variant.SelectionPredicateJson))
        {
            return false;
        }

        using var document = JsonDocument.Parse(variant.SelectionPredicateJson);
        foreach (var predicate in document.RootElement.EnumerateObject())
        {
            var expected = predicate.Value.GetString() ?? string.Empty;
            string? actual = null;
            if (string.Equals(predicate.Name, "AddendaType", StringComparison.OrdinalIgnoreCase)
                && variant.Fields.ContainsKey("ADDENDATYPE"))
            {
                actual = ReadField(record, variant, "ADDENDATYPE").Trim();
            }
            else if (selectionContext is not null)
            {
                selectionContext.TryGetValue(predicate.Name, out actual);
            }

            if (!string.Equals(actual?.Trim(), expected.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static string ReadField(string record, VariantSnapshot variant, string fieldCode)
    {
        if (!variant.Fields.TryGetValue(fieldCode, out var field))
        {
            throw new InvalidOperationException(
                $"NACHA_PROFILE_FIELD_NOT_CONFIGURED: la variante {variant.Code} no configura {fieldCode}.");
        }

        var zeroBasedStart = field.StartPosition - 1;
        if (zeroBasedStart < 0 || field.Length <= 0 || zeroBasedStart + field.Length > record.Length)
        {
            throw new InvalidOperationException(
                $"NACHA_PROFILE_FIELD_BOUNDARY: {variant.Code}.{fieldCode} excede el registro físico.");
        }

        return record.Substring(zeroBasedStart, field.Length);
    }

    private sealed record VariantSnapshot(
        string Code,
        bool IsDefault,
        int Priority,
        string? SelectionPredicateJson,
        IReadOnlyDictionary<string, FieldSnapshot> Fields);

    private sealed record FieldSnapshot(int StartPosition, int Length);
}
