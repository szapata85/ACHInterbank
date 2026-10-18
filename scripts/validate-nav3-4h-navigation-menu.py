#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
assertions = 0

def read(path: str) -> str:
    return (ROOT / path).read_text(encoding='utf-8')

def require(condition: bool, message: str) -> None:
    global assertions
    assertions += 1
    if not condition:
        raise AssertionError(message)

def between(source: str, start: str, end: str) -> str:
    i = source.find(start)
    require(i >= 0, f'missing start marker: {start}')
    j = source.find(end, i + len(start))
    require(j > i, f'missing end marker after {start}: {end}')
    return source[i:j]

fine = read('src/Cfa.ACHInterbank.Application/Security/FineGrainedPermissions.cs')
p1 = read('src/Cfa.ACHInterbank.Application/Security/P1Policies.cs')
policies = read('src/Cfa.ACHInterbank.External/DependencyInjectionService.cs')
perm_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/NavigationMenuPermissionSeeder.cs')
menu_seed = read('src/Cfa.ACHInterbank.Persistence/Security/Seeders/NavigationMenuAuthorizationSeeder.cs')
role_perm_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/RolePermissionConfiguration.cs')
role_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/RoleConfiguration.cs')
perm_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/PermissionConfiguration.cs')
menu_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemConfiguration.cs')
menu_perm_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemPermissionConfiguration.cs')
menu_role_cfg = read('src/Cfa.ACHInterbank.Persistence/Configuration/MenuItemRoleConfiguration.cs')
ia = read('src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs')
controller = read('src/Cfa.ACHInterbank.Api/Controllers/MenuItemsController.cs')
service = read('src/Cfa.ACHInterbank.Persistence/Navigation/Services/MenuItemsService.cs')
dtos = read('src/Cfa.ACHInterbank.Application/Navigation/Dtos/MenuItemAdminDtos.cs')
app_routing = read('web/ach-interbank-ui/src/app/app-routing.module.ts')
nav_routing = read('web/ach-interbank-ui/src/app/features/navigation/navigation-routing.module.ts')
component = read('web/ach-interbank-ui/src/app/features/navigation/components/navigation-menu.component.ts')
html = read('web/ach-interbank-ui/src/app/features/navigation/components/navigation-menu.component.html')
ng_service = read('web/ach-interbank-ui/src/app/features/navigation/services/navigation-menu.service.ts')
roles_service = read('web/ach-interbank-ui/src/app/core/services/roles-api.service.ts')
permissions_service = read('web/ach-interbank-ui/src/app/core/services/permissions.service.ts')
handler = read('src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs')
navlogs = read('src/Cfa.ACHInterbank.Api/Controllers/NavigationLogsController.cs')
reports_routing = read('web/ach-interbank-ui/src/app/features/reports/reports-routing.module.ts')

def route_block(start: str, end: str) -> str:
    return between(app_routing, f"path: '{start}'", f"path: '{end}'")

# Characterization: actual menu identity and structure remain canonical.
require('public const int NavigationAdminId = 9;' in menu_cfg, 'NavigationAdminId changed')
require('Route = "/navigation/menu-items"' in menu_cfg, 'legacy menu route missing')
require('public const string MenuOrganizationRoute = "/navigation/menu-items";' in ia, 'canonical route changed')
require('public const string MenuOrganizationLabel = "Organización del menú";' in ia, 'canonical label changed')
require('Reconcile(menuOrganization, MenuOrganizationLabel, administration.Id, order: 40);' in ia, 'menu organization must remain Administration child Order 40')

# Legacy audience evidence: only source-defined Admin receives CanManageUsers; Operator does not.
require('Name = "Admin"' in role_cfg and 'Name = "ACH.Operator"' in role_cfg, 'expected seeded roles missing')
manage_grant = between(role_perm_cfg,
    'RoleId = RoleConfiguration.AdminRoleId,\n                PermissionId = PermissionConfiguration.ManageUsersPermissionId',
    'new RolePermission\n            {\n                RoleId = RoleConfiguration.AdminRoleId,\n                PermissionId = PermissionConfiguration.ManageCertificatesPermissionId')
require('ManageUsersPermissionId' in manage_grant, 'Admin CanManageUsers grant missing')
require('RoleConfiguration.OperatorRoleId,\n                PermissionId = PermissionConfiguration.ManageUsersPermissionId' not in role_perm_cfg,
        'ACH.Operator must not have CanManageUsers')
