using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class CenitLocalGatewayPickupHostedService(
    ICenitGatewayTransportAdapter transport,
    IServiceScopeFactory scopeFactory,
    IOptions<CenitLocalGatewayOptions> options,
    ILogger<CenitLocalGatewayPickupHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!transport.Enabled) return;
        var delay = TimeSpan.FromMilliseconds(Math.Clamp(options.Value.PollIntervalMilliseconds, 100, 30_000));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                foreach (var artifact in await transport.PickupInboundAsync(stoppingToken))
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var service = scope.ServiceProvider.GetRequiredService<ICenitChamberResponseService>();
                    await service.ImportAsync(new CenitChamberResponseImportCommand(
                        artifact.SourceResponseId,
                        artifact.SourceFileName,
                        artifact.MessageType,
                        artifact.Content,
                        artifact.ReceivedAtUtc,
                        artifact.RelatedOutboundFileName,
                        artifact.RelatedReference,
                        artifact.TransactionTraceNumber,
                        artifact.AchCycleId), stoppingToken);
                    await transport.ArchiveInboundAsync(artifact, stoppingToken);
                    logger.LogInformation("CENIT_LOCAL_GATEWAY_ARTIFACT_PROCESSED SourceResponseId={SourceResponseId}", artifact.SourceResponseId);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "CENIT_LOCAL_GATEWAY_PICKUP_FAILED");
            }
            await Task.Delay(delay, stoppingToken);
        }
    }
}
