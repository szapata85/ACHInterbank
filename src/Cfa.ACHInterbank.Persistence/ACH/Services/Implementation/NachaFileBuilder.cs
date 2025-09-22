using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaFileBuilder : INachaFileBuilder
{
    private readonly AchDbContext _context;

    public NachaFileBuilder(AchDbContext context)
    {
        _context = context;
    }

    public async Task<string> BuildRecordAsync<T>(string recordType, T entity, CancellationToken ct)
    {
        var layout = await _context.NachaRecordLayouts
            .Include(l => l.Fields.OrderBy(f => f.StartPosition))
            .FirstAsync(l => l.RecordType == recordType, ct);

        var buffer = new char[layout.TotalLength];
        Array.Fill(buffer, ' ');

        foreach (var field in layout.Fields)
        {
            var prop = typeof(T).GetProperty(field.DbColumn);
            if (prop == null) continue;

            var rawValue = prop.GetValue(entity);
            string value = FormatValue(rawValue, field);

            if (value.Length > field.Length)
                value = value.Substring(0, field.Length);

            value = field.Justification == 'R'
                ? value.PadLeft(field.Length, field.PadChar)
                : value.PadRight(field.Length, field.PadChar);

            int start = field.StartPosition - 1;
            for (int i = 0; i < value.Length; i++)
                buffer[start + i] = value[i];
        }

        return new string(buffer);
    }

    public async Task<string> BuildNachaFileAsync(IEnumerable<AchTransaction> transactions, CancellationToken ct)
    {
        var sb = new StringBuilder();
        // Ejemplo: header
        sb.Append(await BuildRecordAsync("HEADER", new { /* campos generales */ }, ct));

        // Detalles
        foreach (var tx in transactions)
            sb.Append(await BuildRecordAsync("ENTRY_DETAIL", tx, ct));

        // Control
        sb.Append(await BuildRecordAsync("FILE_CONTROL", new { /* totales */ }, ct));

        // Devuelve todo en una sola cadena sin saltos de línea
        return sb.ToString();
    }

    private static string FormatValue(object? raw, NachaRecordField field)
    {
        if (raw == null) return "";
        if (raw is DateTime dt && !string.IsNullOrEmpty(field.Format))
            return dt.ToString(field.Format, CultureInfo.InvariantCulture);
        if (raw is decimal dec && !string.IsNullOrEmpty(field.Format))
            return dec.ToString(field.Format, CultureInfo.InvariantCulture);
        return raw.ToString() ?? "";
    }
}
