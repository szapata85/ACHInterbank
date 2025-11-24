using Cfa.ACHInterbank.Application.Services.Notifications.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.External.Notifications;

[Scoped]
public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetAsync(User user, string resetLink, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Password reset requested for {Email}. Link: {Link}", user.Email, resetLink);
        return Task.CompletedTask;
    }
}
