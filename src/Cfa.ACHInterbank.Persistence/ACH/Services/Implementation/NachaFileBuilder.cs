using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Nacha.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
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

    public async Task<string> BuildNachaFileAsync(IEnumerable<int> batchIds, CancellationToken ct)
    {
        var batches = await _context.AchBatches
            .Include(b => b.ClearingHouse)
            .Include(b => b.Transactions)
                .ThenInclude(t => t.Addendas)
            .Where(b => batchIds.Contains(b.Id))
            .ToListAsync(ct);

        if (!batches.Any())
            throw new InvalidOperationException("No se encontraron lotes para exportar.");

        var sb = new StringBuilder();

        // 1️⃣ Encabezado de archivo (record type "FILE_HEADER")
        var headerDto = new FileHeaderDto
        {
            PriorityCode = "01",
            ImmediateDestination = "0000000000",
            ImmediateOrigin = "0000000000",
            FileCreationDate = DateTime.UtcNow,
            FileCreationTime = DateTime.UtcNow,
            ReferenceCode = "REF12345"
        };
        sb.Append(await BuildRecordAsync("FILE_HEADER", headerDto, ct));

        long totalDebit = 0, totalCredit = 0;
        int fileEntryAddendaCount = 0;
        int fileBatchCount = 0;
        int recordCount = 1; // contamos el header de archivo

        foreach (var batch in batches)
        {
            fileBatchCount++;

            // 5️⃣ Encabezado de lote
            var batchHeader = new BatchHeaderDto
            {
                ServiceClassCode = "220",
                CompanyName = batch.CompanyName,
                CompanyIdentification = batch.CompanyIdentification,
                CompanyEntryDescription = "PPD",
                OriginOrOdfi = batch.ClearingHouse.Code,
                EffectiveEntryDate = batch.EffectiveEntryDate
            };
            sb.Append(await BuildRecordAsync("BATCH_HEADER", batchHeader, ct));
            recordCount++;

            long batchDebit = 0, batchCredit = 0;
            int batchEntryAddenda = 0;

            foreach (var tx in batch.Transactions)
            {
                var entry = new EntryDetailDto
                {
                    TransactionCode = tx.Type == TransactionTypeEnum.Credit ? "22" : "27",
                    RoutingNumber = tx.DestinationInstitution.RoutingNumber,
                    AccountNumber = tx.DestinationAccountNumber,
                    AmountCents = (long)decimal.Truncate(tx.Amount * 100m),
                    Reference = tx.Reference
                };
                sb.Append(await BuildRecordAsync("ENTRY_DETAIL", entry, ct));
                recordCount++;
                batchEntryAddenda++;

                if (tx.Type == TransactionTypeEnum.Debit)
                    batchDebit += (long)decimal.Truncate(tx.Amount * 100m);
                else
                    batchCredit += (long)decimal.Truncate(tx.Amount * 100m);

                if (tx.Addendas != null)
                {
                    int seq = 1;
                    foreach (var add in tx.Addendas)
                    {
                        var addDto = new AddendaFileDto
                        {
                            Information = add.Information ?? string.Empty,
                            SequenceNumber = seq,
                            EntryDetailSequenceNumber = tx.Id
                        };
                        sb.Append(await BuildRecordAsync("ADDENDA", addDto, ct));
                        recordCount++;
                        batchEntryAddenda++;
                        seq++;
                    }
                }
            }

            // 8️⃣ Control de lote
            var batchControl = new BatchControlDto
            {
                ServiceClassCode = "220",
                EntryAddendaCount = batchEntryAddenda,
                TotalDebitAmountCents = batchDebit,
                TotalCreditAmountCents = batchCredit,
                CompanyIdentification = batch.CompanyIdentification
            };
            sb.Append(await BuildRecordAsync("BATCH_CONTROL", batchControl, ct));
            recordCount++;

            totalDebit += batchDebit;
            totalCredit += batchCredit;
            fileEntryAddendaCount += batchEntryAddenda;
        }

        // 9️⃣ Control de archivo
        int blockCount = (int)Math.Ceiling(recordCount / 10.0);
        var fileControl = new FileControlDto
        {
            BatchCount = fileBatchCount,
            BlockCount = blockCount,
            EntryAddendaCount = fileEntryAddendaCount,
            TotalDebitAmountCents = totalDebit,
            TotalCreditAmountCents = totalCredit
        };
        sb.Append(await BuildRecordAsync("FILE_CONTROL", fileControl, ct));

        return sb.ToString();
    }

    /// <summary>
    /// Genera una línea de 106 caracteres según la definición en las tablas
    /// NachaRecordLayout y NachaRecordField.
    /// </summary>
    public async Task<string> BuildRecordAsync<T>(string recordType, T entity, CancellationToken ct)
    {
        var layout = await _context.NachaRecordLayouts
            .Include(l => l.Fields)
            .FirstOrDefaultAsync(l => l.RecordType == recordType, ct)
            ?? throw new InvalidOperationException($"Layout no encontrado para '{recordType}'.");

        var fields = layout.Fields.OrderBy(f => f.StartPosition).ToList();
        var buffer = new char[layout.TotalLength];
        Array.Fill(buffer, ' ');

        if (!string.IsNullOrEmpty(layout.RecordCode))
            buffer[0] = layout.RecordCode[0];

        foreach (var field in fields)
        {
            var prop = typeof(T).GetProperty(field.DbColumn);
            if (prop == null) continue;

            object? raw = prop.GetValue(entity);
            string value = FormatValue(raw, field);

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

    // Formatea según la definición de cada campo
    private static string FormatValue(object? raw, NachaRecordField field)
    {
        if (raw == null) return string.Empty;

        if (!string.IsNullOrWhiteSpace(field.Format))
        {
            if (raw is DateTime dt) return dt.ToString(field.Format, CultureInfo.InvariantCulture);
            if (raw is int i) return i.ToString(field.Format, CultureInfo.InvariantCulture);
            if (raw is long l) return l.ToString(field.Format, CultureInfo.InvariantCulture);
            if (raw is decimal d) return d.ToString(field.Format, CultureInfo.InvariantCulture);
        }
        return raw.ToString() ?? string.Empty;
    }
}
