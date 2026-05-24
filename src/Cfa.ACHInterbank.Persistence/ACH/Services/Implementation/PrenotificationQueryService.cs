using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class PrenotificationQueryService : IPrenotificationQueryService
{
    private readonly AchDbContext _context;
    private readonly IBankHoliday _holidayService;

    public PrenotificationQueryService(AchDbContext context, IBankHoliday holidayService)
    {
        _context = context;
        _holidayService = holidayService;
    }

    public async Task<PrenotificationStatusDto?> GetByReferenceAsync(string reference, CancellationToken ct = default)
    {
        var normalized = (reference ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var transaction = await BuildBaseQuery()
            .FirstOrDefaultAsync(x => x.Reference == normalized && x.IsPrenotification, ct);

        return transaction is null ? null : Map(transaction);
    }

    public async Task<PrenotificationStatusDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var transaction = await BuildBaseQuery()
            .FirstOrDefaultAsync(x => x.Id == id && x.IsPrenotification, ct);

        return transaction is null ? null : Map(transaction);
    }

    private IQueryable<AchTransaction> BuildBaseQuery()
        => _context.AchTransactions
            .AsNoTracking()
            .Include(x => x.SourceInstitution)
            .Include(x => x.AchCycle)
                .ThenInclude(x => x.ClearingHouse)
            .Include(x => x.StateEvents);

    private PrenotificationStatusDto Map(AchTransaction transaction)
    {
        var maturityDate = AddBusinessDays(transaction.EffectiveEntryDate.Date, 3);
        var approvedAt = transaction.StateEvents
            .Where(x => x.ToState is AchTransferStateEnum.AppliedTacitly or AchTransferStateEnum.Certified)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => (DateTimeOffset?)x.CreatedAt)
            .FirstOrDefault();

        bool isApproved = transaction.State is AchTransferStateEnum.AppliedTacitly or AchTransferStateEnum.Certified;
        bool isMatured = isApproved && DateTime.UtcNow.Date >= maturityDate.Date;
        bool canBeUsedForDebit = isApproved && isMatured;

        return new PrenotificationStatusDto
        {
            Id = transaction.Id,
            Reference = transaction.Reference,
            ClearingHouse = transaction.AchCycle.ClearingHouse?.Name ?? string.Empty,
            SourceFinancialInstitution = transaction.SourceInstitution?.Name ?? string.Empty,
            SourceIsDefault = transaction.SourceInstitution?.IsDefaultSource ?? false,
            TransactionId = transaction.Id,
            NachaCode = transaction.TransactionCode,
            Status = transaction.State.ToString(),
            StatusDescription = ToSpanishStatus(transaction.State),
            EffectiveDate = transaction.EffectiveEntryDate.Date,
            ApprovedAt = approvedAt,
            MaturityDate = maturityDate,
            IsMatured = isMatured,
            CanBeUsedForDebit = canBeUsedForDebit,
            Message = BuildMessage(transaction.State, isMatured)
        };
    }

    private DateTime AddBusinessDays(DateTime start, int days)
    {
        var current = start;
        var remaining = days;
        var currentYear = current.Year;
        var holidays = _holidayService.GetHolidays(currentYear)
            .Select(x => x.Date)
            .ToHashSet();

        while (remaining > 0)
        {
            current = current.AddDays(1);
            if (current.Year != currentYear)
            {
                currentYear = current.Year;
                holidays = _holidayService.GetHolidays(currentYear)
                    .Select(x => x.Date)
                    .ToHashSet();
            }

            var dateOnly = DateOnly.FromDateTime(current);
            if (current.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday) && !holidays.Contains(dateOnly))
            {
                remaining--;
            }
        }

        return current.Date;
    }

    private static string ToSpanishStatus(AchTransferStateEnum state)
        => state switch
        {
            AchTransferStateEnum.Pending => "Pendiente",
            AchTransferStateEnum.AppliedTacitly => "Aprobada",
            AchTransferStateEnum.Certified => "Certificada",
            AchTransferStateEnum.ReturnedByOperator => "Rechazada por operador",
            AchTransferStateEnum.ReturnedByEpr => "Rechazada por EPR",
            _ => "Estado no clasificado"
        };

    private static string BuildMessage(AchTransferStateEnum state, bool isMatured)
        => state switch
        {
            AchTransferStateEnum.Pending => "La prenotificacion esta pendiente. Puede exportarse como prenotificacion, pero aun no habilita debito monetario posterior.",
            AchTransferStateEnum.AppliedTacitly or AchTransferStateEnum.Certified when !isMatured => "La prenotificacion fue aprobada, pero aun no cumple los dias habiles requeridos para debito monetario.",
            AchTransferStateEnum.AppliedTacitly or AchTransferStateEnum.Certified => "La prenotificacion esta madura y puede usarse para debito monetario posterior.",
            AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr => "La prenotificacion fue rechazada y no puede usarse para debito monetario posterior.",
            _ => "La prenotificacion requiere revision operativa."
        };
}
