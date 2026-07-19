using Cfa.ACHInterbank.Application.ACH.Responses.Queries.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public class AchResponseQueryRepositoryTests
{
    [Fact]
    public async Task AchResponseRepository_SearchAsync_ShouldReturnPagedResults()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        await fixture.SeedResponsesAsync(3);
        var repo = new AchResponseRepository(fixture.Context);

        var result = await repo.SearchAsync(new AchResponseSearchQuery(null, null, null, null, null, null, null, null, null, null, 1, 2));

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
    }

    [Fact] public async Task AchResponseRepository_SearchAsync_ShouldFilterByTipoRespuesta(){await using var f=await SqliteFixture.CreateAsync();await f.SeedResponsesAsync(2);var repo=new AchResponseRepository(f.Context);var r=await repo.SearchAsync(new(null,null,"Prenota",null,null,null,null,null,null,null,1,10));r.Items.Should().OnlyContain(x=>x.TipoRespuesta=="Prenota");}
    [Fact] public async Task AchResponseRepository_SearchAsync_ShouldFilterByIdTransaccion(){await using var f=await SqliteFixture.CreateAsync();await f.SeedResponsesAsync(2);var repo=new AchResponseRepository(f.Context);var r=await repo.SearchAsync(new(null,null,null,"TX-2",null,null,null,null,null,null,1,10));r.Items.Should().ContainSingle();}
    [Fact] public async Task AchResponseRepository_SearchAsync_ShouldFilterByFechaRange(){await using var f=await SqliteFixture.CreateAsync();await f.SeedResponsesAsync(2);var repo=new AchResponseRepository(f.Context);var r=await repo.SearchAsync(new(DateTime.UtcNow.AddDays(-1),DateTime.UtcNow.AddDays(1),null,null,null,null,null,null,null,null,1,10));r.Items.Should().NotBeEmpty();}

    [Fact]
    public async Task AchResponseRepository_SearchAsync_ShouldClampInvalidPagination()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        await fixture.SeedResponsesAsync(1);
        var repo = new AchResponseRepository(fixture.Context);

        var result = await repo.SearchAsync(new AchResponseSearchQuery(null, null, null, null, null, null, null, null, null, null, 0, 0));

        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task AchResponseRepository_GetDashboardAsync_ShouldAggregateAndFilterInDatabase()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var date = new DateTime(2026, 1, 10, 12, 0, 0, DateTimeKind.Utc);
        fixture.Context.AchResponses.AddRange(
            CreateResponse("TX-1", TipoRespuestaAch.Transaccion, AchResponseProcessingStatus.Notificada, date),
            CreateResponse("TX-2", TipoRespuestaAch.Transaccion, AchResponseProcessingStatus.ErrorFuncional, date.AddDays(1)),
            CreateResponse("TX-3", TipoRespuestaAch.Prenota, AchResponseProcessingStatus.Duplicada, date.AddDays(2)));
        await fixture.Context.SaveChangesAsync();
        var repository = new AchResponseRepository(fixture.Context);

        var dashboard = await repository.GetDashboardAsync(new AchResponseDashboardQuery(
            date.AddDays(-1),
            date.AddDays(3),
            TipoRespuestaAch.Transaccion));

        dashboard.TotalRespuestas.Should().Be(2);
        dashboard.Notificadas.Should().Be(1);
        dashboard.ErroresFuncionales.Should().Be(1);
        dashboard.Duplicadas.Should().Be(0);
    }

    [Fact]
    public async Task AchResponseRepository_GetDashboardAsync_ShouldReturnZeroCountsWhenEmpty()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var repository = new AchResponseRepository(fixture.Context);

        var dashboard = await repository.GetDashboardAsync(new AchResponseDashboardQuery(null, null, null));

        dashboard.Should().Be(new AchResponseDashboardModel(0, 0, 0, 0, 0, 0, 0, 0, 0));
    }

    [Fact]
    public async Task AchResponseRepository_FindDetailByIdAsync_ShouldReturnDetailWithAttempts()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var response = await fixture.SeedResponseWithAttemptsAsync();
        var repo = new AchResponseRepository(fixture.Context);

        var detail = await repo.FindDetailByIdAsync(response.Id);

        detail.Should().NotBeNull();
        detail!.NotificationAttempts.Select(x => x.NumeroIntento).Should().BeInAscendingOrder();
    }

    [Fact] public async Task AchResponseRepository_FindDetailByIdAsync_ShouldReturnNull_WhenNotFound(){await using var f=await SqliteFixture.CreateAsync();var repo=new AchResponseRepository(f.Context);(await repo.FindDetailByIdAsync(Guid.NewGuid())).Should().BeNull();}

    [Fact]
    public async Task AchResponseNotificationAttemptRepository_FindPublicByResponseIdAsync_ShouldNotExposePayloads()
    {
        await using var fixture = await SqliteFixture.CreateAsync();
        var response = await fixture.SeedResponseWithAttemptsAsync();
        var repo = new AchResponseNotificationAttemptRepository(fixture.Context);

        var items = await repo.FindPublicByResponseIdAsync(response.Id);

        items.Should().NotBeEmpty();
        typeof(AchResponseNotificationAttemptModel).GetProperties().Select(p => p.Name).Should().NotContain(new[] { "RequestPayload", "ResponsePayload" });
    }

    [Fact] public async Task AchResponseStatusMappingRepository_ListAsync_ShouldReturnMappings(){await using var f=await SqliteFixture.CreateAsync();await f.SeedMappingsAsync();var repo=new AchResponseStatusMappingRepository(f.Context);(await repo.ListAsync()).Should().NotBeEmpty();}
    [Fact] public async Task AchResponseStatusMappingRepository_ListAsync_ShouldFilterByCamaraTipoAndActivo(){await using var f=await SqliteFixture.CreateAsync();await f.SeedMappingsAsync();var repo=new AchResponseStatusMappingRepository(f.Context);var list=await repo.ListAsync("ACH",TipoRespuestaAch.Transaccion,true);list.Should().OnlyContain(x=>x.CodigoCamaraCompensacion=="ACH"&&x.TipoRespuesta=="Transaccion"&&x.Activo);}    

    [Fact]
    public void QueryModels_ShouldNotExposeSoapOrProviderFields()
    {
        var types = new[] { typeof(AchResponseSearchQuery), typeof(AchResponseDashboardQuery), typeof(AchResponseDashboardModel), typeof(AchResponseListItemModel), typeof(AchResponseDetailModel), typeof(AchResponseNotificationAttemptModel), typeof(AchResponseStatusMappingListItemModel) };
        var forbidden = new[] { "Axon", "Soap", "Xml", "Wsdl", "Envelope", "SOAPAction", "idTransaccionAxon", "IdTransaccionAxon", "RequestPayload", "ResponsePayload" };
        foreach (var t in types)
            t.GetProperties().Select(x => x.Name).Should().NotContain(p => forbidden.Any(f => p.Contains(f, StringComparison.OrdinalIgnoreCase)));
    }

    private static AchResponse CreateResponse(
        string idTransaccion,
        TipoRespuestaAch tipoRespuesta,
        AchResponseProcessingStatus estado,
        DateTime fechaRecepcion)
        => new()
        {
            Id = Guid.NewGuid(),
            TipoRespuesta = tipoRespuesta,
            IdTransaccion = idTransaccion,
            CodigoCamaraCompensacion = "ACH",
            CodigoEstadoExterno = "E1",
            IdTransaccionServicioExterno = 1,
            HashIdempotencia = $"H-{idTransaccion}",
            EstadoProcesamiento = estado,
            PermiteNotificacion = true,
            FechaRecepcion = fechaRecepcion,
            FechaCreacion = fechaRecepcion
        };

    private sealed class SqliteFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AchDbContext Context { get; }
        private SqliteFixture(SqliteConnection connection, AchDbContext context){_connection=connection;Context=context;}
        public static async Task<SqliteFixture> CreateAsync(){var c=new SqliteConnection("DataSource=:memory:");await c.OpenAsync();var o=new DbContextOptionsBuilder<AchDbContext>().UseSqlite(c).Options;var ctx=new AchDbContext(o);await ctx.Database.EnsureCreatedAsync();return new(c,ctx);}        
        public async Task SeedResponsesAsync(int count){for(int i=1;i<=count;i++) Context.AchResponses.Add(new AchResponse{Id=Guid.NewGuid(),TipoRespuesta=i%2==0?TipoRespuestaAch.Transaccion:TipoRespuestaAch.Prenota,IdTransaccion=$"TX-{i}",CodigoCamaraCompensacion="ACH",CodigoEstadoExterno="E1",IdTransaccionServicioExterno=i,HashIdempotencia=$"H-{i}",EstadoProcesamiento=AchResponseProcessingStatus.Homologada,PermiteNotificacion=true,FechaRecepcion=DateTime.UtcNow.AddHours(-i),FechaCreacion=DateTime.UtcNow.AddHours(-i)});await Context.SaveChangesAsync();}
        public async Task<AchResponse> SeedResponseWithAttemptsAsync(){var r=new AchResponse{Id=Guid.NewGuid(),TipoRespuesta=TipoRespuestaAch.Transaccion,IdTransaccion="TX-1",CodigoCamaraCompensacion="ACH",CodigoEstadoExterno="E1",IdTransaccionServicioExterno=1,HashIdempotencia="HX",EstadoProcesamiento=AchResponseProcessingStatus.Homologada,PermiteNotificacion=true,FechaRecepcion=DateTime.UtcNow,FechaCreacion=DateTime.UtcNow};Context.AchResponses.Add(r);await Context.SaveChangesAsync();Context.AchResponseNotificationAttempts.AddRange(new AchResponseNotificationAttempt{AchResponseId=r.Id,NumeroIntento=2,EstadoNotificacion=AchResponseNotificationStatus.Pendiente,IdCanal=1,NombreCanal="C",IdTransaccion="TX-1",IdEstado=1,IdTransaccionServicioExterno=1,FechaCreacion=DateTime.UtcNow},new AchResponseNotificationAttempt{AchResponseId=r.Id,NumeroIntento=1,EstadoNotificacion=AchResponseNotificationStatus.Pendiente,IdCanal=1,NombreCanal="C",IdTransaccion="TX-1",IdEstado=1,IdTransaccionServicioExterno=1,FechaCreacion=DateTime.UtcNow});await Context.SaveChangesAsync();return r;}
        public async Task SeedMappingsAsync(){Context.AchResponseStatusMappings.AddRange(new AchResponseStatusMapping{CodigoCamaraCompensacion="ACH",TipoRespuesta=TipoRespuestaAch.Transaccion,CodigoEstadoExterno="E1",IdEstadoInterno=1,IdEstadoServicioExterno=1,EstadoInternoNombre="OK",Activo=true,PermiteNotificacion=true,FechaInicioVigencia=DateTime.UtcNow,FechaCreacion=DateTime.UtcNow},new AchResponseStatusMapping{CodigoCamaraCompensacion="CENIT",TipoRespuesta=TipoRespuestaAch.Prenota,CodigoEstadoExterno="E2",IdEstadoInterno=2,IdEstadoServicioExterno=2,EstadoInternoNombre="NO",Activo=false,PermiteNotificacion=false,FechaInicioVigencia=DateTime.UtcNow,FechaCreacion=DateTime.UtcNow});await Context.SaveChangesAsync();}
        public async ValueTask DisposeAsync(){await Context.DisposeAsync();await _connection.DisposeAsync();}
    }
}
