using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

public class NachaLayoutSeeder : IDbSeeder
{
    private readonly AchDbContext _context;
    public int Order => 5;

    public NachaLayoutSeeder(AchDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        if (await _context.NachaRecordLayouts.AnyAsync())
        {
            await EnsureHeaderOriginAndDestinationLeftPaddingAsync();
            return;
        }

        var layouts = new List<NachaRecordLayout>
            {
                // ╔══════════════════════════════════════════════════════════╗
                // ║  Registro 1 - Encabezado de archivo                      ║
                // ╚══════════════════════════════════════════════════════════╝
                new NachaRecordLayout
                {
                    RecordType = "FILE_HEADER",
                    RecordCode = "1",
                    TotalLength = 106,
                    Description = "Registro encabezado de archivo NACHA-M (ACH Colombia)",
                    Fields = new List<NachaRecordField>
                    {
                        new NachaRecordField { FieldName="PriorityCode", StartPosition=2, Length=2, PadChar='0', Justification='R', DbColumn="PriorityCode" },
                        new NachaRecordField { FieldName="ImmediateDestination", StartPosition=4, Length=10, PadChar='0', Justification='R', DbColumn="ImmediateDestination" },
                        new NachaRecordField { FieldName="ImmediateOrigin", StartPosition=14, Length=10, PadChar='0', Justification='R', DbColumn="ImmediateOrigin" },
                        new NachaRecordField { FieldName="FileCreationDate", StartPosition=24, Length=8, PadChar='0', Justification='R', DbColumn="FileCreationDate", Format="yyyyMMdd" },
                        new NachaRecordField { FieldName="FileCreationTime", StartPosition=32, Length=4, PadChar='0', Justification='R', DbColumn="FileCreationTime", Format="HHmm" },
                        new NachaRecordField { FieldName="FileIdModifier", StartPosition=36, Length=1, PadChar='A', Justification='L', DbColumn="FileIdModifier" },
                        new NachaRecordField { FieldName="RecordSize", StartPosition=37, Length=3, PadChar='0', Justification='R', DbColumn="RecordSize" },
                        new NachaRecordField { FieldName="BlockingFactor", StartPosition=40, Length=2, PadChar='0', Justification='R', DbColumn="BlockingFactor" },
                        new NachaRecordField { FieldName="FormatCode", StartPosition=42, Length=1, PadChar='0', Justification='R', DbColumn="FormatCode" },
                        new NachaRecordField { FieldName="ImmediateDestinationName", StartPosition=43, Length=23, PadChar=' ', Justification='L', DbColumn="ImmediateDestinationName" },
                        new NachaRecordField { FieldName="ImmediateOriginName", StartPosition=66, Length=23, PadChar=' ', Justification='L', DbColumn="ImmediateOriginName" },
                        new NachaRecordField { FieldName="ReferenceCode", StartPosition=89, Length=8, PadChar=' ', Justification='L', DbColumn="ReferenceCode" }
                    }
                },

                // ╔══════════════════════════════════════════════════════════╗
                // ║  Registro 5 - Encabezado de lote                         ║
                // ╚══════════════════════════════════════════════════════════╝
                new NachaRecordLayout
                {
                    RecordType = "BATCH_HEADER",
                    RecordCode = "5",
                    TotalLength = 106,
                    Description = "Registro encabezado de lote NACHA-M",
                    Fields = new List<NachaRecordField>
                    {
                        new NachaRecordField { FieldName="ServiceClassCode", StartPosition=2, Length=3, PadChar='0', Justification='R', DbColumn="ServiceClassCode" },
                        new NachaRecordField { FieldName="CompanyName", StartPosition=5, Length=16, PadChar=' ', Justification='L', DbColumn="CompanyName" },
                        new NachaRecordField { FieldName="CompanyDiscretionaryData", StartPosition=21, Length=20, PadChar=' ', Justification='L', DbColumn="CompanyDiscretionaryData" },
                        new NachaRecordField { FieldName="CompanyIdentification", StartPosition=41, Length=10, PadChar=' ', Justification='L', DbColumn="CompanyIdentification" },
                        new NachaRecordField { FieldName="StandardEntryClassCode", StartPosition=51, Length=3, PadChar=' ', Justification='L', DbColumn="StandardEntryClassCode" },
                        new NachaRecordField { FieldName="CompanyEntryDescription", StartPosition=54, Length=10, PadChar=' ', Justification='L', DbColumn="CompanyEntryDescription" },
                        new NachaRecordField { FieldName="CompanyDescriptiveDate", StartPosition=64, Length=8, PadChar=' ', Justification='L', DbColumn="CompanyDescriptiveDate", Format="yyyyMMdd" },
                        new NachaRecordField { FieldName="EffectiveEntryDate", StartPosition=72, Length=8, PadChar='0', Justification='R', DbColumn="EffectiveEntryDate", Format="yyyyMMdd" },
                        new NachaRecordField { FieldName="SettlementDate", StartPosition=80, Length=3, PadChar='0', Justification='R', DbColumn="SettlementDate" },
                        new NachaRecordField { FieldName="OriginatorStatusCode", StartPosition=83, Length=1, PadChar='0', Justification='R', DbColumn="OriginatorStatusCode" },
                        new NachaRecordField { FieldName="OriginatingDFI", StartPosition=84, Length=8, PadChar='0', Justification='R', DbColumn="OriginatingDFI" },
                        new NachaRecordField { FieldName="BatchNumber", StartPosition=92, Length=7, PadChar='0', Justification='R', DbColumn="BatchNumber" }
                    }
                },

                // ╔══════════════════════════════════════════════════════════╗
                // ║  Registro 6 - Detalle de transacción                     ║
                // ╚══════════════════════════════════════════════════════════╝
                new NachaRecordLayout
                {
                    RecordType = "ENTRY_DETAIL",
                    RecordCode = "6",
                    TotalLength = 106,
                    Description = "Registro detalle de transacción NACHA-M",
                    Fields = new List<NachaRecordField>
                    {
                        new NachaRecordField { FieldName="TransactionCode", StartPosition=2, Length=2, PadChar='0', Justification='R', DbColumn="TransactionCode" },
                        new NachaRecordField { FieldName="ReceivingDFI", StartPosition=4, Length=8, PadChar='0', Justification='R', DbColumn="ReceivingDFI" },
                        new NachaRecordField { FieldName="CheckDigit", StartPosition=12, Length=1, PadChar='0', Justification='R', DbColumn="CheckDigit" },
                        new NachaRecordField { FieldName="AccountNumber", StartPosition=13, Length=17, PadChar=' ', Justification='L', DbColumn="DestinationAccountNumber" },
                        new NachaRecordField { FieldName="Amount", StartPosition=30, Length=18, PadChar='0', Justification='R', DbColumn="Amount" },
                        new NachaRecordField { FieldName="RecipientIdNumber", StartPosition=48, Length=15, PadChar=' ', Justification='L', DbColumn="RecipientIdNumber" },
                        new NachaRecordField { FieldName="ReceiverName", StartPosition=63, Length=22, PadChar=' ', Justification='L', DbColumn="ReceiverName" },
                        new NachaRecordField { FieldName="DiscretionaryData", StartPosition=85, Length=2, PadChar=' ', Justification='L', DbColumn="DiscretionaryData" },
                        new NachaRecordField { FieldName="AddendumIndicator", StartPosition=87, Length=1, PadChar='0', Justification='R', DbColumn="AddendumIndicator" },
                        new NachaRecordField { FieldName="TraceNumber", StartPosition=88, Length=15, PadChar='0', Justification='R', DbColumn="TraceNumber" }
                    }
                },

                // ╔══════════════════════════════════════════════════════════╗
                // ║  Registro 7 - Addenda (información adicional)            ║
                // ╚══════════════════════════════════════════════════════════╝
                new NachaRecordLayout
                {
                    RecordType = "ADDENDA",
                    RecordCode = "7",
                    TotalLength = 106,
                    Description = "Registro Addenda NACHA-M (información adicional de AchTransactionAddenda)",
                    Fields = new List<NachaRecordField>
                    {
                        new NachaRecordField { FieldName="AddendaType", StartPosition=2, Length=2, PadChar='0', Justification='R', DbColumn="AddendaType" },
                        new NachaRecordField { FieldName="Information", StartPosition=4, Length=80, PadChar=' ', Justification='L', DbColumn="Information" },
                        new NachaRecordField { FieldName="SequenceNumber", StartPosition=84, Length=4, PadChar='0', Justification='R', DbColumn="SequenceNumber" },
                        new NachaRecordField { FieldName="EntryDetailSequenceNumber", StartPosition=88, Length=7, PadChar='0', Justification='R', DbColumn="EntryDetailSequenceNumber" }
                    }
                },

                // ╔══════════════════════════════════════════════════════════╗
                // ║  Registro 8 - Control de lote                             ║
                // ╚══════════════════════════════════════════════════════════╝
                new NachaRecordLayout
                {
                    RecordType = "BATCH_CONTROL",
                    RecordCode = "8",
                    TotalLength = 106,
                    Description = "Registro control de lote NACHA-M",
                    Fields = new List<NachaRecordField>
                    {
                        new NachaRecordField { FieldName="ServiceClassCode", StartPosition=2, Length=3, PadChar='0', Justification='R', DbColumn="ServiceClassCode" },
                        new NachaRecordField { FieldName="EntryAddendaCount", StartPosition=5, Length=6, PadChar='0', Justification='R', DbColumn="EntryAddendaCount" },
                        new NachaRecordField { FieldName="EntryHash", StartPosition=11, Length=10, PadChar='0', Justification='R', DbColumn="EntryHash" },
                        new NachaRecordField { FieldName="TotalDebitAmount", StartPosition=21, Length=18, PadChar='0', Justification='R', DbColumn="TotalDebitAmount" },
                        new NachaRecordField { FieldName="TotalCreditAmount", StartPosition=39, Length=18, PadChar='0', Justification='R', DbColumn="TotalCreditAmount" },
                        new NachaRecordField { FieldName="CompanyIdentification", StartPosition=57, Length=10, PadChar=' ', Justification='L', DbColumn="CompanyIdentification" },
                        new NachaRecordField { FieldName="MessageAuthenticationCode", StartPosition=67, Length=19, PadChar=' ', Justification='L', DbColumn="MessageAuthenticationCode" },
                        new NachaRecordField { FieldName="OriginatingDFI", StartPosition=92, Length=8, PadChar='0', Justification='R', DbColumn="OriginatingDFI" },
                        new NachaRecordField { FieldName="BatchNumber", StartPosition=100, Length=7, PadChar='0', Justification='R', DbColumn="BatchNumber" }
                    }
                },

                // ╔══════════════════════════════════════════════════════════╗
                // ║  Registro 9 - Control de archivo                         ║
                // ╚══════════════════════════════════════════════════════════╝
                new NachaRecordLayout
                {
                    RecordType = "FILE_CONTROL",
                    RecordCode = "9",
                    TotalLength = 106,
                    Description = "Registro control de archivo NACHA-M (resumen general del archivo)",
                    Fields = new List<NachaRecordField>
                    {
                        new NachaRecordField { FieldName="BatchCount", StartPosition=2, Length=6, PadChar='0', Justification='R', DbColumn="BatchCount" },
                        new NachaRecordField { FieldName="BlockCount", StartPosition=8, Length=6, PadChar='0', Justification='R', DbColumn="BlockCount" },
                        new NachaRecordField { FieldName="EntryAddendaCount", StartPosition=14, Length=8, PadChar='0', Justification='R', DbColumn="EntryAddendaCount" },
                        new NachaRecordField { FieldName="EntryHash", StartPosition=22, Length=10, PadChar='0', Justification='R', DbColumn="EntryHash" },
                        new NachaRecordField { FieldName="TotalDebitAmount", StartPosition=32, Length=18, PadChar='0', Justification='R', DbColumn="TotalDebitAmount" },
                        new NachaRecordField { FieldName="TotalCreditAmount", StartPosition=50, Length=18, PadChar='0', Justification='R', DbColumn="TotalCreditAmount" }
                    }
                }
            };
        _context.NachaRecordLayouts.AddRange(layouts);
        await _context.SaveChangesAsync();

        await EnsureHeaderOriginAndDestinationLeftPaddingAsync();
    }

    private async Task EnsureHeaderOriginAndDestinationLeftPaddingAsync()
    {
        List<NachaRecordField> headerFields = await _context.NachaRecordFields
            .Where(field => field.NachaRecordLayoutId == 1
                && (field.FieldName == "ImmediateOrigin" || field.FieldName == "ImmediateDestination"))
            .ToListAsync();

        if (headerFields.Count == 0)
        {
            return;
        }

        foreach (NachaRecordField field in headerFields)
        {
            field.PadChar = '0';
            field.Justification = 'R';
        }

        await _context.SaveChangesAsync();
    }
}
