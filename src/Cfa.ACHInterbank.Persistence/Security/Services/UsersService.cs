using Cfa.ACHInterbank.Application.Common;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Cfa.ACHInterbank.Persistence.Security.Services;

[Scoped]
public class UsersService : IUsersService
{
    private readonly AchDbContext _dbContext;

    public UsersService(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResponse<UserSummaryDto>> GetUsersAsync(UserQueryRequest request, CancellationToken ct = default)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 10 : request.PageSize;

        var query = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .AsSplitQuery()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            query = query.Where(u =>
                (u.Username != null && u.Username.Contains(request.Search)) ||
                (u.FullName != null && u.FullName.Contains(request.Search)) ||
                (u.Email != null && u.Email.Contains(request.Search)));
        }

        if (request.RoleId.HasValue)
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.RoleId == request.RoleId.Value));
        }

        var total = await query.CountAsync(ct);

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
            .ToListAsync(ct);

        return new PagedResponse<UserSummaryDto>
        {
            Items = items,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> ValidateEmailDomainAsync(string email, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        var atIndex = email.LastIndexOf('@');
        if (atIndex <= 0 || atIndex >= email.Length - 1)
        {
            return false;
        }

        var domain = email[(atIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(domain))
        {
            return false;
        }

        try
        {
            var asciiDomain = new IdnMapping().GetAscii(domain);
            var hostEntry = await Dns.GetHostEntryAsync(asciiDomain).WaitAsync(ct);
            return hostEntry.AddressList.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserSummaryDto?> GetUserAsync(Guid id, CancellationToken ct = default)
    {
        return await BuildUserQuery()
            .FirstOrDefaultAsync(u => u.Id == id, ct);
    }

    public async Task<UserOperationResult> CreateAsync(SaveUserRequest? request, CancellationToken ct = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
        {
            return UserOperationResult.ValidationError("El usuario y la contraseña son obligatorios.");
        }

        var usernameExists = await _dbContext.Users
            .AnyAsync(u => u.Username == request.UserName, ct);

        if (usernameExists)
        {
            return UserOperationResult.Conflict($"Ya existe un usuario con el nombre {request.UserName}.");
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

        await UpdateUserRolesAsync(user, request.RoleIds, ct);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(ct);

        var created = await BuildUserQuery()
            .FirstAsync(u => u.Id == user.Id, ct);

        return UserOperationResult.Success(created);
    }

    public async Task<UserOperationResult> UpdateAsync(Guid id, SaveUserRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return UserOperationResult.ValidationError("El cuerpo de la solicitud no puede estar vacío.");
        }

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
        {
            return UserOperationResult.NotFound();
        }

        if (!string.IsNullOrWhiteSpace(request.UserName) && request.UserName != user.Username)
        {
            var usernameExists = await _dbContext.Users
                .AnyAsync(u => u.Username == request.UserName && u.Id != id, ct);

            if (usernameExists)
            {
                return UserOperationResult.Conflict($"Ya existe un usuario con el nombre {request.UserName}.");
            }

            user.Username = request.UserName;
        }

        user.FullName = request.FullName;
        user.Email = request.Email;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = BuildHash(request.Password);
        }

        await UpdateUserRolesAsync(user, request.RoleIds, ct);

        await _dbContext.SaveChangesAsync(ct);

        var updated = await BuildUserQuery()
            .FirstAsync(u => u.Id == id, ct);

        return UserOperationResult.Success(updated);
    }

    public async Task<UserOperationResult> AssignRolesAsync(Guid id, AssignRolesRequest? request, CancellationToken ct = default)
    {
        if (request is null)
        {
            return UserOperationResult.ValidationError("El cuerpo de la solicitud no puede estar vacío.");
        }

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
        {
            return UserOperationResult.NotFound();
        }

        await UpdateUserRolesAsync(user, request.RoleIds, ct);
        await _dbContext.SaveChangesAsync(ct);

        return UserOperationResult.Success();
    }

    public async Task<UserOperationStatus> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

        if (user is null)
        {
            return UserOperationStatus.NotFound;
        }

        user.IsActive = false;
        await _dbContext.SaveChangesAsync(ct);

        return UserOperationStatus.Success;
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

    private async Task UpdateUserRolesAsync(User user, IEnumerable<Guid>? roleIds, CancellationToken ct)
    {
        if (roleIds is null)
        {
            return;
        }

        var validRoleIds = await _dbContext.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Id)
            .ToListAsync(ct);

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
