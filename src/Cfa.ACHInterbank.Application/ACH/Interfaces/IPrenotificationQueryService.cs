using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IPrenotificationQueryService
{
    Task<PrenotificationStatusDto?> GetByReferenceAsync(string reference, CancellationToken ct = default);
    Task<PrenotificationStatusDto?> GetByIdAsync(int id, CancellationToken ct = default);
}
