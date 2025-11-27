using System.Security.Claims;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace Cfa.ACHInterbank.Application.Navigation.Queries;

public class GetMenuForCurrentUserHandler : IRequestHandler<GetMenuForCurrentUserQuery, IList<MenuItemDto>>
{
    private readonly IMenuQueryRepository _menuRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public GetMenuForCurrentUserHandler(IMenuQueryRepository menuRepository, IHttpContextAccessor httpContextAccessor)
    {
        _menuRepository = menuRepository;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<IList<MenuItemDto>> Handle(GetMenuForCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var principal = _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
        var roleClaimType = (principal.Identity as ClaimsIdentity)?.RoleClaimType ?? ClaimTypes.Role;
        var userRoles = principal.FindAll(roleClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var userPermissions = principal.FindAll("permission")
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var menuItems = await _menuRepository.GetActiveMenuItemsAsync(cancellationToken);

        var visibleItems = menuItems
            .Where(mi => ShouldInclude(mi, userRoles, userPermissions))
            .ToList();

        var dtoLookup = visibleItems.ToDictionary(mi => mi.Id, mi => new MenuItemDto
        {
            Id = mi.Id,
            Label = mi.Label,
            Route = mi.Route,
            Icon = mi.Icon,
            Exact = mi.Exact,
            Order = mi.Order
        });

        var roots = new List<MenuItemDto>();

        foreach (var item in visibleItems.OrderBy(mi => mi.Order))
        {
            var dto = dtoLookup[item.Id];
            if (item.ParentId.HasValue && dtoLookup.TryGetValue(item.ParentId.Value, out var parentDto))
            {
                parentDto.Children.Add(dto);
            }
            else
            {
                roots.Add(dto);
            }
        }

        SortChildren(roots);
        return roots;
    }

    private static bool ShouldInclude(MenuItem menuItem, HashSet<string> userRoles, HashSet<string> userPermissions)
    {
        var requiredRoles = menuItem.MenuItemRoles
            .Select(mr => mr.Role?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        var requiredPermissions = menuItem.MenuItemPermissions
            .Select(mp => mp.Permission?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();

        var hasRole = !requiredRoles.Any() || requiredRoles.Any(role => userRoles.Contains(role!));
        var hasPermission = !requiredPermissions.Any() || requiredPermissions.Any(permission => userPermissions.Contains(permission!));

        return hasRole && hasPermission;
    }

    private static void SortChildren(IList<MenuItemDto> items)
    {
        foreach (var item in items.OrderBy(i => i.Order).ToList())
        {
            if (item.Children.Any())
            {
                var sorted = item.Children.OrderBy(child => child.Order).ToList();
                item.Children.Clear();
                foreach (var child in sorted)
                {
                    item.Children.Add(child);
                }
                SortChildren(item.Children);
            }
        }
    }
}
