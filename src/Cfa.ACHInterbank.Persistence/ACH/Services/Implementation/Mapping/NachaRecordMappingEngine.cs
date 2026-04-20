using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;

[Scoped]
public sealed class NachaRecordMappingEngine : INachaRecordMappingEngine
{
    private readonly INachaFieldMappingEngine _fieldMappingEngine;

    public NachaRecordMappingEngine(INachaFieldMappingEngine fieldMappingEngine)
    {
        _fieldMappingEngine = fieldMappingEngine;
    }

    public async Task<RecordMappingResult> MapRecordAsync(RecordMappingRequest request, CancellationToken ct = default)
    {
        var result = new RecordMappingResult();
        foreach (var field in request.RecordPlan.Fields)
        {
            var fieldResult = await _fieldMappingEngine.MapFieldAsync(new FieldMappingRequest
            {
                RecordCode = request.RecordCode,
                FieldPlan = field,
                SourceRecord = request.SourceRecord,
                ContextValues = request.ContextValues
            }, ct);

            result.FieldTraces.Add(fieldResult.Trace);
            result.ValuesByFieldCode[field.FieldCode] = fieldResult.FinalValue;
            if (!fieldResult.Success)
            {
                result.Warnings.Add($"Field {field.FieldCode} no mapeado completamente.");
            }
        }

        result.Success = result.Warnings.Count == 0;
        return result;
    }
}
