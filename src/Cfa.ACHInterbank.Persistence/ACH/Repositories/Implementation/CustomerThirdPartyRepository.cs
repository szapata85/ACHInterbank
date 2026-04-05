using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;

[Scoped]
public class CustomerThirdPartyRepository : ICustomerThirdPartyRepository
{
    private readonly AchDbContext _context;

    public CustomerThirdPartyRepository(AchDbContext context)
    {
        _context = context;
    }

    public Task<CustomerThirdParty?> FindAsync(int customerId, int destinationInstitutionId, string destinationAccountNumber, string recipientIdNumber, CancellationToken ct = default)
    {
        return _context.CustomerThirdParties
            .FirstOrDefaultAsync(t =>
                t.CustomerId == customerId &&
                t.DestinationInstitutionId == destinationInstitutionId &&
                t.DestinationAccountNumber == destinationAccountNumber &&
                t.RecipientIdNumber == recipientIdNumber, ct);
    }

    public Task AddAsync(CustomerThirdParty entity, CancellationToken ct = default)
    {
        _context.CustomerThirdParties.Add(entity);
        return Task.CompletedTask;
    }
}
