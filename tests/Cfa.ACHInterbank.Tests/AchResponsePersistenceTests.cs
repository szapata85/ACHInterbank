using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchResponsePersistenceTests
{

    [Fact]
    public async Task AchResponseNotificationAttempt_ResponseIdAndNumeroIntento_ShouldBeUnique()
    {
        await using var context = await BuildContextAsync();
        var entityType = context.Model.FindEntityType(typeof(AchResponseNotificationAttempt));
        var index = entityType!.GetIndexes().Single(i => i.Properties.Select(p => p.Name).SequenceEqual(new[] { "AchResponseId", "NumeroIntento" }));

        Assert.True(index.IsUnique);
    }

    [Fact]
    public async Task AchResponseNotificationAttempt_PayloadColumns_ShouldBeConfiguredAsLongTextWithoutMaxLength()
    {
        await using var context = await BuildContextAsync();
        var entityType = context.Model.FindEntityType(typeof(AchResponseNotificationAttempt));

        var requestPayload = entityType!.FindProperty(nameof(AchResponseNotificationAttempt.RequestPayload));
        var responsePayload = entityType.FindProperty(nameof(AchResponseNotificationAttempt.ResponsePayload));

        Assert.NotNull(requestPayload);
        Assert.NotNull(responsePayload);
        Assert.Null(requestPayload!.GetMaxLength());
        Assert.Null(responsePayload!.GetMaxLength());
    }

    [Fact]
    public async Task AchResponse_AuditDates_ShouldBeRequired()
    {
        await using var context = await BuildContextAsync();
        var entityType = context.Model.FindEntityType(typeof(AchResponse));

        Assert.False(entityType!.FindProperty(nameof(AchResponse.FechaRecepcion))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(AchResponse.FechaCreacion))!.IsNullable);
    }

    [Fact]
    public async Task AchResponseNotificationAttempt_AuditDates_ShouldBeRequired()
    {
        await using var context = await BuildContextAsync();
        var entityType = context.Model.FindEntityType(typeof(AchResponseNotificationAttempt));

        Assert.False(entityType!.FindProperty(nameof(AchResponseNotificationAttempt.FechaCreacion))!.IsNullable);
    }

    [Fact]
    public async Task AchResponseNotificationAttempt_CriticalFields_ShouldBeRequired()
    {
        await using var context = await BuildContextAsync();
        var entityType = context.Model.FindEntityType(typeof(AchResponseNotificationAttempt));

        Assert.False(entityType!.FindProperty(nameof(AchResponseNotificationAttempt.IdCanal))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(AchResponseNotificationAttempt.NombreCanal))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(AchResponseNotificationAttempt.IdTransaccion))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(AchResponseNotificationAttempt.IdEstado))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(AchResponseNotificationAttempt.IdTransaccionServicioExterno))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(AchResponseNotificationAttempt.EstadoNotificacion))!.IsNullable);
    }

    [Fact]
    public async Task AchResponseRepository_AddAsync_ShouldPersistResponse()
    {
        await using var context = await BuildContextAsync();
        var repo = new AchResponseRepository(context);
        var response = BuildResponse();

        await repo.AddAsync(response);
        await context.SaveChangesAsync();

        Assert.True(await context.AchResponses.AnyAsync(x => x.Id == response.Id));
    }

    [Fact]
    public async Task AchResponseRepository_FindByIdempotencyHashAsync_ShouldReturnResponse()
    {
        await using var context = await BuildContextAsync();
        var response = BuildResponse();
        context.AchResponses.Add(response);
        await context.SaveChangesAsync();

        var repo = new AchResponseRepository(context);
        var found = await repo.FindByIdempotencyHashAsync(response.HashIdempotencia);

        Assert.NotNull(found);
        Assert.Equal(response.Id, found!.Id);
        Assert.Equal(response.IdTransaccion, found.IdTransaccion);
    }

    [Fact]
    public async Task AchResponseRepository_FindByIdempotencyHashAsync_ShouldReturnNull_WhenNotFound()
    {
        await using var context = await BuildContextAsync();
        var repo = new AchResponseRepository(context);

        var found = await repo.FindByIdempotencyHashAsync("missing");

        Assert.Null(found);
    }

    [Fact]
    public async Task AchResponse_HashIdempotencia_ShouldBeUnique()
    {
        await using var context = await BuildContextAsync();
        context.AchResponses.Add(BuildResponse(hash: "same"));
        context.AchResponses.Add(BuildResponse(hash: "same"));

        await Assert.ThrowsAnyAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
    }

    [Fact]
    public async Task AchResponseNotificationAttemptRepository_AddAsync_ShouldPersistAttempt()
    {
        await using var context = await BuildContextAsync();
        var response = BuildResponse();
        context.AchResponses.Add(response);
        await context.SaveChangesAsync();

        var repo = new AchResponseNotificationAttemptRepository(context);
        var attempt = BuildAttempt(response.Id, 1);
        await repo.AddAsync(attempt);
        await context.SaveChangesAsync();

        Assert.True(await context.AchResponseNotificationAttempts.AnyAsync(x => x.Id == attempt.Id));
    }

    [Fact]
    public async Task AchResponseNotificationAttemptRepository_GetNextAttemptNumberAsync_ShouldReturnOne_WhenNoAttempts()
    {
        await using var context = await BuildContextAsync();
        var response = BuildResponse();
        context.AchResponses.Add(response);
        await context.SaveChangesAsync();

        var repo = new AchResponseNotificationAttemptRepository(context);
        var next = await repo.GetNextAttemptNumberAsync(response.Id);

        Assert.Equal(1, next);
    }

    [Fact]
    public async Task AchResponseNotificationAttemptRepository_GetNextAttemptNumberAsync_ShouldReturnMaxPlusOne()
    {
        await using var context = await BuildContextAsync();
        var response = BuildResponse();
        context.AchResponses.Add(response);
        context.AchResponseNotificationAttempts.Add(BuildAttempt(response.Id, 1));
        context.AchResponseNotificationAttempts.Add(BuildAttempt(response.Id, 2));
        await context.SaveChangesAsync();

        var repo = new AchResponseNotificationAttemptRepository(context);
        var next = await repo.GetNextAttemptNumberAsync(response.Id);

        Assert.Equal(3, next);
    }

    [Fact]
    public async Task AchResponseNotificationAttemptRepository_FindByResponseIdAsync_ShouldReturnOrderedAttempts()
    {
        await using var context = await BuildContextAsync();
        var response = BuildResponse();
        context.AchResponses.Add(response);
        context.AchResponseNotificationAttempts.Add(BuildAttempt(response.Id, 3));
        context.AchResponseNotificationAttempts.Add(BuildAttempt(response.Id, 1));
        context.AchResponseNotificationAttempts.Add(BuildAttempt(response.Id, 2));
        await context.SaveChangesAsync();

        var repo = new AchResponseNotificationAttemptRepository(context);
        var attempts = await repo.FindByResponseIdAsync(response.Id);

        Assert.Equal(new[] { 1, 2, 3 }, attempts.Select(x => x.NumeroIntento).ToArray());
    }

    [Fact]
    public async Task AchResponse_EfConfiguration_ShouldHaveExpectedIndexes()
    {
        await using var context = await BuildContextAsync();
        var entityType = context.Model.FindEntityType(typeof(AchResponse));
        Assert.NotNull(entityType);

        var indexes = entityType!.GetIndexes().Select(i => string.Join(",", i.Properties.Select(p => p.Name))).ToList();
        Assert.Contains("HashIdempotencia", indexes);
        Assert.Contains("IdTransaccion", indexes);
        Assert.Contains("TipoRespuesta,CodigoCamaraCompensacion,CodigoEstadoExterno", indexes);
        Assert.Contains("EstadoProcesamiento", indexes);
        Assert.Contains("CorrelationId", indexes);
    }

    [Fact]
    public async Task AchResponseNotificationAttempt_EfConfiguration_ShouldHaveExpectedIndexes()
    {
        await using var context = await BuildContextAsync();
        var entityType = context.Model.FindEntityType(typeof(AchResponseNotificationAttempt));
        Assert.NotNull(entityType);

        var indexes = entityType!.GetIndexes().Select(i => string.Join(",", i.Properties.Select(p => p.Name))).ToList();
        Assert.Contains("AchResponseId,NumeroIntento", indexes);
        Assert.Contains("EstadoNotificacion", indexes);
        Assert.Contains("FechaCreacion", indexes);
    }

    [Fact]
    public async Task AchResponse_ShouldCascadeOrRestrictAttempts_AsConfigured()
    {
        await using var context = await BuildContextAsync();
        var entityType = context.Model.FindEntityType(typeof(AchResponseNotificationAttempt));
        var fk = entityType!.GetForeignKeys().Single(x => x.PrincipalEntityType.ClrType == typeof(AchResponse));

        Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior);
    }

    private static async Task<AchDbContext> BuildContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static AchResponse BuildResponse(string? hash = null)
    {
        var id = Guid.NewGuid();
        return new AchResponse
        {
            Id = id,
            TipoRespuesta = TipoRespuestaAch.Transaccion,
            IdTransaccion = "TX-1",
            CodigoCamaraCompensacion = "ACH",
            CodigoEntidadOrigen = "001",
            CodigoEntidadDestino = "002",
            CodigoEstadoExterno = "00",
            CodigoCausalExterna = "R01",
            IdEstadoInterno = 2,
            IdEstadoServicioExterno = 200,
            EstadoInternoNombre = "Aplicada",
            CausalNormalizada = "R01",
            DescripcionCausal = "Cuenta cerrada",
            IdTransaccionServicioExterno = 999,
            HashIdempotencia = hash ?? Guid.NewGuid().ToString("N"),
            EstadoProcesamiento = AchResponseProcessingStatus.Recibida,
            MotivoNoHomologacion = null,
            PermiteNotificacion = true,
            CorrelationId = "corr-1",
            FechaRecepcion = DateTime.UtcNow,
            FechaCreacion = DateTime.UtcNow
        };
    }

    private static AchResponseNotificationAttempt BuildAttempt(Guid responseId, int number)
        => new()
        {
            Id = number,
            AchResponseId = responseId,
            NumeroIntento = number,
            EstadoNotificacion = AchResponseNotificationStatus.Pendiente,
            IdCanal = 1,
            NombreCanal = "ACH",
            IdTransaccion = "TX-1",
            IdEstado = 2,
            Causal = "R01",
            IdTransaccionServicioExterno = 999,
            DescripcionCausal = "Cuenta cerrada",
            FechaCreacion = DateTime.UtcNow
        };
}
