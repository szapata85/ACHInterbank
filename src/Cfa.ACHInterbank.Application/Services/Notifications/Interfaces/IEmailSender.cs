using Cfa.ACHInterbank.Domain.Entities.User;

namespace Cfa.ACHInterbank.Application.Services.Notifications.Interfaces;

public interface IEmailSender
{
    Task SendPasswordResetAsync(User user, string resetLink, CancellationToken cancellationToken = default);
}
