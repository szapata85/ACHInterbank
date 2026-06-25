using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationMappingTraceWriterTests
{
    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldPersistFieldByFieldTrace()
    {
        await using var context = await BuildContextAsync();
        var catalog = new IntegrationCatalogService(context);
        await catalog.GetMethodsAsync();
        await PublishRegistrarRespuestaMappingsAsync(context);

        var writer = new IntegrationMappingTraceWriter(context, catalog);
        var operation = new TransactionIntegrationOperationResult(
            null,
            "TX-TRACE-001",
            IntegrationGuaranteeConstants.WsAxon,
            IntegrationGuaranteeConstants.RegistrarRespuestaTransaccion,
            IntegrationGuaranteeConstants.DifferentialResponseNotification,
            IntegrationGuaranteeConstants.InboundResponse,
            "Respuesta diferencial / notificacion",
            "Entidad externa",
            false,
            "No monetaria.",
            true,
            []);
        var command = new RegistrarRespuestaAchCommand(
            TipoRespuestaAch.Transaccion,
            "TX-TRACE-001",
            1,
            "CANAL-UAT",
            2,
            "00",
            2001,
            "Aprobada",
            "ACH",
            "BANCO-UAT",
            "CFA",
            "corr-trace");

        var result = await writer.WriteAsync(operation, command, null, "TX-TRACE-001", "corr-trace", dryRun: true, externalTransmission: false);

        Assert.Empty(result.MissingRequiredFields);
        Assert.True(result.EntryCount >= 3);

        var trace = await context.IntegrationMappingTraces
            .Include(x => x.Entries)
            .SingleAsync(x => x.Id == result.TraceId);
        Assert.Equal(IntegrationGuaranteeConstants.RegistrarRespuestaTransaccion, trace.OperationKey);
        Assert.Equal(IntegrationGuaranteeConstants.DifferentialResponseNotification, trace.MappingPurpose);
        Assert.False(trace.MonetaryMovementCreated);
        Assert.False(trace.ExternalTransmission);
        Assert.Contains(trace.Entries, x => x.TargetField == "idTransaccion" && x.MappedValueSanitized == "TX-TRACE-001");
        Assert.Contains(trace.Entries, x => x.TargetField == "causal" && x.MappedValueSanitized == "00");
        Assert.DoesNotContain(trace.Entries, x => x.TargetField.StartsWith("ANS", StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<AchDbContext> BuildContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task PublishRegistrarRespuestaMappingsAsync(AchDbContext context)
    {
        var method = await context.IntegrationMethods.SingleAsync(x => x.Code == "WSAXON.RegistrarRespuestaTransaccion");
        var parameters = await context.IntegrationMethodParameters
            .Where(x => x.MethodId == method.Id && x.IsActive && x.Direction == IntegrationParameterDirectionEnum.Input)
            .ToListAsync();

        var set = new IntegrationMappingSet
        {
            Id = Guid.NewGuid(),
            MethodId = method.Id,
            Name = "RegistrarRespuestaTraceTest",
            Version = 1,
            Status = IntegrationMappingSetStatusEnum.Published,
            IsActive = true,
            PublishedAtUtc = DateTime.UtcNow,
            PublishedBy = "test"
        };
        context.IntegrationMappingSets.Add(set);

        foreach (var parameter in parameters)
        {
            context.IntegrationMappingRules.Add(new IntegrationMappingRule
            {
                MappingSetId = set.Id,
                MethodId = method.Id,
                ParameterId = parameter.Id,
                SourceKind = IntegrationSourceKindEnum.Transaction,
                SourceFieldPath = parameter.ParameterPath switch
                {
                    "idCanal" => nameof(RegistrarRespuestaAchCommand.IdCanal),
                    "nombreCanal" => nameof(RegistrarRespuestaAchCommand.NombreCanal),
                    "idTransaccion" => nameof(RegistrarRespuestaAchCommand.IdTransaccion),
                    "idEstado" => nameof(RegistrarRespuestaAchCommand.IdEstado),
                    "causal" => nameof(RegistrarRespuestaAchCommand.Causal),
                    "idTransaccionAxon" => nameof(RegistrarRespuestaAchCommand.IdTransaccionServicioExterno),
                    "descripcionCausal" => nameof(RegistrarRespuestaAchCommand.DescripcionCausal),
                    _ => nameof(RegistrarRespuestaAchCommand.IdTransaccion)
                },
                Priority = 1,
                Enabled = true
            });
        }

        await context.SaveChangesAsync();
    }
}
