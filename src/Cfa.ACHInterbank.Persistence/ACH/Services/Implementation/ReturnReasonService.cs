using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.ACH.Dtos;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class ReturnReasonService(AchDbContext context) : IReturnReasonService
{
    private readonly AchDbContext _context = context;

    public async Task<IEnumerable<ReturnReasonDto>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _context.ReturnReasons
            .AsNoTracking()
            .OrderBy(reason => reason.Code)
            .ToListAsync(ct);

        return items.Select(reason => new ReturnReasonDto
        {
            Id = reason.Id,
            Code = reason.Code,
            Description = reason.Description,
            Category = reason.Category
        });
    }
}
