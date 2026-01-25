using System;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/users/password-rules")]
[Authorize]
public class PasswordRulesController : ControllerBase
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

    public PasswordRulesController(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet]
    public async Task<ActionResult<PasswordRulesDto>> GetRulesAsync(CancellationToken cancellationToken)
    {
        var rules = await _dbContext.PasswordRuleSettings
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (rules is null)
        {
            return Ok(DefaultRules);
        }

        return Ok(MapToDto(rules));
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPut]
    public async Task<ActionResult<PasswordRulesDto>> SaveRulesAsync(
        [FromBody] PasswordRulesDto request,
        CancellationToken cancellationToken)
    {
        var rules = await _dbContext.PasswordRuleSettings
            .OrderBy(r => r.Id)
            .FirstOrDefaultAsync(cancellationToken);

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

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(rules));
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

public record PasswordRulesDto
{
    public int MinLength { get; init; }
    public int MinUppercase { get; init; }
    public int MinNumbers { get; init; }
    public int MinSpecial { get; init; }
    public int? MaxSpecial { get; init; }
}
