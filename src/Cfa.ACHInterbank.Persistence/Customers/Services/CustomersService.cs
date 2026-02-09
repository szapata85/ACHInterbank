using Cfa.ACHInterbank.Application.Customers.Dtos;
using Cfa.ACHInterbank.Application.Customers.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Customers.Services;

[Scoped]
public class CustomersService : ICustomersService
{
    private readonly AchDbContext _context;

    public CustomersService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CustomerSummaryDto>> GetAllAsync(CancellationToken ct = default)
    {
        var customers = await _context.Customers
            .AsNoTracking()
            .Include(c => c.Accounts)
            .OrderBy(c => c.FirstName)
            .ThenBy(c => c.MiddleName)
            .ThenBy(c => c.LastName)
            .ThenBy(c => c.SecondLastName)
            .ToListAsync(ct);

        return customers.Select(c =>
        {
            var accountNumbers = c.Accounts
                .Select(a => a.AccountNumber)
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .OrderBy(a => a)
                .ToList();

            return new CustomerSummaryDto
            {
                Id = c.Id,
                DocumentType = c.DocumentType,
                DocumentNumber = c.DocumentNumber,
                AccountNumber = accountNumbers.FirstOrDefault() ?? string.Empty,
                AccountNumbers = accountNumbers,
                PersonType = c.PersonType,
                CompanyName = c.CompanyName,
                FullName = string.Join(" ", new[]
                {
                    c.FirstName,
                    c.MiddleName,
                    c.LastName,
                    c.SecondLastName
                }.Where(part => !string.IsNullOrWhiteSpace(part)))
            };
        });
    }

    public async Task<CustomerDetailDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        return customer is null ? null : Map(customer);
    }

    public async Task<CustomerDetailDto> CreateAsync(SaveCustomerRequest request, CancellationToken ct = default)
    {
        var accountNumbers = NormalizeAccountNumbers(request);
        var validation = ValidateRequest(request, accountNumbers);
        if (!string.IsNullOrWhiteSpace(validation))
        {
            throw new ArgumentException(validation);
        }

        var customer = new Customer
        {
            FirstName = request.FirstName.Trim(),
            MiddleName = request.MiddleName?.Trim(),
            LastName = request.LastName.Trim(),
            SecondLastName = request.SecondLastName?.Trim(),
            Gender = request.Gender?.Trim(),
            PersonType = request.PersonType.Trim(),
            CompanyName = request.CompanyName?.Trim(),
            DocumentType = request.DocumentType.Trim(),
            DocumentNumber = request.DocumentNumber.Trim(),
            Accounts = accountNumbers.Select(accountNumber => new CustomerAccount
            {
                AccountNumber = accountNumber
            }).ToList()
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(ct);

        return Map(customer);
    }

    public async Task<CustomerDetailDto?> UpdateAsync(int id, SaveCustomerRequest request, CancellationToken ct = default)
    {
        var accountNumbers = NormalizeAccountNumbers(request);
        var validation = ValidateRequest(request, accountNumbers);
        if (!string.IsNullOrWhiteSpace(validation))
        {
            throw new ArgumentException(validation);
        }

        var customer = await _context.Customers
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (customer is null)
        {
            return null;
        }

        customer.FirstName = request.FirstName.Trim();
        customer.MiddleName = request.MiddleName?.Trim();
        customer.LastName = request.LastName.Trim();
        customer.SecondLastName = request.SecondLastName?.Trim();
        customer.Gender = request.Gender?.Trim();
        customer.PersonType = request.PersonType.Trim();
        customer.CompanyName = request.CompanyName?.Trim();
        customer.DocumentType = request.DocumentType.Trim();
        customer.DocumentNumber = request.DocumentNumber.Trim();

        customer.Accounts.Clear();
        foreach (var accountNumber in accountNumbers)
        {
            customer.Accounts.Add(new CustomerAccount { AccountNumber = accountNumber });
        }

        await _context.SaveChangesAsync(ct);

        return Map(customer);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (customer is null)
        {
            return false;
        }

        _context.Customers.Remove(customer);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    private static CustomerDetailDto Map(Customer customer)
    {
        var accounts = customer.Accounts
            .Select(a => a.AccountNumber)
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .OrderBy(a => a)
            .ToList();

        return new CustomerDetailDto
        {
            Id = customer.Id,
            FirstName = customer.FirstName,
            MiddleName = customer.MiddleName,
            LastName = customer.LastName,
            SecondLastName = customer.SecondLastName,
            Gender = customer.Gender,
            PersonType = customer.PersonType,
            CompanyName = customer.CompanyName,
            DocumentType = customer.DocumentType,
            DocumentNumber = customer.DocumentNumber,
            AccountNumber = accounts.FirstOrDefault() ?? string.Empty,
            AccountNumbers = accounts
        };
    }

    private static List<string> NormalizeAccountNumbers(SaveCustomerRequest request)
    {
        var accounts = request.AccountNumbers ?? [];
        var normalized = accounts
            .Select(a => a?.Trim() ?? string.Empty)
            .Where(a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalized.Count == 0 && !string.IsNullOrWhiteSpace(request.AccountNumber))
        {
            normalized.Add(request.AccountNumber.Trim());
        }

        return normalized;
    }

    private static string? ValidateRequest(SaveCustomerRequest request, IReadOnlyCollection<string> accountNumbers)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentType))
        {
            return "El tipo de documento es obligatorio.";
        }

        if (string.IsNullOrWhiteSpace(request.DocumentNumber))
        {
            return "El número de documento es obligatorio.";
        }

        if (accountNumbers.Count == 0)
        {
            return "Debe registrar al menos un número de cuenta.";
        }

        if (string.IsNullOrWhiteSpace(request.PersonType))
        {
            return "El tipo de persona es obligatorio.";
        }

        var personType = request.PersonType.Trim().ToUpperInvariant();
        if (personType == "PJ" && string.IsNullOrWhiteSpace(request.CompanyName))
        {
            return "La razón social es obligatoria para persona jurídica.";
        }

        if (personType == "PN" && string.IsNullOrWhiteSpace(request.FirstName))
        {
            return "El nombre es obligatorio para persona natural.";
        }

        if (personType == "PN" && string.IsNullOrWhiteSpace(request.LastName))
        {
            return "El apellido es obligatorio para persona natural.";
        }

        return null;
    }
}
