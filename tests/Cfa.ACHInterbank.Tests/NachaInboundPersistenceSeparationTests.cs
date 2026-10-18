using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaInboundPersistenceSeparationTests
{
    [Fact]
    public async Task PersistAsync_ShouldPreserveExactFileMembershipLineageAndRorFields()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();

        var ingestion = new IncomingNachaFileIngestion
        {
            Id = Guid.NewGuid(),
            FileName = "1234567.001.20260816.1",
            FileHashSha256 = new string('a', 64),
            FileSize = 1060,
            CorrelationId = "r5c-lineage"
        };
        context.IncomingNachaFileIngestions.Add(ingestion);
        await context.SaveChangesAsync();

        var header = BuildGraph("R5C-NACHA-001", ingestion.Id);
        var sut = new NachaInboundPersistenceCoordinator(context);

        await sut.PersistAsync([header], CancellationToken.None);

        var persistedHeader = await context.NachaHeaders.AsNoTracking().SingleAsync();
        var batch = await context.BatchHeaders.AsNoTracking().SingleAsync();
        var entry = await context.EntryDetails.AsNoTracking().SingleAsync();
        var addenda = await context.AddendaRecords.AsNoTracking().SingleAsync();
        var batchControl = await context.BatchControls.AsNoTracking().SingleAsync();
        var fileControl = await context.FileControls.AsNoTracking().SingleAsync();

        persistedHeader.NachaID.Should().Be("R5C-NACHA-001");
        persistedHeader.IncomingNachaFileIngestionId.Should().Be(ingestion.Id);
        batch.NachaID.Should().Be(persistedHeader.NachaID);
        entry.NachaID.Should().Be(persistedHeader.NachaID);
        addenda.NachaID.Should().Be(persistedHeader.NachaID);
        batchControl.NachaID.Should().Be(persistedHeader.NachaID);
        fileControl.NachaID.Should().Be(persistedHeader.NachaID);
        entry.BatchHeaderId.Should().Be(batch.BatchID);
        addenda.EntryDetailId.Should().Be(entry.EntryDetailID);

        addenda.CodeTypeAddendumRecord.Should().Be("99");
        addenda.ReturnReasonCode.Should().Be("R61");
        addenda.OriginalTraceNumber.Should().Be("223456780003312");
        addenda.NewTraceNumber.Should().Be("765432158175914");
    }

    [Fact]
    public async Task PersistAsync_ShouldParticipateInAmbientTransactionWithoutCommittingIt()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var sut = new NachaInboundPersistenceCoordinator(context);

        await using var transaction = await context.Database.BeginTransactionAsync();
        await sut.PersistAsync([BuildGraph("R5C-ROLLBACK-001", null)], CancellationToken.None);

        (await context.NachaHeaders.CountAsync()).Should().Be(1);
        await transaction.RollbackAsync();
        context.ChangeTracker.Clear();

        (await context.NachaHeaders.AsNoTracking().CountAsync()).Should().Be(0);
        (await context.EntryDetails.AsNoTracking().CountAsync()).Should().Be(0);
        (await context.AddendaRecords.AsNoTracking().CountAsync()).Should().Be(0);
    }

    private static AchDbContext CreateContext(SqliteConnection connection)
        => new(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);

    private static ParsedNachaHeader BuildGraph(string nachaId, Guid? ingestionId)
        => new()
        {
            NachaID = nachaId,
            IncomingNachaFileIngestionId = ingestionId,
            Batches =
            [
                new ParsedBatchHeader
                {
                    BatchNumber = 1,
                    ServiceClassCode = "220",
                    StandardEntryClassCode = "PPD"
                }
            ],
            EntryDetails =
            [
                new ParsedEntryDetail
                {
                    TransactionCode = "21",
                    SequenceNumber = "123456780000001",
                    BatchNumber = 1,
                    Amount = 100m
                }
            ],
            AddendaRecords =
            [
                new ParsedAddendaRecord
                {
                    CodeTypeAddendumRecord = "99",
                    ReturnReasonCode = "R61",
                    OriginalTraceNumber = "223456780003312",
                    NewTraceNumber = "765432158175914",
                    EntryDetailSequenceNumber = "0000001"
                }
            ],
            BatchControls =
            [
                new ParsedBatchControl
                {
                    BatchNumber = "1",
                    EntryAddendaCount = 2
                }
            ],
            FileControls =
            [
                new ParsedFileControl
                {
                    BatchCount = 1,
                    EntryAddendaCount = 2
                }
            ]
        };
}
