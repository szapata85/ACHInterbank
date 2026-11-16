using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>Dedicated watchdog: scans expired Axon InFlight leases even without new notifications.</summary>
public sealed class AxonNotificationRecoveryHostedService(
    IServiceScopeFactory scopeFactory, ILogger<AxonNotificationRecoveryHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var claimStore = scope.ServiceProvider.GetRequiredService<IAxonNotificationClaimStore>();
                await claimStore.QuarantineExpiredAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Axon expired-claim quarantine failed; no SOAP replay attempted.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
