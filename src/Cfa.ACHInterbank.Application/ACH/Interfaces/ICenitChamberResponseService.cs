using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface ICenitChamberResponseService
{
    Task<CenitChamberResponseResult> ImportAsync(CenitChamberResponseImportCommand command, CancellationToken ct = default);
    Task<CenitChamberResponseResult?> GetAsync(Guid id, CancellationToken ct = default);
    Task<CenitChamberResponsePage> ListAsync(int page = 1, int pageSize = 50, CancellationToken ct = default);
}
