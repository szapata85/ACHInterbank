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
            return;

        var layouts = new List<NachaRecordLayout>
        {
            // ===== HEADER 106 =====
            new NachaRecordLayout
            {
                RecordType = "HEADER",
                RecordCode = "1",
                TotalLength = 106,
                Description = "Encabezado de archivo NACHA-M (106 caracteres)",
                Fields = new List<NachaRecordField>
                {
                    new NachaRecordField { FieldName="PriorityCode",    StartPosition=2,  Length=2,  PadChar='0', Justification='R', DbColumn="PriorityCode" },
                    new NachaRecordField { FieldName="ImmediateDest",   StartPosition=4,  Length=10, PadChar='0', Justification='R', DbColumn="ImmediateDestination" },
                    new NachaRecordField { FieldName="ImmediateOrigin", StartPosition=14, Length=10, PadChar='0', Justification='R', DbColumn="ImmediateOrigin" },
                    new NachaRecordField { FieldName="CreationDate",    StartPosition=24, Length=8,  PadChar='0', Justification='R', DbColumn="FileCreationDate", Format="yyyyMMdd" },
                    new NachaRecordField { FieldName="CreationTime",    StartPosition=32, Length=4,  PadChar='0', Justification='R', DbColumn="FileCreationTime", Format="HHmm" },
                    // Ajusta las posiciones de los campos siguientes para llegar a 106
                    new NachaRecordField { FieldName="ReferenceCode",   StartPosition=99, Length=8,  PadChar=' ', Justification='L', DbColumn="ReferenceCode" }
                }
            },

            // ===== ENTRY DETAIL 106 =====
            new NachaRecordLayout
            {
                RecordType = "ENTRY_DETAIL",
                RecordCode = "6",
                TotalLength = 106,
                Description = "Detalle de transacción NACHA-M (106 caracteres)",
                Fields = new List<NachaRecordField>
                {
                    new NachaRecordField { FieldName="TransactionCode", StartPosition=2,  Length=2,  PadChar='0', Justification='R', DbColumn="TransactionCode" },
                    new NachaRecordField { FieldName="RoutingNumber",   StartPosition=4,  Length=9,  PadChar='0', Justification='R', DbColumn="RoutingNumber" },
                    new NachaRecordField { FieldName="AccountNumber",   StartPosition=13, Length=17, PadChar=' ', Justification='L', DbColumn="DestinationAccountNumber" },
                    new NachaRecordField { FieldName="Amount",          StartPosition=30, Length=12, PadChar='0', Justification='R', DbColumn="Amount", Format="000000000000" },
                    new NachaRecordField { FieldName="Reference",       StartPosition=42, Length=20, PadChar=' ', Justification='L', DbColumn="Reference" }
                    // completa hasta 106 con fillers u otros campos del estándar local
                }
            },

            // ===== FILE CONTROL 106 =====
            new NachaRecordLayout
            {
                RecordType = "FILE_CONTROL",
                RecordCode = "9",
                TotalLength = 106,
                Description = "Control final NACHA-M (106 caracteres)",
                Fields = new List<NachaRecordField>
                {
                    new NachaRecordField { FieldName="BatchCount",     StartPosition=2,  Length=6,  PadChar='0', Justification='R', DbColumn="BatchCount" },
                    new NachaRecordField { FieldName="BlockCount",     StartPosition=8,  Length=6,  PadChar='0', Justification='R', DbColumn="BlockCount" },
                    new NachaRecordField { FieldName="EntryAddenda",   StartPosition=14, Length=8,  PadChar='0', Justification='R', DbColumn="EntryAddendaCount" },
                    new NachaRecordField { FieldName="TotalDebit",     StartPosition=22, Length=12, PadChar='0', Justification='R', DbColumn="TotalDebitAmount", Format="000000000000" },
                    new NachaRecordField { FieldName="TotalCredit",    StartPosition=34, Length=12, PadChar='0', Justification='R', DbColumn="TotalCreditAmount", Format="000000000000" }
                    // completa hasta 106 si el estándar local exige más campos
                }
            }
        };

        _context.NachaRecordLayouts.AddRange(layouts);
        await _context.SaveChangesAsync();
    }
}

