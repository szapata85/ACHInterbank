using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchResponseStatusMappingRepositoryTests
{
    [Fact]
    public async Task FindCandidatesAsync_ShouldReturnMappings_ByCamaraTipoAndEstado()
    {
        await using var context = await BuildContextAsync();
        context.AchResponseStatusMappings.AddRange(
            BuildEntity(1, "ACH", TipoRespuestaAch.Transaccion, "00"),
            BuildEntity(2, "ACH", TipoRespuestaAch.Prenota, "00"),
            BuildEntity(3, "CENIT", TipoRespuestaAch.Transaccion, "00"),
            BuildEntity(4, "ACH", TipoRespuestaAch.Transaccion, "99"));
        await context.SaveChangesAsync();

        var repo = new AchResponseStatusMappingRepository(context);
        var result = await repo.FindCandidatesAsync("ACH", TipoRespuestaAch.Transaccion, "00");

        Assert.Single(result);
        Assert.Equal(1, result[0].Id);
    }

    [Fact]
    public async Task FindCandidatesAsync_ShouldNormalizeCamaraAndEstado()
    {
        await using var context = await BuildContextAsync();
        context.AchResponseStatusMappings.Add(BuildEntity(1, "ACH", TipoRespuestaAch.Transaccion, "00"));
        await context.SaveChangesAsync();

        var repo = new AchResponseStatusMappingRepository(context);
        var result = await repo.FindCandidatesAsync(" ach ", TipoRespuestaAch.Transaccion, " 00 ");

        Assert.Single(result);
    }

    [Fact]
    public async Task FindCandidatesAsync_ShouldReturnInactiveMappingsToo_AsCandidates()
    {
        await using var context = await BuildContextAsync();
        context.AchResponseStatusMappings.Add(BuildEntity(1, "ACH", TipoRespuestaAch.Transaccion, "00", activo: false));
        await context.SaveChangesAsync();

        var repo = new AchResponseStatusMappingRepository(context);
        var result = await repo.FindCandidatesAsync("ACH", TipoRespuestaAch.Transaccion, "00");

        Assert.Single(result);
        Assert.False(result[0].Activo);
    }

    [Fact]
    public async Task FindCandidatesAsync_ShouldMapAllFieldsToApplicationModel()
    {
        await using var context = await BuildContextAsync();
        var entity = BuildEntity(55, "ACH", TipoRespuestaAch.Transaccion, "00");
        entity.CodigoCausalExterna = "R01";
        entity.CausalNormalizada = "R01";
        entity.DescripcionCausalNormalizada = "Cuenta cerrada";
        entity.FechaFinVigencia = DateTime.UtcNow.AddDays(10);
        context.AchResponseStatusMappings.Add(entity);
        await context.SaveChangesAsync();

        var repo = new AchResponseStatusMappingRepository(context);
        var result = await repo.FindCandidatesAsync("ACH", TipoRespuestaAch.Transaccion, "00");

        var mapped = Assert.Single(result);
        Assert.Equal(entity.Id, mapped.Id);
        Assert.Equal(entity.CodigoCamaraCompensacion, mapped.CodigoCamaraCompensacion);
        Assert.Equal(entity.TipoRespuesta, mapped.TipoRespuesta);
        Assert.Equal(entity.CodigoEstadoExterno, mapped.CodigoEstadoExterno);
        Assert.Equal(entity.CodigoCausalExterna, mapped.CodigoCausalExterna);
        Assert.Equal(entity.IdEstadoInterno, mapped.IdEstadoInterno);
        Assert.Equal(entity.IdEstadoServicioExterno, mapped.IdEstadoServicioExterno);
        Assert.Equal(entity.EstadoInternoNombre, mapped.EstadoInternoNombre);
        Assert.Equal(entity.CausalNormalizada, mapped.CausalNormalizada);
        Assert.Equal(entity.DescripcionCausalNormalizada, mapped.DescripcionCausalNormalizada);
        Assert.Equal(entity.RequiereCausal, mapped.RequiereCausal);
        Assert.Equal(entity.PermiteNotificacion, mapped.PermiteNotificacion);
        Assert.Equal(entity.Activo, mapped.Activo);
        Assert.Equal(entity.FechaInicioVigencia, mapped.FechaInicioVigencia);
        Assert.Equal(entity.FechaFinVigencia, mapped.FechaFinVigencia);
    }

    [Fact]
    public async Task AchResponseStatusMapping_EfConfiguration_ShouldRequireMandatoryFields()
    {
        await using var context = await BuildContextAsync();
        var entityType = context.Model.FindEntityType(typeof(AchResponseStatusMapping));
        Assert.NotNull(entityType);

        Assert.False(entityType!.FindProperty(nameof(AchResponseStatusMapping.CodigoCamaraCompensacion))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(AchResponseStatusMapping.CodigoEstadoExterno))!.IsNullable);
        Assert.False(entityType.FindProperty(nameof(AchResponseStatusMapping.EstadoInternoNombre))!.IsNullable);
    }

    [Fact]
    public async Task AchResponseStatusMapping_EfConfiguration_ShouldHaveExpectedIndexes()
    {
        await using var context = await BuildContextAsync();
        var entityType = context.Model.FindEntityType(typeof(AchResponseStatusMapping));
        Assert.NotNull(entityType);

        var indexColumns = entityType!.GetIndexes()
            .Select(i => string.Join(",", i.Properties.Select(p => p.Name)))
            .ToList();

        Assert.Contains("CodigoCamaraCompensacion,TipoRespuesta,CodigoEstadoExterno,Activo", indexColumns);
        Assert.Contains("CodigoCamaraCompensacion,TipoRespuesta,CodigoEstadoExterno,CodigoCausalExterna,Activo", indexColumns);
        Assert.Contains("FechaInicioVigencia,FechaFinVigencia", indexColumns);
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

    private static AchResponseStatusMapping BuildEntity(int id, string camara, TipoRespuestaAch tipo, string estado, bool activo = true)
        => new()
        {
            Id = id,
            CodigoCamaraCompensacion = camara,
            TipoRespuesta = tipo,
            CodigoEstadoExterno = estado,
            CodigoCausalExterna = null,
            IdEstadoInterno = id,
            IdEstadoServicioExterno = 100 + id,
            EstadoInternoNombre = $"EST-{id}",
            CausalNormalizada = null,
            DescripcionCausalNormalizada = null,
            RequiereCausal = false,
            PermiteNotificacion = true,
            Activo = activo,
            FechaInicioVigencia = DateTime.UtcNow.AddDays(-5),
            FechaFinVigencia = null,
            FechaCreacion = DateTime.UtcNow
        };
}
