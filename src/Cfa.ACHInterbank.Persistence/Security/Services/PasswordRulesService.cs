using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Security.Services;

[Scoped]
public class PasswordRulesService : IPasswordRulesService
{
    private static readonly PasswordRulesDto DefaultRules = new()
    {
        MinLength = 6,
        MinUppercase = 1,
        MinNumbers = 1,
        MinSpecial = 1,
        MaxSpecial = 4
    };

    private readonly AchDbContext _dbContext;

    public PasswordRulesService(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PasswordRulesDto> GetAsync(CancellationToken ct = default)
    {
        var rules = await _dbContext.PasswordRuleSettings
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync(ct);

        return rules is null ? DefaultRules : MapToDto(rules);
    }

    public async Task<PasswordRulesDto> SaveAsync(PasswordRulesDto request, CancellationToken ct = default)
    {
        var rules = await _dbContext.PasswordRuleSettings
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync(ct);

        if (rules is null)
        {
            rules = new PasswordRuleSetting();
            _dbContext.PasswordRuleSettings.Add(rules);
        }

        var normalized = NormalizeRules(request);
        rules.MinLength = normalized.MinLength;
        rules.MinUppercase = normalized.MinUppercase;
        rules.MinNumbers = normalized.MinNumbers;
        rules.MinSpecial = normalized.MinSpecial;
        rules.MaxSpecial = normalized.MaxSpecial;

        await _dbContext.SaveChangesAsync(ct);

        return MapToDto(rules);
    }

    private static PasswordRulesDto NormalizeRules(PasswordRulesDto request) => new()
    {
        MinLength = Math.Max(1, request.MinLength),
        MinUppercase = Math.Max(0, request.MinUppercase),
        MinNumbers = Math.Max(0, request.MinNumbers),
        MinSpecial = Math.Max(0, request.MinSpecial),
        MaxSpecial = request.MaxSpecial.HasValue ? Math.Max(0, request.MaxSpecial.Value) : null
    };

    private static PasswordRulesDto MapToDto(PasswordRuleSetting entity) => new()
    {
        MinLength = entity.MinLength,
        MinUppercase = entity.MinUppercase,
        MinNumbers = entity.MinNumbers,
        MinSpecial = entity.MinSpecial,
        MaxSpecial = entity.MaxSpecial
    };
}
