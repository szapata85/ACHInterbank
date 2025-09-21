using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaService : INachaService
{
    private readonly AchDbContext _context;

    public NachaService(AchDbContext context)
    {
        _context = context;
    }

    public async Task SaveHeaderAsync(NachaHeader header)
    {
        _context.NachaHeaders.Add(header);
        await _context.SaveChangesAsync();
    }
}
