using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;


public class NachaRecordFieldValidator : INachaRecordFieldValidator
{
    public NachaRecordValidationResult Validate(NachaRecordValidationContext context)
    {
        var issues = new List<NachaRecordValidationIssue>();
        var records = SplitRecords(context.NachaContent, context.Config.Record1.RecordSize);
        if (!records.Any()) issues.Add(new("NACHA_EMPTY", NachaRecordValidationSeverity.Error, "NACHA vacío"));
        if (records.Any(r => r.Length != context.Config.Record1.RecordSize)) issues.Add(new("RECORD_LENGTH_INVALID", NachaRecordValidationSeverity.Error, "Longitud inválida"));
        var r1 = records.FirstOrDefault(r => r.StartsWith('1'));
        var r5 = records.FirstOrDefault(r => r.StartsWith('5'));
        var r8 = records.FirstOrDefault(r => r.StartsWith('8'));
        var r9 = records.FirstOrDefault(r => r.StartsWith('9') && r.Any(c => c!='9'));
        if (r1 is null || r5 is null || r8 is null || r9 is null) issues.Add(new("RECORD_MISSING", NachaRecordValidationSeverity.Error, "Faltan records 1/5/8/9"));
        if (r1 is not null)
        {
            var token = $"{context.Config.Record1.FileIdModifier}094{context.Config.Record1.BlockingFactor:00}{context.Config.Record1.FormatCode}";
            if (!r1.Contains(token)) issues.Add(new("TYPE1_CONTROL_INVALID", NachaRecordValidationSeverity.Error, "Control Type1 inválido"));
            if (r1.Contains("A094106")) issues.Add(new("TYPE1_A094106_INVALID", NachaRecordValidationSeverity.Error, "A094106 inválido"));
        }
        var type6 = records.Where(r => r.StartsWith('6')).ToList();
        var type7 = records.Where(r => r.StartsWith('7')).ToList();
        if (r8 is not null)
        {
            var declared = int.Parse(r8.Substring(4,6));
            if (declared != type6.Count + type7.Count) issues.Add(new("ENTRY_ADDENDA_MISMATCH", NachaRecordValidationSeverity.Error, "Conteo 6+7 inconsistente"));
            var hash = type6.Sum(r => long.Parse(r.Substring(3,8))) % 10_000_000_000L;
            var declaredHash = long.Parse(r8.Substring(10,10));
            if (hash != declaredHash) issues.Add(new("ENTRY_HASH_MISMATCH", NachaRecordValidationSeverity.Error, "Hash inconsistente"));
        }
        if (context.ClearingHouseCode?.Equals("CENIT", StringComparison.OrdinalIgnoreCase) == true)
            issues.Add(new("CENIT_CURRENTLAYOUT_CHARACTERIZATION", NachaRecordValidationSeverity.Warning, "CurrentLayout caracterizado pendiente normativa"));
        if (context.IsCurrentLayoutMode)
            issues.Add(new("CURRENTLAYOUT_PROVISIONAL", NachaRecordValidationSeverity.Warning, "CurrentLayout/UAT provisional"));
        issues.Add(new("CURRENTLAYOUT_INFO", NachaRecordValidationSeverity.Info, $"Rail={context.ClearingHouseCode};Flow={context.Flow};Approved={context.Config.IsProductiveApproved}"));
        return new NachaRecordValidationResult(!issues.Any(x => x.Severity == NachaRecordValidationSeverity.Error), issues);
    }

    private static List<string> SplitRecords(string content, int len)
    {
        var n=(content??"").Replace("\r","");
        var lines=n.Split('\n', StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).ToList();
        if (lines.Count>1) return lines;
        if (n.Length < len) return [];
        return Enumerable.Range(0, n.Length/len).Select(i=>n.Substring(i*len,len)).ToList();
    }
}