require('Name = "CanManageUsers"' in perm_cfg, 'legacy CanManageUsers definition missing')

# Dedicated capability and idempotent persistence, without grants.
require('public static class NavigationMenu' in fine and 'public const string Manage = "NavigationMenu.Manage";' in fine,
        'NavigationMenu.Manage declaration missing')
require('NavigationMenu.Manage' in fine[fine.find('AllPermissions'):], 'NavigationMenu.Manage missing from AllPermissions')
require('public const string NavigationMenuManage = "P1.NavigationMenuManage";' in p1, 'P1 policy constant missing')
require('public sealed class NavigationMenuPermissionSeeder' in perm_seed, 'permission seeder missing')
require('FirstOrDefaultAsync(item => item.Name == FineGrainedPermissions.NavigationMenu.Manage)' in perm_seed, 'permission seeder must search by name')
require('?? await context.Permissions.FindAsync(PermissionId)' in perm_seed, 'permission seeder must fallback by stable id')
require('permission.Description = "Administrar organización del menú";' in perm_seed, 'permission functional label missing')
for forbidden in ('RolePermission', 'AdminRoleId', 'OperatorRoleId', 'UserPermission'):
    require(forbidden not in perm_seed, f'permission seeder must not create grants: {forbidden}')
require('NavigationMenuPermissionSeeder.PermissionId' not in role_perm_cfg, 'new permission must have no RolePermission seed')

# Policy is semantic capability OR validated legacy fallback; no role-name boundary.
nav_policy = between(policies, 'options.AddPolicy(P1Policies.NavigationMenuManage', 'options.AddPolicy(P1Policies.SchedulerView')
require('FineGrainedPermissions.NavigationMenu.Manage' in nav_policy, 'navigation menu policy missing dedicated capability')
require('"CanManageUsers"' in nav_policy, 'navigation menu policy missing validated legacy fallback')
require('IsInRole' not in nav_policy and '"Admin"' not in nav_policy, 'navigation menu policy must not hardcode Admin role')

# Supporting selector reads accept the dedicated capability, because the UI edits MenuItemRole/MenuItemPermission.
roles_read = between(policies, 'options.AddPolicy(P1Policies.RolesRead', 'options.AddPolicy(P1Policies.RolesCreate')
permissions_read = between(policies, 'options.AddPolicy(P1Policies.PermissionsRead', 'options.AddPolicy(P1Policies.PermissionsAssign')
require('FineGrainedPermissions.NavigationMenu.Manage' in roles_read, 'RolesRead must support navigation menu selector')
require('FineGrainedPermissions.NavigationMenu.Manage' in permissions_read, 'PermissionsRead must support navigation menu selector')

# Menu authorization reconciler mirrors policy OR and removes legacy role AND gate.
require('public const int AuthorizationReconciliationOrder = 108;' in menu_seed, 'NAV3.4H menu reconciler order must be 108')
require('NavigationInformationArchitectureSeeder.MenuOrganizationRoute' in menu_seed, 'menu reconciler must target canonical route')
require('NavigationMenuPermissionSeeder.PermissionId' in menu_seed and 'PermissionConfiguration.ManageUsersPermissionId' in menu_seed,
        'menu must use dedicated OR legacy permissions')
require('context.MenuItemRoles.RemoveRange(roleLinks);' in menu_seed, 'menu role gate must be removed')
require('RolePermission' not in menu_seed and 'UserPermission' not in menu_seed, 'menu reconciler must not create grants')

# Current base seed is characterized as Admin + CanManageUsers before reconciliation.
require('MenuItemConfiguration.NavigationAdminId' in menu_perm_cfg and 'PermissionConfiguration.ManageUsersPermissionId' in menu_perm_cfg,
        'legacy MenuItem CanManageUsers association missing from model seed')
require('MenuItemConfiguration.NavigationAdminId' in menu_role_cfg and 'RoleConfiguration.AdminRoleId' in menu_role_cfg,
        'legacy MenuItem Admin association missing from model seed')

