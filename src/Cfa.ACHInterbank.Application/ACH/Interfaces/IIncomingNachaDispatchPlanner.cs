using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IIncomingNachaDispatchPlanner
{
    Task<int> PlanForIngestionAsync(Guid ingestionId, string plannedBy, CancellationToken ct = default);
}
