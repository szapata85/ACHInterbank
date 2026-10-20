using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaManagedFileExchangeTests
{
    [Fact]
    public async Task Monitoring_ShouldProjectDurableEvidenceAndCommandEligibility_WithoutMutatingHistory()
    {
        await using var fixture = await Fixture.CreateAsync();
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.Uncertain, [3]);
        transfer.AttemptCount = fixture.Configuration.MaximumRetries + 1;
        transfer.LastAttemptAtUtc = new DateTime(2026, 9, 1, 12, 0, 0, DateTimeKind.Utc);
        transfer.LastErrorCode = "ACHCOL_MFT_IO_UNCERTAIN";
        transfer.Events.Add(new() { Id = 20, OccurredAtUtc = transfer.LastAttemptAtUtc.Value, EventType = "OutboundAttempt", Result = "Uncertain" });
        transfer.Events.Add(new() { Id = 10, OccurredAtUtc = transfer.LastAttemptAtUtc.Value, EventType = "OutboundAttempt", Result = "Started" });
        await fixture.Context.SaveChangesAsync();
        fixture.Context.ChangeTracker.Clear();

        var detail = await fixture.Service.GetAsync(transfer.Id);

        Assert.NotNull(detail);
        Assert.Equal(transfer.LastAttemptAtUtc, detail.LastAttemptAtUtc);
        Assert.Equal(transfer.LastErrorCode, detail.LastErrorCode);
        Assert.Equal(transfer.CorrelationId, detail.CorrelationId);
        Assert.True(detail.ContentAvailable);
        Assert.False(detail.CanRetry);
        Assert.False(detail.CanReprocess);
        Assert.True(detail.CanArchive);
        Assert.Equal(new long[] { 10, 20 }, detail.History.Select(x => x.Id));
        Assert.Empty(fixture.Context.ChangeTracker.Entries());

        var retired = await fixture.Service.RetireAsync(transfer.Id, "operator", "Test retention completed");
        Assert.False(retired.ContentAvailable);
        Assert.False(retired.CanRetry);
        Assert.False(retired.CanArchive);
        Assert.False(retired.CanRetire);
    }

    [Theory]
    [InlineData(AchManagedFileExecutionOrigin.Automatic, AchManagedFileExecutionOrigin.Manual)]
    [InlineData(AchManagedFileExecutionOrigin.Manual, AchManagedFileExecutionOrigin.Automatic)]
    public async Task RetryTimeline_ShouldRecordActualTrigger_AndPreserveInitialOrigin(
        AchManagedFileExecutionOrigin initial, AchManagedFileExecutionOrigin retry)
    {
        await using var fixture = await Fixture.CreateAsync();
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.RetryPending, [3]);
        transfer.ExecutionOrigin = initial;
        transfer.AchCycleId = "ACH-1";
        fixture.Configuration.AutomaticOutboundEnabled = true;
        await fixture.Context.SaveChangesAsync();

        if (retry == AchManagedFileExecutionOrigin.Manual)
            await fixture.Service.RetryAsync(transfer.Id, "operator", "manual-retry");
        else
            await fixture.Service.ExecuteOutboundAsync("ACH-1", retry, "task:AchColombiaManagedMftOutbound", "task-retry");

        var detail = await fixture.Service.GetAsync(transfer.Id);
        Assert.Equal(initial, detail!.ExecutionOrigin);
        Assert.All(detail.History.Where(x => x.EventType == "OutboundAttempt"), x => Assert.Equal(retry, x.ExecutionOrigin));
        Assert.Equal(2, detail.History.Count(x => x.EventType == "OutboundAttempt"));
    }

    [Fact]
    public async Task MonitoringSearch_ShouldFilterAndPageDeterministically()
    {
        await using var fixture = await Fixture.CreateAsync();
        var one = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.Uncertain, [1]);
        var two = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.Uncertain, [2]);
        one.PhysicalFileName = "one.OUT.env";
        two.PhysicalFileName = "two.OUT.env";
        one.CreatedAtUtc = two.CreatedAtUtc = new(2026, 9, 1);
        two.ArchivedAtUtc = new(2026, 9, 2);
        await fixture.Context.SaveChangesAsync();

        var first = await fixture.Service.QueryAsync(new(PageSize: 1));
        var second = await fixture.Service.QueryAsync(new(PageNumber: 2, PageSize: 1));
        Assert.Single(first);
        Assert.Single(second);
        Assert.NotEqual(first[0].Id, second[0].Id);
        Assert.Equal(first[0].Id, (await fixture.Service.QueryAsync(new(PageSize: 1)))[0].Id);
        Assert.Equal(two.Id, Assert.Single(await fixture.Service.QueryAsync(new(FileName: "two.OUT", Archived: true))).Id);
        Assert.Equal(one.Id, Assert.Single(await fixture.Service.QueryAsync(new(TransferId: one.Id, Status: AchManagedFileTransferStatus.Uncertain))).Id);
        Assert.Empty(await fixture.Service.QueryAsync(new(TransferId: one.Id, Archived: true)));
    }

    [Fact]
    public async Task Administration_SftpEndpoint_ShouldRequireProtectedPasswordAndHostKey_WithoutReturningSecret()
    {
        await using var fixture = await Fixture.CreateAsync();
        var credentialBefore = await fixture.Service.GetAdministrationAsync();
        await fixture.Service.SetCredentialAsync(new("Password", "super-secret", credentialBefore.ConcurrencyToken), "operator");
        var before = await fixture.Service.GetAdministrationAsync();
        var fingerprint = "SHA256:" + Convert.ToBase64String(new byte[32]).TrimEnd('=');

        var updated = await fixture.Service.UpdateAdministrationAsync(new(
            "MFT SFTP CFA", "SftpEndpoint", "Sftp", true, "sftp.cfa.invalid", 22, "svc-ach",
            false, false, true, true, 4, 120, 180,
            "/input", "/output", "/archive", before.ConcurrencyToken)
        {
            SftpHostKeyFingerprint = fingerprint,
            SftpTimeoutSeconds = 40
        }, "operator");

        Assert.Equal("SftpEndpoint", updated.Provider);
        Assert.Equal("Sftp", updated.Protocol);
        Assert.Equal(fingerprint, updated.SftpHostKeyFingerprint);
        Assert.Equal(40, updated.SftpTimeoutSeconds);
        Assert.DoesNotContain("super-secret", System.Text.Json.JsonSerializer.Serialize(updated));

        var conflict = updated with { };
        var invalid = new UpdateAchManagedMftAdministrationRequest(
            "bad", "SftpEndpoint", "ManagedFile", true, "sftp.cfa.invalid", 22, "svc-ach",
            false, false, true, true, 4, 120, 180, "/input", "/output", "/archive", conflict.ConcurrencyToken)
        {
            SftpHostKeyFingerprint = fingerprint
        };
        await Assert.ThrowsAsync<ArgumentException>(() => fixture.Service.UpdateAdministrationAsync(invalid, "operator"));
    }

    [Theory]
    [InlineData(AchManagedFileExecutionOrigin.Manual)]
    [InlineData(AchManagedFileExecutionOrigin.Automatic)]
    public async Task Outbound_ShouldUseSameServicePath_AndPersistImmutableContent(AchManagedFileExecutionOrigin origin)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticOutboundEnabled = true;
        await fixture.Context.SaveChangesAsync();
        var result = await fixture.Service.ExecuteOutboundAsync("ACH-1", origin, "operator", $"out-{origin}");
        Assert.Equal(1, result.Succeeded);
        var transfer = await fixture.Context.AchManagedFileTransfers.Include(x => x.Events).SingleAsync();
        Assert.Equal(AchManagedFileTransferStatus.Transferred, transfer.Status);
        Assert.NotNull(transfer.RetainedContent);
        Assert.Contains(transfer.Events, x => x.EventType == "OutboundPrepared");
        Assert.Contains(transfer.Events, x => x.EventType == "OutboundAttempt" && x.Result == "Succeeded");
    }

    [Fact]
    public async Task AutomaticOutboundDisabled_ShouldNotGenerateOrTransfer()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Service.ExecuteOutboundAsync("ACH-1", AchManagedFileExecutionOrigin.Automatic, "task", "disabled");
        Assert.Equal(0, result.Processed);
        fixture.Builder.Verify(x => x.BuildNachaFileArtifactByCycleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ManualOutbound_ShouldRemainAvailableWhenAutomaticExecutionIsDisabled()
    {
        await using var fixture = await Fixture.CreateAsync(new DateTimeOffset(2026, 8, 31, 14, 0, 0, TimeSpan.Zero));
        var result = await fixture.Service.ExecuteOutboundAsync("ACH-1", AchManagedFileExecutionOrigin.Manual, "operator", "manual-only");
        Assert.Equal(1, result.Succeeded);
    }

    [Fact]
    public async Task AutomaticOutbound_OutsideCycleSendingWindow_ShouldFailClosedBeforeBuildingFile()
    {
        await using var fixture = await Fixture.CreateAsync(new DateTimeOffset(2026, 8, 31, 14, 0, 0, TimeSpan.Zero));
        fixture.Configuration.AutomaticOutboundEnabled = true;
        await fixture.Context.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ExecuteOutboundAsync(
            "ACH-1", AchManagedFileExecutionOrigin.Automatic, "task", "outside-window"));

        Assert.Equal("ACHCOL_MFT_OUTBOUND_OUTSIDE_CYCLE_WINDOW", error.Message);
        fixture.Builder.Verify(x => x.BuildNachaFileArtifactByCycleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Empty(await fixture.Context.AchManagedFileTransfers.ToListAsync());
    }

    [Fact]
    public async Task AutomaticOutbound_RepeatedPollingInsideSameCycle_ShouldReuseOfficialTransfer()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticOutboundEnabled = true;
        await fixture.Context.SaveChangesAsync();

        var first = await fixture.Service.ExecuteOutboundAsync(
            "ACH-1", AchManagedFileExecutionOrigin.Automatic, "task", "poll-1");
        var second = await fixture.Service.ExecuteOutboundAsync(
            "ACH-1", AchManagedFileExecutionOrigin.Automatic, "task", "poll-2");

        Assert.Equal(1, first.Succeeded);
        Assert.Equal(1, second.Succeeded);
        Assert.Single(await fixture.Context.AchManagedFileTransfers.ToListAsync());
        Assert.Single(await fixture.Context.AchFileExports.ToListAsync());
        fixture.Builder.Verify(x => x.BuildNachaFileArtifactByCycleAsync("ACH-1", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, fixture.Adapter.OutboundHandoffCount);
    }

    [Fact]
    public async Task AutomaticOutbound_DuplicateLogicalExecutionFromTwoSchedulerNodes_ShouldHaveSingleFunctionalEffect()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticOutboundEnabled = true;
        await fixture.Context.SaveChangesAsync();
        const string logicalExecution = "task:AchColombiaManagedMftOutbound:CH:1:DATE:20260831:CYCLE:ACH-1:FLOW:OUTBOUND";

        var first = await fixture.Service.ExecuteOutboundAsync(
            "ACH-1", AchManagedFileExecutionOrigin.Automatic, "scheduler-node-a", logicalExecution);
        var second = await fixture.Service.ExecuteOutboundAsync(
            "ACH-1", AchManagedFileExecutionOrigin.Automatic, "scheduler-node-b", logicalExecution);

        Assert.Equal(1, first.Succeeded);
        Assert.Equal(1, second.Succeeded);
        Assert.Single(await fixture.Context.AchManagedFileTransfers.ToListAsync());
        Assert.Single(await fixture.Context.AchFileExports.ToListAsync());
        fixture.Builder.Verify(x => x.BuildNachaFileArtifactByCycleAsync("ACH-1", It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(1, fixture.Adapter.OutboundHandoffCount);
    }

    [Theory]
    [InlineData(AchManagedFileDirection.Outbound)]
    [InlineData(AchManagedFileDirection.Inbound)]
    public async Task DisabledProfile_ShouldFailClosedForManualExecution(AchManagedFileDirection direction)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.ProfileEnabled = false;
        await fixture.Context.SaveChangesAsync();

        var error = direction == AchManagedFileDirection.Outbound
            ? await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ExecuteOutboundAsync("ACH-1", AchManagedFileExecutionOrigin.Manual, "operator", "profile-off"))
            : await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, "operator", "profile-off"));

        Assert.Equal("ACHCOL_MFT_DISABLED", error.Message);
    }

    [Fact]
    public async Task Administration_ShouldPersistSafeConfigurationAndProtectedCredential()
    {
        await using var fixture = await Fixture.CreateAsync();
        var before = await fixture.Service.GetAdministrationAsync();
        var updated = await fixture.Service.UpdateAdministrationAsync(new(
            "MFT principal", "ManagedFolder", "ManagedFile", true, "mft.local", 443, "operador",
            false, false, true, true, 4, 120, 180,
            "out-persisted", "in-persisted", "archive-persisted", before.ConcurrencyToken), "operator");

        var credential = await fixture.Service.SetCredentialAsync(new("Password", "super-secret", updated.ConcurrencyToken), "operator");
        var persisted = await fixture.Context.AchManagedFileTransferConfigurations.SingleAsync();
        var read = await fixture.Service.GetAdministrationAsync();

        Assert.Equal("out-persisted", updated.OutboundLocation);
        Assert.True(credential.CredentialConfigured);
        Assert.NotEqual("super-secret", persisted.ProtectedCredential);
        Assert.DoesNotContain("super-secret", System.Text.Json.JsonSerializer.Serialize(read));
        Assert.DoesNotContain(await fixture.Context.AuditLogs.Select(x => (x.BeforeJson ?? "") + (x.AfterJson ?? "") + (x.ChangedFields ?? "")).ToListAsync(), x => x.Contains("super-secret", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CredentialUpdate_ShouldRejectStaleConcurrencyToken()
    {
        await using var fixture = await Fixture.CreateAsync();
        var before = await fixture.Service.GetAdministrationAsync();

        await fixture.Service.SetCredentialAsync(new("Password", "first-secret", before.ConcurrencyToken), "operator");

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() =>
            fixture.Service.SetCredentialAsync(new("Password", "second-secret", before.ConcurrencyToken), "operator"));
    }

    [Fact]
    public async Task EffectiveConfiguration_ShouldUsePersistedRoutesAndStaticProcessingLimits()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.ProfileEnabled = true;
        fixture.Configuration.Provider = "SftpEndpoint";
        fixture.Configuration.Protocol = "Sftp";
        fixture.Configuration.Endpoint = "sftp.cfa.invalid";
        fixture.Configuration.Port = 22;
        fixture.Configuration.Principal = "svc-ach";
        fixture.Configuration.CredentialType = "Password";
        fixture.Configuration.ProtectedCredential = "protected-value";
        fixture.Configuration.SftpHostKeyFingerprint = "SHA256:" + Convert.ToBase64String(new byte[32]).TrimEnd('=');
        fixture.Configuration.SftpTimeoutSeconds = 45;
        fixture.Configuration.OutboundLocation = "out-persisted";
        fixture.Configuration.InboundLocation = "in-persisted";
        fixture.Configuration.ArchiveLocation = "archive-persisted";
        await fixture.Context.SaveChangesAsync();

        var provider = new AchColombiaManagedMftConfigurationProvider(fixture.Context, Options.Create(new AchColombiaManagedMftOptions
        { ProcessingPath = "processing-static", MaximumFileBytes = 123 }));
        var effective = await provider.GetEffectiveAsync();

        Assert.True(effective.Enabled);
        Assert.Equal("out-persisted", effective.OutboundPath);
        Assert.Equal("in-persisted", effective.InboundPath);
        Assert.Equal("archive-persisted", effective.ArchivePath);
        Assert.Equal("processing-static", effective.ProcessingPath);
        Assert.Equal(123, effective.MaximumFileBytes);
        Assert.Equal("SftpEndpoint", effective.Provider);
        Assert.Equal("Sftp", effective.Protocol);
        Assert.Equal("sftp.cfa.invalid", effective.Endpoint);
        Assert.Equal(22, effective.Port);
        Assert.Equal("svc-ach", effective.Principal);
        Assert.Equal("Password", effective.CredentialType);
        Assert.Equal("protected-value", effective.ProtectedCredential);
        Assert.Equal(fixture.Configuration.SftpHostKeyFingerprint, effective.SftpHostKeyFingerprint);
        Assert.Equal(45, effective.SftpTimeoutSeconds);
    }

    [Theory]
    [InlineData(AchManagedFileExecutionOrigin.Manual)]
    [InlineData(AchManagedFileExecutionOrigin.Automatic)]
    public async Task Inbound_ShouldUseSameServicePath_AndArchive(AchManagedFileExecutionOrigin origin)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticInboundEnabled = true;
        fixture.Adapter.Artifacts = [new("received.OUT.env", [7, 8], Hash([7, 8]), "claim")];
        await fixture.Context.SaveChangesAsync();
        var result = await fixture.Service.ExecuteInboundAsync(origin, "operator", $"in-{origin}");
        Assert.Equal(1, result.Succeeded);
        var transfer = await fixture.Context.AchManagedFileTransfers.Include(x => x.Events).SingleAsync();
        Assert.Equal(AchManagedFileTransferStatus.Processed, transfer.Status);
        Assert.NotNull(transfer.ArchiveReference);
        Assert.Contains(transfer.Events, x => x.EventType == "Archived");
    }

    [Theory]
    [InlineData("received.OUT.env")]
    [InlineData("received.RET.env")]
    public async Task Inbound_ShouldProcessOnlyAuthorizedOutAndRetFinalEnvelopes(string fileName)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticInboundEnabled = true;
        fixture.Adapter.Artifacts = [new(fileName, [7, 8], Hash([7, 8]), $"claim:{fileName}")];
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Automatic,
            "task",
            $"authorized-{fileName}");

        Assert.Equal(1, result.Succeeded);
        fixture.Ingestion.Verify(
            x => x.IngestAsync(
                It.Is<IncomingNachaIngestionRequest>(r => r.FileName == fileName),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Inbound_ShouldIgnoreUnknownEnvelopeNameDefensively()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticInboundEnabled = true;
        fixture.Adapter.Artifacts = [new("unknown.env", [7, 8], Hash([7, 8]), "claim:unknown.env")];
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Automatic,
            "task",
            "unknown-name");

        Assert.Empty(result.TransferIds);
        Assert.Equal(0, await fixture.Context.AchManagedFileTransfers.CountAsync());
        fixture.Ingestion.Verify(
            x => x.IngestAsync(It.IsAny<IncomingNachaIngestionRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LateInbound_ShouldPreserveResolvedAchCycleIdFromIngestion()
    {
        await using var fixture = await Fixture.CreateAsync(
            new DateTimeOffset(2026, 8, 31, 15, 0, 0, TimeSpan.Zero));
        fixture.Configuration.AutomaticInboundEnabled = true;
        var cycle = await fixture.Context.AchCycles.SingleAsync(x => x.Id == "ACH-1");
        cycle.OutputReleaseStartTime = new(9, 15, 0);
        cycle.OutputReleaseEndTime = new(9, 30, 0);
        fixture.Ingestion.Setup(x => x.IngestAsync(It.IsAny<IncomingNachaIngestionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncomingNachaIngestionRequest request, CancellationToken _) => new IncomingNachaIngestionResponse
            {
                IngestionId = Guid.NewGuid(),
                OriginalFileName = request.FileName,
                IngestionStatus = IncomingNachaIngestionStatus.Completado,
                ParsingStatus = IncomingNachaParsingStatus.Exitoso,
                OperationalDate = new(2026, 8, 31),
                ResolvedAchCycleId = "ACH-1"
            });
        fixture.Adapter.Artifacts = [new("late.OUT.env", [7, 9], Hash([7, 9]), "claim-late")
        {
            BoundaryLastWriteTimeUtc = new DateTime(2026, 8, 31, 15, 0, 0, DateTimeKind.Utc)
        }];
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Automatic, "task", "late-after-release");

        Assert.Equal(1, result.Succeeded);
        var transfer = await fixture.Context.AchManagedFileTransfers.Include(x => x.Events).SingleAsync();
        Assert.Equal("ACH-1", transfer.AchCycleId);
        Assert.Contains(transfer.Events, x => x.EventType == "InboundOutsideExpectedWindow");
        var detail = await fixture.Service.GetAsync(transfer.Id);
        Assert.Contains(detail!.History, x => x.EventType == "InboundOutsideExpectedWindow"
            && x.DisplayName == "Recibido fuera del horario esperado");
    }

    [Fact]
    public async Task InboundBoundaryTimestampInsideResolvedCycleWindow_ShouldNotBeMarkedLate()
    {
        await using var fixture = await Fixture.CreateAsync(
            new DateTimeOffset(2026, 8, 31, 14, 20, 0, TimeSpan.Zero));
        fixture.Configuration.AutomaticInboundEnabled = true;
        var cycle = await fixture.Context.AchCycles.SingleAsync(x => x.Id == "ACH-1");
        cycle.OutputReleaseStartTime = new(9, 15, 0);
        cycle.OutputReleaseEndTime = new(9, 30, 0);
        fixture.Ingestion.Setup(x => x.IngestAsync(It.IsAny<IncomingNachaIngestionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncomingNachaIngestionRequest request, CancellationToken _) => new IncomingNachaIngestionResponse
            {
                IngestionId = Guid.NewGuid(),
                OriginalFileName = request.FileName,
                IngestionStatus = IncomingNachaIngestionStatus.Completado,
                ParsingStatus = IncomingNachaParsingStatus.Exitoso,
                OperationalDate = new(2026, 8, 31),
                ResolvedAchCycleId = "ACH-1"
            });
        fixture.Adapter.Artifacts = [new("inside.OUT.env", [2, 7], Hash([2, 7]), "claim-inside")
        {
            BoundaryLastWriteTimeUtc = new DateTime(2026, 8, 31, 14, 20, 0, DateTimeKind.Utc)
        }];
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Automatic, "task", "inside-release-window");

        Assert.Equal(1, result.Succeeded);
        var transfer = await fixture.Context.AchManagedFileTransfers.Include(x => x.Events).SingleAsync();
        Assert.DoesNotContain(transfer.Events, x => x.EventType == "InboundOutsideExpectedWindow");
    }

    [Fact]
    public async Task ManualInbound_ShouldRemainUngatedBeforeConfiguredReleaseStart()
    {
        await using var fixture = await Fixture.CreateAsync(
            new DateTimeOffset(2026, 8, 31, 13, 0, 0, TimeSpan.Zero));
        var cycle = await fixture.Context.AchCycles.SingleAsync(x => x.Id == "ACH-1");
        cycle.OutputReleaseStartTime = new(9, 15, 0);
        cycle.OutputReleaseEndTime = new(9, 30, 0);
        fixture.Ingestion.Setup(x => x.IngestAsync(It.IsAny<IncomingNachaIngestionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IncomingNachaIngestionRequest request, CancellationToken _) => new IncomingNachaIngestionResponse
            {
                IngestionId = Guid.NewGuid(),
                OriginalFileName = request.FileName,
                IngestionStatus = IncomingNachaIngestionStatus.Completado,
                ParsingStatus = IncomingNachaParsingStatus.Exitoso,
                OperationalDate = new(2026, 8, 31),
                ResolvedAchCycleId = "ACH-1"
            });
        fixture.Adapter.Artifacts = [new("manual-before-release.OUT.env", [6, 7], Hash([6, 7]), "claim-manual")];
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Manual, "operator", "manual-before-release");

        Assert.Equal(1, result.Succeeded);
        Assert.Equal("ACH-1", (await fixture.Context.AchManagedFileTransfers.SingleAsync()).AchCycleId);
    }

    [Fact]
    public async Task InboundDuplicate_ShouldNotCreateSecondTransfer()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Adapter.Artifacts = [new("first.OUT.env", [9], Hash([9]), "claim-1")];
        await fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, "operator", "first");
        fixture.Adapter.Artifacts = [new("renamed.OUT.env", [9], Hash([9]), "claim-2")];
        await fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, "operator", "second");
        Assert.Equal(1, await fixture.Context.AchManagedFileTransfers.CountAsync());
        Assert.Contains(await fixture.Context.AchManagedFileTransferEvents.ToListAsync(), x => x.EventType == "DuplicateDetected");
    }

    [Fact]
    public async Task SameInboundNameWithDifferentContent_ShouldReject()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Adapter.Artifacts = [new("same.OUT.env", [1], Hash([1]), "claim-1")];
        await fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, "operator", "first");
        fixture.Adapter.Artifacts = [new("same.OUT.env", [2], Hash([2]), "claim-2")];
        await fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, "operator", "second");
        Assert.Equal(AchManagedFileTransferStatus.Rejected, (await fixture.Context.AchManagedFileTransfers.OrderBy(x => x.CreatedAtUtc).ToListAsync()).Last().Status);
    }

    [Fact]
    public async Task InboundRestartRecovery_ShouldResumePersistedClaimWithoutCreatingAnotherTransfer()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticInboundEnabled = true;
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Inbound, AchManagedFileTransferStatus.Received, [8]);
        transfer.CreatedAtUtc = fixture.NowUtc.UtcDateTime.AddMinutes(-5);
        fixture.Adapter.Artifacts = [new("file.OUT.env", [8], Hash([8]), "processing:file.OUT.env")];
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Automatic, "task", "recovery");

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, await fixture.Context.AchManagedFileTransfers.CountAsync());
        Assert.Contains(await fixture.Context.AchManagedFileTransferEvents.ToListAsync(), x => x.EventType == "InboundRecovery");
        var detail = await fixture.Service.GetAsync(transfer.Id);
        Assert.Equal(AchManagedFileExecutionOrigin.Manual, detail!.ExecutionOrigin);
        Assert.All(detail.History.Where(x => x.EventType.StartsWith("InboundProcessing", StringComparison.Ordinal)),
            x => Assert.Equal(AchManagedFileExecutionOrigin.Automatic, x.ExecutionOrigin));
        Assert.NotNull(detail.IncomingNachaFileIngestionId);
    }

    [Fact]
    public async Task InboundRecentClaim_ShouldNotBeTakenOverBeforeRecoveryDelay()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticInboundEnabled = true;
        fixture.Configuration.RetryDelaySeconds = 60;
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Inbound, AchManagedFileTransferStatus.Received, [4, 5]);
        transfer.ActiveStorageReference = "processing:file.OUT.env";
        transfer.CreatedAtUtc = fixture.NowUtc.UtcDateTime.AddSeconds(-60).AddTicks(1);
        fixture.Adapter.Artifacts = [new("file.OUT.env", [4, 5], Hash([4, 5]), "processing:file.OUT.env")];
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Automatic,
            "task",
            "concurrent-owner");

        Assert.Equal(0, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(AchManagedFileTransferStatus.Received,
            (await fixture.Context.AchManagedFileTransfers.SingleAsync(x => x.Id == transfer.Id)).Status);
        fixture.Ingestion.Verify(
            x => x.IngestAsync(It.IsAny<IncomingNachaIngestionRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
        Assert.Equal(0, fixture.Adapter.ArchiveCallCount);
    }

    [Fact]
    public async Task InboundCrashAfterClaim_ShouldPersistRecoveryPending_AndNotArchive()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticInboundEnabled = true;
        fixture.Ingestion.Reset();
        fixture.Ingestion.Setup(x => x.IngestAsync(It.IsAny<IncomingNachaIngestionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("simulated process interruption"));
        fixture.Adapter.Artifacts = [new("crash.OUT.env", [3, 4], Hash([3, 4]), "claim:crash.OUT.env")];
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Automatic,
            "task",
            "crash-after-claim");

        Assert.Equal(1, result.Failed);
        var transfer = await fixture.Context.AchManagedFileTransfers.Include(x => x.Events).SingleAsync();
        Assert.Equal(AchManagedFileTransferStatus.RetryPending, transfer.Status);
        Assert.Null(transfer.ArchivedAtUtc);
        Assert.Equal("claim:crash.OUT.env", transfer.ActiveStorageReference);
        Assert.Contains(transfer.Events, x => x.EventType == "InboundRecoveryPending");
        Assert.Contains(transfer.Events, x => x.EventType == "InboundProcessingFailed");
        var detail = await fixture.Service.GetAsync(transfer.Id);
        Assert.Contains(detail!.History, x => x.EventType == "InboundRecoveryPending"
            && x.DisplayName == "Pendiente de recuperación");
        Assert.Contains(detail.History, x => x.EventType == "InboundProcessingFailed"
            && x.DisplayName == "No se pudo procesar");
        Assert.Equal(0, fixture.Adapter.ArchiveCallCount);
    }

    [Fact]
    public async Task InboundRecovery_ShouldResumeAfterRetryDelay_ThenArchive()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticInboundEnabled = true;
        fixture.Configuration.RetryDelaySeconds = 1;
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Inbound, AchManagedFileTransferStatus.RetryPending, [5, 6]);
        transfer.ActiveStorageReference = "claim:file.OUT.env";
        transfer.LastAttemptAtUtc = fixture.NowUtc.UtcDateTime.AddSeconds(-1);
        transfer.CreatedAtUtc = fixture.NowUtc.UtcDateTime.AddSeconds(-1);
        fixture.Adapter.Artifacts = [new("file.OUT.env", [5, 6], Hash([5, 6]), "claim:file.OUT.env")];
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Automatic,
            "task",
            "recover-after-crash");

        Assert.Equal(1, result.Succeeded);
        var recovered = await fixture.Context.AchManagedFileTransfers.Include(x => x.Events).SingleAsync(x => x.Id == transfer.Id);
        Assert.Equal(AchManagedFileTransferStatus.Processed, recovered.Status);
        Assert.NotNull(recovered.ArchivedAtUtc);
        Assert.Null(recovered.ActiveStorageReference);
        Assert.Contains(recovered.Events, x => x.EventType == "InboundRecovery");
        Assert.Equal(1, fixture.Adapter.ArchiveCallCount);
    }

    [Fact]
    public async Task InboundRecovery_OneTickAfterRetryBoundary_ShouldResume()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticInboundEnabled = true;
        fixture.Configuration.RetryDelaySeconds = 60;
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Inbound, AchManagedFileTransferStatus.RetryPending, [6, 7]);
        transfer.ActiveStorageReference = "claim:file.OUT.env";
        transfer.LastAttemptAtUtc = fixture.NowUtc.UtcDateTime.AddSeconds(-60).AddTicks(-1);
        transfer.CreatedAtUtc = transfer.LastAttemptAtUtc.Value;
        fixture.Adapter.Artifacts = [new("file.OUT.env", [6, 7], Hash([6, 7]), "claim:file.OUT.env")];
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Automatic,
            "task",
            "recover-after-boundary");

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(AchManagedFileTransferStatus.Processed,
            (await fixture.Context.AchManagedFileTransfers.SingleAsync(x => x.Id == transfer.Id)).Status);
    }

    [Fact]
    public async Task InboundArchiveFailure_ShouldRecoverWithoutReprocessingFunctionalResult()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticInboundEnabled = true;
        var content = new byte[] { 8, 1 };
        fixture.Adapter.Artifacts = [new("archive-recovery.OUT.env", content, Hash(content), "claim:archive-recovery.OUT.env")];
        fixture.Adapter.ArchiveException = new IOException("simulated archive interruption");
        await fixture.Context.SaveChangesAsync();

        var first = await fixture.Service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Automatic,
            "task",
            "archive-recovery-first");

        Assert.Equal(1, first.Failed);
        var persisted = await fixture.Context.AchManagedFileTransfers.Include(x => x.Events).SingleAsync();
        Assert.Equal(AchManagedFileTransferStatus.Processed, persisted.Status);
        Assert.NotNull(persisted.IncomingNachaFileIngestionId);
        Assert.Null(persisted.ArchivedAtUtc);
        Assert.Equal("claim:archive-recovery.OUT.env", persisted.ActiveStorageReference);
        Assert.Contains(persisted.Events, x => x.EventType == "InboundArchivePending");
        fixture.Ingestion.Verify(
            x => x.IngestAsync(It.IsAny<IncomingNachaIngestionRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);

        fixture.Adapter.ArchiveException = null;
        var second = await fixture.Service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Automatic,
            "task",
            "archive-recovery-second");

        Assert.Equal(1, second.Succeeded);
        persisted = await fixture.Context.AchManagedFileTransfers.Include(x => x.Events).SingleAsync();
        Assert.NotNull(persisted.ArchivedAtUtc);
        Assert.Null(persisted.ActiveStorageReference);
        Assert.Contains(persisted.Events, x => x.EventType == "InboundRecovery");
        fixture.Ingestion.Verify(
            x => x.IngestAsync(It.IsAny<IncomingNachaIngestionRequest>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Inbound_ShouldPersistProcessingResultBeforeArchiveMove()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.AutomaticInboundEnabled = true;
        fixture.Adapter.Artifacts = [new("ordered.OUT.env", [8, 9], Hash([8, 9]), "claim:ordered.OUT.env")];
        fixture.Adapter.BeforeArchiveAsync = async artifact =>
        {
            var transfer = await fixture.Context.AchManagedFileTransfers.SingleAsync(x => x.PhysicalFileName == artifact.FileName);
            Assert.Equal(AchManagedFileTransferStatus.Processed, transfer.Status);
            Assert.NotNull(transfer.IncomingNachaFileIngestionId);
        };
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Automatic,
            "task",
            "persist-before-archive");

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, fixture.Adapter.ArchiveCallCount);
    }

    [Fact]
    public async Task Retry_ShouldReuseRetainedContentWithoutRegeneration()
    {
        await using var fixture = await Fixture.CreateAsync();
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.RetryPending, [3]);
        await fixture.Context.SaveChangesAsync();
        await fixture.Service.RetryAsync(transfer.Id, "operator", "retry-1");
        fixture.Builder.Verify(x => x.BuildNachaFileArtifactByCycleAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Equal(AchManagedFileTransferStatus.Transferred, (await fixture.Context.AchManagedFileTransfers.FindAsync(transfer.Id))!.Status);
    }

    [Theory]
    [InlineData(true, false, "ACHCOL_MFT_RETRY_NOT_ALLOWED")]
    [InlineData(false, true, "ACHCOL_MFT_DISABLED")]
    public async Task RetryPermissionDenied_ShouldFailBeforeTransportSideEffects(
        bool profileEnabled, bool manualOutboundAllowed, string expectedError)
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Configuration.ProfileEnabled = profileEnabled;
        fixture.Configuration.ManualOutboundAllowed = manualOutboundAllowed;
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.Uncertain, [3]);
        await fixture.Context.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Service.RetryAsync(transfer.Id, "operator", "denied-retry"));

        Assert.Equal(expectedError, error.Message);
        Assert.Equal(0, fixture.Adapter.OutboundHandoffCount);
        var persisted = await fixture.Context.AchManagedFileTransfers.Include(x => x.Events).SingleAsync(x => x.Id == transfer.Id);
        Assert.Equal(0, persisted.AttemptCount);
        Assert.DoesNotContain(persisted.Events, x => x.EventType == "OutboundAttempt");
        Assert.Equal(AchManagedFileTransferStatus.Uncertain, persisted.Status);
        Assert.Equal(new byte[] { 3 }, persisted.RetainedContent);
        Assert.False((await fixture.Service.GetAsync(transfer.Id))!.CanRetry);
    }

    [Fact]
    public async Task RetryPermissionAllowed_ShouldPreserveManualAttemptAndEligibility()
    {
        await using var fixture = await Fixture.CreateAsync();
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.RetryPending, [3]);
        await fixture.Context.SaveChangesAsync();
        Assert.True((await fixture.Service.GetAsync(transfer.Id))!.CanRetry);

        var detail = await fixture.Service.RetryAsync(transfer.Id, "operator", "allowed-retry");

        Assert.Equal(1, fixture.Adapter.OutboundHandoffCount);
        Assert.Equal(1, detail.AttemptCount);
        Assert.Equal(AchManagedFileTransferStatus.Transferred, detail.Status);
        var attempts = detail.History.Where(x => x.EventType == "OutboundAttempt").ToArray();
        Assert.Equal(2, attempts.Length);
        Assert.All(attempts, x => Assert.Equal(AchManagedFileExecutionOrigin.Manual, x.ExecutionOrigin));
        Assert.All(attempts, x => Assert.Equal("operator", x.Actor));
    }

    [Fact]
    public async Task UncertainTransport_ShouldRemainEligibleForControlledRetry()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Adapter.OutboundResult = new(false, true, true, "TIMEOUT", "Resultado no confirmado.", null);
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.RetryPending, [3]);
        await fixture.Context.SaveChangesAsync();
        var detail = await fixture.Service.RetryAsync(transfer.Id, "operator", "retry-uncertain");
        Assert.Equal(AchManagedFileTransferStatus.Uncertain, detail.Status);
    }

    [Fact]
    public async Task NonRetryableTransportFailure_ShouldFailClosedForManualRetry()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Adapter.OutboundResult = new(false, false, false, "REJECTED", "Entrega rechazada.", null);
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.RetryPending, [3]);
        await fixture.Context.SaveChangesAsync();
        var detail = await fixture.Service.RetryAsync(transfer.Id, "operator", "first");
        Assert.Equal(AchManagedFileTransferStatus.Failed, detail.Status);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.RetryAsync(transfer.Id, "operator", "second"));
    }

    [Fact]
    public async Task Reprocess_ShouldRequireEligibleStateAndBecomeIdempotentlyIneligibleAfterSuccess()
    {
        await using var fixture = await Fixture.CreateAsync();
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Inbound, AchManagedFileTransferStatus.Rejected, [4]);
        transfer.ExecutionOrigin = AchManagedFileExecutionOrigin.Automatic;
        transfer.IncomingNachaFileIngestionId = Guid.NewGuid();
        await fixture.Context.SaveChangesAsync();
        var detail = await fixture.Service.ReprocessAsync(transfer.Id, "operator");
        Assert.Equal(AchManagedFileTransferStatus.Processed, detail.Status);
        Assert.Equal(AchManagedFileExecutionOrigin.Automatic, detail.ExecutionOrigin);
        Assert.All(detail.History.Where(x => x.EventType.StartsWith("Reprocess", StringComparison.Ordinal)),
            x => Assert.Equal(AchManagedFileExecutionOrigin.Manual, x.ExecutionOrigin));
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Service.ReprocessAsync(transfer.Id, "operator"));
    }

    [Fact]
    public async Task ArchiveDownloadAndRetire_ShouldPreserveHistoryAndMetadata()
    {
        await using var fixture = await Fixture.CreateAsync();
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.Transferred, [4, 5]);
        await fixture.Context.SaveChangesAsync();
        await fixture.Service.ArchiveAsync(transfer.Id, "operator");
        Assert.NotNull(await fixture.Service.DownloadAsync(transfer.Id, "operator"));
        var retired = await fixture.Service.RetireAsync(transfer.Id, "operator", "Fin de conservación operativa");
        Assert.True(retired.Retired);
        Assert.Equal(Hash([4, 5]), retired.ContentSha256);
        Assert.NotEmpty(retired.History);
        Assert.Null(await fixture.Service.DownloadAsync(transfer.Id, "operator"));
    }

    [Fact]
    public async Task CorrectedOutbound_ShouldCreateNewArtifactAndPreservePredecessorRelationship()
    {
        await using var fixture = await Fixture.CreateAsync();
        var predecessor = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.Failed, [6]);
        predecessor.AchCycleId = "ACH-1";
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Service.ExecuteOutboundAsync("ACH-1", AchManagedFileExecutionOrigin.Manual,
            "operator", "corrected", default, predecessor.Id);

        Assert.Equal(1, result.Succeeded);
        var replacement = await fixture.Context.AchManagedFileTransfers.SingleAsync(x => x.CorrectedFromTransferId == predecessor.Id);
        Assert.NotEqual(predecessor.Id, replacement.Id);
        Assert.Equal(AchManagedFileTransferStatus.Transferred, replacement.Status);
    }

    [Fact]
    public async Task OutboundPublication_ShouldExposeBoundaryEvidenceWithoutClaimingAchColombiaAcceptance()
    {
        await using var fixture = await Fixture.CreateAsync();

        var result = await fixture.Service.ExecuteOutboundAsync(
            "ACH-1", AchManagedFileExecutionOrigin.Manual, "operator", "phase6-publication");
        var detail = await fixture.Service.GetAsync(Assert.Single(result.TransferIds));

        Assert.NotNull(detail);
        Assert.Equal(AchManagedFileTransferStatus.Transferred, detail.Status);
        Assert.Equal("Publicado para envío", detail.StatusLabel);
        Assert.Equal(AchManagedFileTransferPresentation.ExternalNotConfirmed, detail.ExternalConfirmationStatus);
        Assert.Equal("Recepción por ACH Colombia no confirmada.", detail.ExternalConfirmationMessage);
        Assert.Contains(detail.History, x => x.EventType == "Generated");
        Assert.Contains(detail.History, x => x.EventType == "Encrypted");
        Assert.Contains(detail.History, x => x.EventType == "PublicationStarted");
        Assert.Contains(detail.History, x => x.EventType == "PublishedToMftBoundary"
            && x.DisplayMessage.Contains("no está confirmada", StringComparison.OrdinalIgnoreCase));

        var json = System.Text.Json.JsonSerializer.Serialize(detail);
        Assert.DoesNotContain("Aceptado por ACH Colombia", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Recibido por ACH Colombia", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Monitoring_ShouldSanitizeStorageReferences_AndExposeHashSizeCycleAndOperationalDate()
    {
        await using var fixture = await Fixture.CreateAsync();
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.Transferred, [4, 5, 6]);
        transfer.AchCycleId = "ACH-1";
        transfer.ActiveStorageReference = "/srv/secret/mft/outbound/file.env";
        transfer.ArchiveReference = @"C:\secret\archive\file.env";
        await fixture.Context.SaveChangesAsync();

        var detail = await fixture.Service.GetAsync(transfer.Id);

        Assert.NotNull(detail);
        Assert.Equal("ACHCOL", detail.ClearingHouseCode);
        Assert.Equal("ACH Colombia", detail.ClearingHouseName);
        Assert.Equal("ACH-1", detail.CycleId);
        Assert.Equal(new DateTime(2026, 8, 31), detail.OperationalDate);
        Assert.Equal(3, detail.FileSize);
        Assert.Equal(Hash([4, 5, 6]), detail.ContentSha256);
        Assert.Equal("Referencia administrada: file.env", detail.SanitizedActiveReference);
        Assert.Equal("Archivado: file.env", detail.SanitizedArchiveReference);
        Assert.DoesNotContain("/srv/secret", System.Text.Json.JsonSerializer.Serialize(detail), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(@"C:\secret", System.Text.Json.JsonSerializer.Serialize(detail), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UncertainRetryPendingAndFailed_ShouldHaveDistinctUserSafePresentation()
    {
        await using var fixture = await Fixture.CreateAsync();
        var transfer = fixture.SeedTransfer(AchManagedFileDirection.Outbound, AchManagedFileTransferStatus.RetryPending, [3]);
        await fixture.Context.SaveChangesAsync();

        fixture.Adapter.OutboundResult = new(false, true, true, "TIMEOUT", "Resultado no confirmado.", null);
        var uncertain = await fixture.Service.RetryAsync(transfer.Id, "operator", "phase6-uncertain");
        Assert.Equal("Pendiente de confirmación", uncertain.StatusLabel);
        Assert.Contains(uncertain.History, x => x.EventType == "PublicationUncertain");

        transfer = await fixture.Context.AchManagedFileTransfers.Include(x => x.Events).SingleAsync(x => x.Id == transfer.Id);
        transfer.Status = AchManagedFileTransferStatus.RetryPending;
        fixture.Adapter.OutboundResult = new(false, true, false, "TEMP", "Falla temporal.", null);
        await fixture.Context.SaveChangesAsync();
        var pending = await fixture.Service.RetryAsync(transfer.Id, "operator", "phase6-retry-pending");
        Assert.Equal(AchManagedFileTransferStatus.RetryPending, pending.Status);
        Assert.Equal("Reintento pendiente", pending.StatusLabel);
        Assert.Contains(pending.History, x => x.EventType == "RetryPending");

        transfer = await fixture.Context.AchManagedFileTransfers.Include(x => x.Events).SingleAsync(x => x.Id == transfer.Id);
        transfer.Status = AchManagedFileTransferStatus.RetryPending;
        fixture.Adapter.OutboundResult = new(false, false, false, "REJECTED", "No publicable.", null);
        await fixture.Context.SaveChangesAsync();
        var failed = await fixture.Service.RetryAsync(transfer.Id, "operator", "phase6-failed");
        Assert.Equal(AchManagedFileTransferStatus.Failed, failed.Status);
        Assert.Equal("No se pudo publicar", failed.StatusLabel);
        Assert.Equal(AchManagedFileTransferPresentation.ExternalNotApplicable, failed.ExternalConfirmationStatus);
        Assert.Null(failed.ExternalConfirmationMessage);
        Assert.Contains(failed.History, x => x.EventType == "PublicationFailed");
    }

    [Fact]
    public async Task Inbound_ShouldRecordReceiptFromCfaBoundary_AndProcessingOutcome()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Adapter.Artifacts = [new("received.OUT.env", [9, 8], Hash([9, 8]), "/sensitive/inbound/.received.OUT.env.claim")];

        var execution = await fixture.Service.ExecuteInboundAsync(
            AchManagedFileExecutionOrigin.Manual, "operator", "phase6-inbound");
        var detail = await fixture.Service.GetAsync(Assert.Single(execution.TransferIds));

        Assert.NotNull(detail);
        Assert.Equal(AchManagedFileTransferStatus.Processed, detail.Status);
        Assert.Equal("Procesado", detail.StatusLabel);
        Assert.Equal(AchManagedFileTransferPresentation.ExternalNotApplicable, detail.ExternalConfirmationStatus);
        Assert.Null(detail.ExternalConfirmationMessage);
        Assert.Contains(detail.History, x => x.EventType == "ReceivedFromMftBoundary");
        Assert.Contains(detail.History, x => x.EventType == "Processed");
        Assert.Equal(detail.History.OrderBy(x => x.OccurredAtUtc).ThenBy(x => x.Id).Select(x => x.Id).ToArray(), detail.History.Select(x => x.Id).ToArray());
    }

    [Fact]
    public async Task InboundDuplicate_ShouldExposeDuplicateEventWithoutCreatingSecondFunctionalTransfer()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Adapter.Artifacts = [new("first.OUT.env", [7], Hash([7]), "claim-1")];
        await fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, "operator", "phase6-first");
        fixture.Adapter.Artifacts = [new("again.OUT.env", [7], Hash([7]), "claim-2")];

        var second = await fixture.Service.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, "operator", "phase6-second");
        var detail = await fixture.Service.GetAsync(Assert.Single(second.TransferIds));

        Assert.Equal(1, await fixture.Context.AchManagedFileTransfers.CountAsync());
        Assert.NotNull(detail);
        Assert.Contains(detail.History, x => x.EventType == "Duplicate" && x.DisplayName == "Duplicado");
    }

    private static string Hash(byte[] bytes) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes));

    private sealed class Fixture : IAsyncDisposable
    {
        public required AchDbContext Context { get; init; }
        public required Mock<INachaFileBuilder> Builder { get; init; }
        public required Mock<IIncomingNachaIngestionAppService> Ingestion { get; init; }
        public required StubAdapter Adapter { get; init; }
        public required AchManagedFileTransferConfiguration Configuration { get; init; }
        public required AchColombiaManagedFileExchangeService Service { get; init; }
        public required DateTimeOffset NowUtc { get; init; }

        public static async Task<Fixture> CreateAsync(DateTimeOffset? currentInstant = null)
        {
            var options = new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase($"achcol-mft-{Guid.NewGuid():N}").Options;
            var context = new AchDbContext(options);
            context.ClearingHouseConfigs.Add(new ClearingHouseConfig
            {
                Id = 1,
                ClearingHouseId = 1,
                TimeZoneId = "America/Bogota"
            });
            var chamber = new ClearingHouse { Id = 1, ClearingHouseId = 1, Code = "ACHCOL", Name = "ACH Colombia", OriginCode = "0001001" };
            context.ClearingHouses.Add(chamber);
            context.AchCycles.Add(new AchCycle
            {
                Id = "ACH-1",
                CycleName = "Ciclo 1",
                ProcessingDate = new(2026, 8, 31),
                StartTime = new(19, 1, 0),
                EndTime = new(8, 30, 0),
                CutoffTime = new(8, 30, 0),
                OutputReleaseTime = new(9, 30, 0),
                ClearingHouseId = 1
            });
            var configuration = new AchManagedFileTransferConfiguration { ClearingHouseId = 1, ProfileEnabled = true, ManualOutboundAllowed = true, ManualInboundAllowed = true };
            context.AchManagedFileTransferConfigurations.Add(configuration);
            await context.SaveChangesAsync();
            var builder = new Mock<INachaFileBuilder>();
            builder.Setup(x => x.BuildNachaFileArtifactByCycleAsync("ACH-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new NachaFileBuildArtifact(new string('1', 106), []));
            var naming = new Mock<IExternalFileNamePolicy>();
            naming.Setup(x => x.GenerateExternalNameAsync(It.IsAny<ExternalFileNameContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ExternalFileNameContext value, CancellationToken _) => new ExternalFileNamePolicyResult
                {
                    ExternalFileName = value.IdempotencyKey?.Contains("CORRECTED_FROM:NONE", StringComparison.Ordinal) == true
                        ? "0001001.001.20260831.1.OUT" : "0001001.001.20260831.2.OUT",
                    Components = new ExternalFileNameComponents()
                });
            var instant = currentInstant ?? new DateTimeOffset(2026, 8, 31, 13, 0, 0, TimeSpan.Zero);
            var clock = new FixedTimeProvider(instant, TimeZoneInfo.Utc);
            var bogotaLocal = DateTime.SpecifyKind(instant.UtcDateTime.AddHours(-5), DateTimeKind.Unspecified);
            var time = new Mock<IOperationalTimeSnapshotProvider>();
            time.Setup(x => x.CaptureNow())
                .Returns(new OperationalTimeSnapshot(instant.UtcDateTime, bogotaLocal, DateOnly.FromDateTime(bogotaLocal), "America/Bogota"));
            time.Setup(x => x.GetOrCreate(It.IsAny<string>(), It.IsAny<DateOnly>(), It.IsAny<TimeOnly>()))
                .Returns(new OperationalTimeSnapshot(instant.UtcDateTime, new(2026, 8, 31), new(2026, 8, 31), "America/Bogota"));
            var envelope = new Mock<INachaExportDigitalEnvelopeService>();
            envelope.Setup(x => x.EncryptAsync(1, It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((int _, string fileName, byte[] _, string _, CancellationToken _) =>
                    new ManagedDigitalEnvelopeResult(Encoding.ASCII.GetBytes($"protected:{fileName}"), $"{fileName}.env", "application/octet-stream", 1, "thumb", "test"));
            var ingestion = new Mock<IIncomingNachaIngestionAppService>();
            ingestion.Setup(x => x.IngestAsync(It.IsAny<IncomingNachaIngestionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IncomingNachaIngestionRequest request, CancellationToken _) => new IncomingNachaIngestionResponse { IngestionId = Guid.NewGuid(), OriginalFileName = request.FileName, IngestionStatus = IncomingNachaIngestionStatus.Completado, ParsingStatus = IncomingNachaParsingStatus.Exitoso, OperationalDate = new(2026, 8, 31) });
            var adapter = new StubAdapter();
            var encryption = new Mock<Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces.IEncryptionService>();
            encryption.Setup(x => x.Encrypt("super-secret")).Returns("protected:super-secret");
            var mftOptions = Options.Create(new AchColombiaManagedMftOptions
                { Enabled = true, OutboundPath = "out", InboundPath = "in", ProcessingPath = "processing", ArchivePath = "archive", MaximumFileBytes = 99 });
            var resolver = new StubResolver(adapter);
            var administration = new AchColombiaManagedMftAdministrationComponent(context, mftOptions);
            var queryProjection = new AchColombiaManagedMftQueryProjectionComponent(context);
            var lifecycle = new AchColombiaManagedMftLifecycleComponent(context, queryProjection);
            var outboundWorkflow = new AchColombiaManagedMftOutboundWorkflow(
                context, builder.Object, naming.Object, time.Object, envelope.Object, new AchFileExportAuditService(context),
                resolver, administration, new OperationalCycleWindowResolver());
            var inboundWorkflow = new AchColombiaManagedMftInboundWorkflow(
                context, ingestion.Object, resolver, administration, lifecycle, new OperationalCycleWindowResolver(), clock);
            var retryReprocess = new AchColombiaManagedMftRetryReprocessComponent(
                context, resolver, administration, outboundWorkflow, inboundWorkflow, queryProjection);
            var credentials = new AchColombiaManagedMftCredentialComponent(context, administration, encryption.Object);
            var service = new AchColombiaManagedFileExchangeService(
                outboundWorkflow, inboundWorkflow, retryReprocess, lifecycle, queryProjection, administration, credentials,
                Mock.Of<IAchColombiaManagedMftConnectionTester>());
            return new() { Context = context, Builder = builder, Ingestion = ingestion, Adapter = adapter, Configuration = configuration, Service = service, NowUtc = instant };
        }

        public AchManagedFileTransfer SeedTransfer(AchManagedFileDirection direction, AchManagedFileTransferStatus status, byte[] content)
        {
            var transfer = new AchManagedFileTransfer { ClearingHouseId = 1, Direction = direction, LogicalFileIdentity = Guid.NewGuid().ToString("N"), PhysicalFileName = direction == AchManagedFileDirection.Inbound ? "file.OUT.env" : "file.env", ContentSha256 = Hash(content), FileSize = content.Length, RetainedContent = content, OperationalDate = new(2026, 8, 31), ExecutionOrigin = AchManagedFileExecutionOrigin.Manual, Status = status, CorrelationId = Guid.NewGuid().ToString("N"), IdempotencyKey = Guid.NewGuid().ToString("N") };
            Context.Add(transfer); return transfer;
        }
        public async ValueTask DisposeAsync() => await Context.DisposeAsync();
    }


    private sealed class StubResolver(IAchColombiaManagedMftAdapter adapter) : IAchColombiaManagedMftAdapterResolver
    {
        public Task<IAchColombiaManagedMftAdapter> ResolveAsync(CancellationToken ct = default) => Task.FromResult(adapter);
    }

    private sealed class StubAdapter : IAchColombiaManagedMftAdapter
    {
        public bool Enabled => true;
        public int OutboundHandoffCount { get; private set; }
        public int ArchiveCallCount { get; private set; }
        public Func<AchManagedMftArtifact, Task>? BeforeArchiveAsync { get; set; }
        public Exception? ArchiveException { get; set; }
        public IReadOnlyList<AchManagedMftArtifact> Artifacts { get; set; } = [];
        public AchManagedMftResult OutboundResult { get; set; } = new(true, false, false, "OK", "Entregado.", "out");
        public Task<AchManagedMftResult> HandoffOutboundAsync(string fileName, byte[] content, string contentSha256, CancellationToken ct = default)
        {
            OutboundHandoffCount++;
            return Task.FromResult(OutboundResult);
        }
        public Task<IReadOnlyList<AchManagedMftArtifact>> PickupInboundAsync(CancellationToken ct = default) => Task.FromResult(Artifacts);
        public async Task<string> ArchiveInboundAsync(AchManagedMftArtifact artifact, CancellationToken ct = default)
        {
            ArchiveCallCount++;
            if (BeforeArchiveAsync is not null) await BeforeArchiveAsync(artifact);
            if (ArchiveException is not null) throw ArchiveException;
            return $"archive:{artifact.FileName}";
        }
    }
}
