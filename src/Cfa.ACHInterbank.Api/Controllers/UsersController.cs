using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AchDbContext _dbContext;

    public UsersController(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet]
    public async Task<ActionResult<PagedResponse<UserSummaryDto>>> GetUsersAsync(
        [FromQuery] string? search,
        [FromQuery] Guid? roleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (page <= 0)
        {
            page = 1;
        }

        if (pageSize <= 0)
        {
            pageSize = 10;
        }

        var query = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                (u.Username != null && u.Username.Contains(search)) ||
                (u.FullName != null && u.FullName.Contains(search)) ||
                (u.Email != null && u.Email.Contains(search)));
        }

        if (roleId.HasValue)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == roleId.Value));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                UserName = u.Username ?? string.Empty,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = string.Empty,
                Roles = u.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => new RoleSummaryDto
                    {
                        Id = ur.RoleId,
                        Name = ur.Role!.Name ?? string.Empty,
                        Description = ur.Role!.Description,
                        Permissions = Enumerable.Empty<string>()
                    })
                    .ToList(),
                IsActive = u.IsActive
            })
            .ToListAsync(cancellationToken);

        var response = new PagedResponse<UserSummaryDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };

        return Ok(response);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet("validate-email-domain")]
    public async Task<ActionResult<bool>> ValidateEmailDomainAsync(
        [FromQuery] string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Ok(false);
        }

        var atIndex = email.LastIndexOf('@');
        if (atIndex <= 0 || atIndex >= email.Length - 1)
        {
            return Ok(false);
        }

        var domain = email[(atIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(domain))
        {
            return Ok(false);
        }

        try
        {
            var asciiDomain = new IdnMapping().GetAscii(domain);
            var hostEntry = await Dns.GetHostEntryAsync(asciiDomain).WaitAsync(cancellationToken);
            return Ok(hostEntry.AddressList.Length > 0);
        }
        catch
        {
            return Ok(false);
        }
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserSummaryDto>> GetUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await BuildUserQuery()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        return Ok(user);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost]
    public async Task<ActionResult<UserSummaryDto>> CreateUserAsync([FromBody] SaveUserRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("El usuario y la contraseña son obligatorios.");
        }

        var usernameExists = await _dbContext.Users
            .AnyAsync(u => u.Username == request.UserName, cancellationToken);

        if (usernameExists)
        {
            return Conflict($"Ya existe un usuario con el nombre {request.UserName}.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.UserName,
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BuildHash(request.Password),
            IsActive = true
        };

        await UpdateUserRolesAsync(user, request.RoleIds, cancellationToken);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var created = await BuildUserQuery()
            .FirstAsync(u => u.Id == user.Id, cancellationToken);

        return CreatedAtAction(nameof(GetUserAsync), new { id = created.Id }, created);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserSummaryDto>> UpdateUserAsync(Guid id, [FromBody] SaveUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.UserName) && request.UserName != user.Username)
        {
            var usernameExists = await _dbContext.Users
                .AnyAsync(u => u.Username == request.UserName && u.Id != id, cancellationToken);

            if (usernameExists)
            {
                return Conflict($"Ya existe un usuario con el nombre {request.UserName}.");
            }

            user.Username = request.UserName;
        }

        user.FullName = request.FullName;
        user.Email = request.Email;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BuildHash(request.Password);
        }

        await UpdateUserRolesAsync(user, request.RoleIds, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);

        var updated = await BuildUserQuery()
            .FirstAsync(u => u.Id == id, cancellationToken);

        return Ok(updated);
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpPost("{id:guid}/roles")]
    public async Task<IActionResult> AssignRolesAsync(Guid id, [FromBody] AssignRolesRequest request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        await UpdateUserRolesAsync(user, request.RoleIds, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
    /// <summary>
    /// Pendiente de documentación.
    /// </summary>

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeactivateUserAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private IQueryable<UserSummaryDto> BuildUserQuery()
    {
        return _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsSplitQuery()
            .Select(u => new UserSummaryDto
            {
                Id = u.Id,
                UserName = u.Username ?? string.Empty,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = string.Empty,
                Roles = u.UserRoles
                    .Where(ur => ur.Role != null)
                    .Select(ur => new RoleSummaryDto
                    {
                        Id = ur.RoleId,
                        Name = ur.Role!.Name ?? string.Empty,
                        Description = ur.Role!.Description,
                        Permissions = Enumerable.Empty<string>()
                    })
                    .ToList(),
                IsActive = u.IsActive
            });
    }

    private async Task UpdateUserRolesAsync(User user, IEnumerable<Guid>? roleIds, CancellationToken cancellationToken)
    {
        if (roleIds is null)
        {
            return;
        }

        var validRoleIds = await _dbContext.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        user.UserRoles.Clear();

        foreach (var roleId in validRoleIds)
        {
            user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = roleId });
        }
    }

    private static string BuildHash(string value)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(value);
        var hashBytes = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLowerInvariant();
    }
}

public record UserSummaryDto
{
    public Guid Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public IEnumerable<RoleSummaryDto> Roles { get; init; } = Enumerable.Empty<RoleSummaryDto>();
    public bool IsActive { get; init; }
}

public record PagedResponse<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

public record SaveUserRequest
{
    public string? UserName { get; init; }
    public string? FullName { get; init; }
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Password { get; init; }
    public IEnumerable<Guid>? RoleIds { get; init; }
}

public record AssignRolesRequest
{
    public IEnumerable<Guid>? RoleIds { get; init; }
}
