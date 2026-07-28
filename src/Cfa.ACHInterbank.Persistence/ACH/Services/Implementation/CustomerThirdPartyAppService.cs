using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class CustomerThirdPartyAppService : ICustomerThirdPartyAppService
{
    private readonly AchDbContext _context;

    public CustomerThirdPartyAppService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<CustomerThirdPartyListDto>> GetAsync(CustomerThirdPartyQuery query, CancellationToken ct = default)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize <= 0 ? 20 : query.PageSize;

        var dataQuery = _context.CustomerThirdParties
            .AsNoTracking()
            .Include(t => t.Customer)
            .Include(t => t.DestinationInstitution)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            dataQuery = dataQuery.Where(t =>
                (t.Customer.FirstName + " " + t.Customer.LastName).Contains(term) ||
                (t.Customer.CompanyName ?? string.Empty).Contains(term) ||
                t.DestinationAccountNumber.Contains(term) ||
                t.RecipientIdNumber.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.DestinationAccountNumber))
        {
            var destinationAccount = query.DestinationAccountNumber.Trim();
            dataQuery = dataQuery.Where(t => t.DestinationAccountNumber.Contains(destinationAccount));
        }

        if (!string.IsNullOrWhiteSpace(query.RecipientIdNumber))
        {
            var recipientIdNumber = query.RecipientIdNumber.Trim();
            dataQuery = dataQuery.Where(t => t.RecipientIdNumber.Contains(recipientIdNumber));
        }

        if (query.DestinationInstitutionId.HasValue)
        {
            dataQuery = dataQuery.Where(t => t.DestinationInstitutionId == query.DestinationInstitutionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SourceAccountNumber))
        {
            var sourceAccountNumber = query.SourceAccountNumber.Trim();

            var sourceIdentity = await _context.Customers
                .AsNoTracking()
                .Where(c => c.Accounts.Any(a => a.AccountNumber == sourceAccountNumber))
                .Select(c => new { c.DocumentType, c.DocumentNumber })
                .FirstOrDefaultAsync(ct);

            if (sourceIdentity is null)
            {
                return new PagedResponse<CustomerThirdPartyListDto>
                {
                    Items = [],
                    Total = 0,
                    Page = page,
                    PageSize = pageSize
                };
            }

            dataQuery = dataQuery.Where(t =>
                t.Customer.DocumentType == sourceIdentity.DocumentType &&
                t.Customer.DocumentNumber == sourceIdentity.DocumentNumber);
        }

        if (query.Status.HasValue)
        {
            dataQuery = dataQuery.Where(t => t.Status == query.Status.Value);
        }

        var total = await dataQuery.CountAsync(ct);

        var rows = await dataQuery
            .OrderByDescending(t => t.ValidationReceivedAt)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id,
                t.CustomerId,
                t.Customer.CompanyName,
                t.Customer.FirstName,
                t.Customer.LastName,
                t.Customer.SecondLastName,
                t.DestinationInstitutionId,
                DestinationInstitutionName = t.DestinationInstitution.Name,
                t.DestinationAccountNumber,
                t.RecipientIdNumber,
                t.Status,
                t.PrenotificationTransactionId,
                t.ValidationCycleId,
                t.ValidationReceivedAt,
                t.ValidationMessage,
                ClearingHouseName = t.PrenotificationTransaction == null
                    ? null
                    : t.PrenotificationTransaction.AchCycle.ClearingHouse!.Name,
                ClearingHouseCode = t.PrenotificationTransaction == null
                    ? null
                    : t.PrenotificationTransaction.AchCycle.ClearingHouse!.Code,
                SendingCycleId = t.PrenotificationTransaction == null
                    ? null
                    : t.PrenotificationTransaction.AchCycleId,
                PrenotificationState = t.PrenotificationTransaction == null
                    ? null
                    : (AchTransferStateEnum?)t.PrenotificationTransaction.State,
                ResponseFileName = t.PrenotificationTransactionId == null
                    ? null
                    : _context.IncomingNachaTransactionLinks
                        .Where(link => link.AchTransactionId == t.PrenotificationTransactionId && link.IsFinal)
                        .OrderByDescending(link => link.LinkedAtUtc)
                        .Select(link => link.Ingestion.FileName)
                        .FirstOrDefault(),
                ResponseCorrelationId = t.PrenotificationTransactionId == null
                    ? null
                    : _context.IncomingNachaTransactionLinks
                        .Where(link => link.AchTransactionId == t.PrenotificationTransactionId && link.IsFinal)
                        .OrderByDescending(link => link.LinkedAtUtc)
                        .Select(link => link.Ingestion.CorrelationId)
                        .FirstOrDefault()
            })
            .ToListAsync(ct);

        var items = rows.Select(t => new CustomerThirdPartyListDto
        {
            Id = t.Id,
            CustomerId = t.CustomerId,
            CustomerName = string.IsNullOrWhiteSpace(t.CompanyName)
                ? string.Join(" ", new[] { t.FirstName, t.LastName, t.SecondLastName }.Where(x => !string.IsNullOrWhiteSpace(x)))
                : t.CompanyName!,
            DestinationInstitutionId = t.DestinationInstitutionId,
            DestinationInstitutionName = t.DestinationInstitutionName,
            DestinationAccountNumber = t.DestinationAccountNumber,
            RecipientIdNumber = t.RecipientIdNumber,
            Status = t.Status,
            PrenotificationTransactionId = t.PrenotificationTransactionId,
            ValidationCycleId = t.ValidationCycleId,
            ValidationReceivedAt = t.ValidationReceivedAt,
            ValidationMessage = t.ValidationMessage,
            ClearingHouseName = t.ClearingHouseName,
            ClearingHouseCode = t.ClearingHouseCode,
            SendingCycleId = t.SendingCycleId,
            ResponseFileName = t.ResponseFileName,
            ResponseCorrelationId = t.ResponseCorrelationId,
            StatusSource = t.PrenotificationState == AchTransferStateEnum.AppliedTacitly
                ? "Aceptación tácita automática"
                : t.ValidationReceivedAt.HasValue
                    ? "Procesador automático NACHA-M"
                    : "Pendiente de resolución automática"
        }).ToList();

        return new PagedResponse<CustomerThirdPartyListDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

}
