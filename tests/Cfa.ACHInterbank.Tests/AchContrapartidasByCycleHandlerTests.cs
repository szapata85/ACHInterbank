using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Tests.TestSupport;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchContrapartidasByCycleHandlerTests
{
    private static readonly DateTime BogotaProcessingDate = new(2026, 8, 3);
    private static readonly DateTimeOffset FixedUtcInstant = new(2026, 8, 4, 1, 15, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_DebeRetornarMensajeSinCiclos_CuandoNoHayVentanasActivas()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var dispatchJob = new Mock<IContrapartidaDispatchJobService>(MockBehavior.Strict);

        var sut = new AchContrapartidasByCycleHandler(
            context,
            dispatchJob.Object,
            NullLogger<AchContrapartidasByCycleHandler>.Instance,
            CreateFixedClock());

        var resultado = await sut.ExecuteAsync(CrearTask(), CancellationToken.None);

        Assert.Contains("Sin ciclos activos", resultado);
        dispatchJob.Verify(x => x.ProcessCycleAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_DebeProcesarCicloActivo_UsandoChunkSizeConfigurado()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await SembrarCicloActivoAsync(context, "cycle-01", 1, BogotaProcessingDate, new TimeSpan(22, 0, 0));

        var dispatchJob = new Mock<IContrapartidaDispatchJobService>();
        dispatchJob
            .Setup(x => x.ProcessCycleAsync("cycle-01", 1, "task:AchContrapartidasByCycle", 150, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContrapartidaCycleDispatchResult("cycle-01", 1, 5, 4, 1, 0, 1, "ciclo-01 resumen"));

        var sut = new AchContrapartidasByCycleHandler(
            context,
            dispatchJob.Object,
            NullLogger<AchContrapartidasByCycleHandler>.Instance,
            CreateFixedClock());

        var task = CrearTask(new Dictionary<string, string>
        {
            ["ChunkSize"] = "150",
            ["MaxCyclesPerRun"] = "10"
        });

        var resultado = await sut.ExecuteAsync(task, CancellationToken.None);

        Assert.Contains("Proc_Contrapartidas ejecutado", resultado);
        Assert.Contains("Ciclos=1", resultado);
        Assert.Contains("Processed=5", resultado);
        Assert.Contains("Success=4", resultado);
        Assert.Contains("Failed=1", resultado);
        Assert.Contains("ciclo-01 resumen", resultado);
        dispatchJob.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_DebeContinuarConSiguienteCiclo_CuandoUnCicloFalla()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await SembrarCicloActivoAsync(context, "cycle-01", 1, BogotaProcessingDate, new TimeSpan(21, 0, 0));
        await SembrarCicloActivoAsync(context, "cycle-02", 2, BogotaProcessingDate, new TimeSpan(22, 0, 0));

        var dispatchJob = new Mock<IContrapartidaDispatchJobService>();
        dispatchJob
            .Setup(x => x.ProcessCycleAsync("cycle-01", 1, "task:AchContrapartidasByCycle", 300, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("fallo controlado"));
        dispatchJob
            .Setup(x => x.ProcessCycleAsync("cycle-02", 2, "task:AchContrapartidasByCycle", 300, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContrapartidaCycleDispatchResult("cycle-02", 2, 3, 3, 0, 0, 1, "ciclo-02 ok"));

        var sut = new AchContrapartidasByCycleHandler(
            context,
            dispatchJob.Object,
            NullLogger<AchContrapartidasByCycleHandler>.Instance,
            CreateFixedClock());

        var task = CrearTask(new Dictionary<string, string>
        {
            ["ChunkSize"] = "no-numero"
        });

        var resultado = await sut.ExecuteAsync(task, CancellationToken.None);

        Assert.Contains("Ciclos=2", resultado);
        Assert.Contains("Processed=3", resultado);
        Assert.Contains("Success=3", resultado);
        Assert.Contains("Failed=0", resultado);
        Assert.Contains("ERROR=fallo controlado", resultado);
        Assert.Contains("ciclo-02 ok", resultado);
        dispatchJob.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_DebeRespetarMaxCyclesPerRun_CuandoHayMasCiclosActivos()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        await SembrarCicloActivoAsync(context, "cycle-01", 1, BogotaProcessingDate, new TimeSpan(20, 0, 0));
        await SembrarCicloActivoAsync(context, "cycle-02", 2, BogotaProcessingDate, new TimeSpan(21, 0, 0));
        await SembrarCicloActivoAsync(context, "cycle-03", 3, BogotaProcessingDate, new TimeSpan(22, 0, 0));

        var dispatchJob = new Mock<IContrapartidaDispatchJobService>();
        dispatchJob
            .Setup(x => x.ProcessCycleAsync(It.IsAny<string>(), It.IsAny<int>(), "task:AchContrapartidasByCycle", 80, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string cycleId, int chId, string _, int __, CancellationToken ___)
                => new ContrapartidaCycleDispatchResult(cycleId, chId, 1, 1, 0, 0, 1, $"{cycleId}-ok"));

        var sut = new AchContrapartidasByCycleHandler(
            context,
            dispatchJob.Object,
            NullLogger<AchContrapartidasByCycleHandler>.Instance,
            CreateFixedClock());

        var task = CrearTask(new Dictionary<string, string>
        {
            ["ChunkSize"] = "80",
            ["MaxCyclesPerRun"] = "2"
        });

        var resultado = await sut.ExecuteAsync(task, CancellationToken.None);

        Assert.Contains("Ciclos=2", resultado);
        dispatchJob.Verify(x => x.ProcessCycleAsync(It.IsAny<string>(), It.IsAny<int>(), "task:AchContrapartidasByCycle", 80, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    private static TaskDefinition CrearTask(Dictionary<string, string>? parametros = null)
    {
        var task = new TaskDefinition
        {
            Id = 1,
            Code = "AchContrapartidasByCycle",
            Name = "Despacho contrapartidas",
            Parameters = new List<TaskParameter>()
        };

        if (parametros is null)
        {
            return task;
        }

        foreach (var item in parametros)
        {
            task.Parameters.Add(new TaskParameter { Key = item.Key, Value = item.Value });
        }

        return task;
    }

    private static async Task SembrarCicloActivoAsync(
        AchDbContext context,
        string cycleId,
        int clearingHouseId,
        DateTime processingDate,
        TimeSpan cutoff)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = clearingHouseId,
            ClearingHouseId = clearingHouseId,
            HolidayStrategy = "Colombian",
            TimeZoneId = "America/Bogota"
        });

        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = clearingHouseId,
            Name = $"ACH {clearingHouseId}",
            Code = $"CH{clearingHouseId}",
            OriginCode = $"1234567{clearingHouseId}",
            ClearingHouseId = clearingHouseId
        });

        context.AchCycles.Add(new AchCycle
        {
            Id = cycleId,
            CycleName = $"Ciclo {cycleId}",
            ProcessingDate = processingDate,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = cutoff,
            ClearingHouseId = clearingHouseId
        });

        await context.SaveChangesAsync();
    }

    private static FixedTimeProvider CreateFixedClock()
        => new(FixedUtcInstant, TimeZoneInfo.Utc);
}