# SPA parent is structural redirect prefix and child is explicitly guarded by same capability alternatives.
nav_parent = route_block('navigation', 'audit-logs')
require('canActivate: [permissionGuard]' in nav_parent, '/navigation must use permissionGuard')
require("permissions: ['NavigationMenu.Manage', 'CanManageUsers']" in nav_parent, '/navigation permissions mismatch')
require('roles:' not in nav_parent and 'roleGuard' not in nav_parent, '/navigation must not hardcode role')
require("redirectTo: 'menu-items'" in nav_routing and "path: 'menu-items'" in nav_routing, '/navigation redirect/menu-items route missing')
require('canActivate: [permissionGuard]' in nav_routing, '/navigation/menu-items must have explicit permission guard')
require("permissions: ['NavigationMenu.Manage', 'CanManageUsers']" in nav_routing, 'child route capability mismatch')
require("hasPermission(['NavigationMenu.Manage', 'CanManageUsers'])" in component, 'component mutation gate mismatch')
require("hasRole('Admin')" not in component, 'component must not depend on Admin role name')

# UI operations and persistence characterize structure plus authorization management.
for marker in ('roleIds: [[] as string[]]', 'permissionIds: [[] as string[]]', 'payload.isActive = !item.isActive',
               'startCreate(', 'editItem(', 'confirmDelete('):
    require(marker in component, f'component operation missing: {marker}')
for marker in ('Roles permitidos', 'Permisos necesarios', 'formControlName="parentId"', 'formControlName="order"',
               'formControlName="exact"', 'formControlName="isActive"'):
    require(marker in html, f'editor field missing: {marker}')
for marker in ('getMenuItems()', 'createMenuItem(', 'updateMenuItem(', 'deleteMenuItem(', "basePath = 'api/navigation/menu-items'"):
    require(marker in ng_service, f'Angular menu endpoint missing: {marker}')
require("basePath = 'api/roles'" in roles_service, 'roles selector endpoint missing')
require("basePath = 'api/permissions'" in permissions_service, 'permissions selector endpoint missing')
for marker in ('menuItem.ParentId = request.ParentId', 'menuItem.Label = request.Label', 'menuItem.Route = request.Route',
               'menuItem.Order = request.Order', 'menuItem.Exact = request.Exact', 'menuItem.IsActive = request.IsActive',
               'UpdateRelationsAsync(menuItem, request.RoleIds, request.PermissionIds', '_dbContext.MenuItems.Remove(menuItem)'):
    require(marker in service, f'persistence operation missing: {marker}')
require('IEnumerable<Guid> RoleIds' in dtos and 'IEnumerable<Guid> PermissionIds' in dtos,
        'save request must persist role/permission associations')

# API exposes only GET/POST/PUT/DELETE; no hidden patch/reorder endpoint.
require('[Authorize(Policy = P1Policies.NavigationMenuManage)]' in controller, 'controller semantic policy missing')
require('Authorize(Roles' not in controller, 'controller must not be role-name gated')
for marker in ('[HttpGet]', '[HttpPost]', '[HttpPut("{id:int}")]', '[HttpDelete("{id:int}")]'):
    require(marker in controller, f'CRUD endpoint missing: {marker}')
require('[HttpPatch' not in controller and 'reorder' not in controller.lower(), 'unexpected extra mutation endpoint found')

# No direct user grants exist in the auth claim model; permissions are derived from RolePermissions.
auth_service = read('src/Cfa.ACHInterbank.Application/Services/Authentication/Implementations/AuthService.cs')
require('.SelectMany(x => x.Role!.RolePermissions)' in auth_service, 'JWT permissions must remain role-derived')
require('UserPermission' not in auth_service, 'unexpected direct user permission path')

# Escalation boundary: this screen changes menu authorization metadata, not RolePermissions or user roles.
require('_dbContext.MenuItemRoles.RemoveRange' in service and '_dbContext.MenuItemPermissions.RemoveRange' in service,
        'screen must be characterized as mutating menu authorization metadata')
require('RolePermissions' not in service and 'UserRoles' not in service, 'menu service must not mutate grants or user roles')

# Regression checks: NAV3.4G2/report route/handler remain unchanged semantically.
require("permissions: ['NavigationLogs.Read', 'CanReadAch']" in app_routing, 'navigation logs authorization regressed')
require('[Authorize(Policy = P1Policies.NavigationLogsRead)]' in navlogs, 'navigation logs GET policy regressed')
require("path: 'audit'" in reports_routing and "permissions: ['CanReadAch']" in reports_routing, '/reports/audit changed')
require('return hasRole && hasPermission;' in handler,
        'GetMenuForCurrentUserHandler changed unexpectedly')

print(f'PASS: NAV3.4H static focal validation ({assertions} assertions)')
