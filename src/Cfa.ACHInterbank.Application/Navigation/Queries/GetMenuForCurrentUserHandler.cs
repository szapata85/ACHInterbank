using System.Security.Claims;
using Cfa.ACHInterbank.Application.DataBase.Queries.Navigation;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.Navigation;
using Cfa.ACHInterbank.Domain.Entities.Navigation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Application.Navigation.Queries;

public class GetMenuForCurrentUserHandler : IRequestHandler<GetMenuForCurrentUserQuery, IList<MenuItemDto>>
{
    private static readonly HashSet<string> LegacyNachaRoutes = new(StringComparer.OrdinalIgnoreCase)
    {
        "/ach-cycles/nacha/layouts",
        "/ach-cycles/nacha/definitions",
        "/nacha-layouts",
        "/nacha-record-definitions"
    };

    private readonly IMenuQueryRepository _menuRepository;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly bool _uatSimulatorAvailable;

    public GetMenuForCurrentUserHandler(
        IMenuQueryRepository menuRepository,
        IHttpContextAccessor httpContextAccessor,
        IOptions<NachaInboundSimulatorOptions>? simulatorOptions = null,
        IHostEnvironment? environment = null)
    {
        _menuRepository = menuRepository;
        _httpContextAccessor = httpContextAccessor;
        _uatSimulatorAvailable = environment?.IsProduction() != true
            && simulatorOptions?.Value.IsUatLike() == true;
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
            .Where(mi => !LegacyNachaRoutes.Contains(mi.Route))
            .Where(mi => _uatSimulatorAvailable
                || (!string.Equals(mi.Route, "/uat", StringComparison.OrdinalIgnoreCase)
                    && !mi.Route.StartsWith("/uat/", StringComparison.OrdinalIgnoreCase)))
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

        RemoveLegacyNachaRoutes(roots);
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

    private static void EnsureOfficialNachaConfigMenu(IList<MenuItemDto> roots, HashSet<string> userPermissions)
    {
        if (!userPermissions.Contains("CanReadAch"))
        {
            return;
        }

        var group = roots.FirstOrDefault(x =>
            x.Route == NavigationRoutes.NachaConfigGroup ||
            x.Route == NavigationRoutes.NachaConfigProfiles ||
            x.Label == "Configuración NACHA-M" ||
            x.Label == "NACHA-M Configuración" ||
            x.Label == "NACHA-M ConfiguraciÃ³n" ||
            x.Label == "Config Profiles");
        if (group is null)
        {
            group = new MenuItemDto
            {
                Id = 20,
                Label = "Configuración NACHA-M",
                Route = NavigationRoutes.NachaConfigGroup,
                Icon = "tune",
                Exact = true,
                Order = 2
            };
            roots.Add(group);
        }
        else
        {
            group.Label = "Configuración NACHA-M";
            group.Route = NavigationRoutes.NachaConfigGroup;
            group.Icon = group.Icon ?? "tune";
        }

        AddOrUpdateChild(group, 25, "Perfiles oficiales", NavigationRoutes.NachaConfigProfiles, "fact_check", 1);
        AddOrUpdateChild(group, 2802, "Registros oficiales", NavigationRoutes.NachaConfigRecords, "view_list", 2);
        AddOrUpdateChild(group, 2803, "Variantes y campos", NavigationRoutes.NachaConfigVariantsFields, "schema", 3);
    }

    private static void AddOrUpdateChild(MenuItemDto parent, int id, string label, string route, string icon, int order)
    {
        var child = parent.Children.FirstOrDefault(x => x.Route == route || x.Id == id);
        if (child is null)
        {
            parent.Children.Add(new MenuItemDto
            {
                Id = id,
                Label = label,
                Route = route,
                Icon = icon,
                Exact = true,
                Order = order
            });
            return;
        }

        child.Label = label;
        child.Route = route;
        child.Icon = child.Icon ?? icon;
        child.Exact = true;
        child.Order = order;
    }

    private static void RemoveLegacyNachaRoutes(IList<MenuItemDto> items)
    {
        for (var i = items.Count - 1; i >= 0; i--)
        {
            var item = items[i];
            RemoveLegacyNachaRoutes(item.Children);
            if (LegacyNachaRoutes.Contains(item.Route))
            {
                items.RemoveAt(i);
            }
        }
    }
}
