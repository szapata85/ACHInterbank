using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services;

public class NachaServiceScoped : INachaServiceScoped
{
    private readonly NachaDbContext _context;

    public NachaServiceScoped(NachaDbContext context)
    {
        _context = context;
    }

    public async Task SaveHeaderAsync(NachaHeader header)
    {
        _context.NachaHeaders.Add(header);
        await _context.SaveChangesAsync();
    }
}
