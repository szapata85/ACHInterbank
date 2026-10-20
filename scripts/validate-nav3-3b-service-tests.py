#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
checks = 0


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def require(condition: bool, message: str) -> None:
    global checks
    checks += 1
    if not condition:
        raise AssertionError(message)


def slice_between(source: str, start: str, end: str) -> str:
    i = source.find(start)
    require(i >= 0, f"missing start marker: {start}")
    j = source.find(end, i + len(start))
    require(j > i, f"missing end marker after {start}: {end}")
    return source[i:j]


app = read("web/ach-interbank-ui/src/app/app-routing.module.ts")
operational = read("web/ach-interbank-ui/src/app/features/nacha-operational/nacha-operational-routing.module.ts")
spa_service = read("web/ach-interbank-ui/src/app/features/nacha-operational/services/nacha-soap-uat-console.service.ts")
controller = read("src/Cfa.ACHInterbank.Api/Controllers/NachaSoapUatConsoleController.cs")
read_model = read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaSoapUatConsoleReadModelService.cs")
read_store = read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaOperationalReadStore.cs")
uat_contrapartidas = read("src/Cfa.ACHInterbank.Api/Controllers/UatContrapartidasController.cs")
menu_auth = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsMenuAuthorizationSeeder.cs")
canonical_menu = read("src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs")
grant = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/TestToolsAdminRolePermissionSeeder.cs")
simulator_controller = read("src/Cfa.ACHInterbank.Api/Controllers/NachaInboundSimulatorController.cs")
simulator_service = read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs")

service_route = slice_between(app, "path: 'ach/nacha/soap-uat-console'", "path: 'ach'")
require("canActivate: [permissionGuard]" in service_route, "service route must use only permissionGuard")
require("permissions: ['TestTools.Access']" in service_route, "service route must require TestTools.Access")
require("roleGuard" not in service_route and "roles:" not in service_route, "service route must not require roles")
require("CanReadAch" not in service_route and "CanManageAch" not in service_route, "service route must not require legacy ACH permissions")
require("NachaSoapUatConsoleComponent" in service_route, "service route must load the console component directly")
require("path: 'nacha/soap-uat-console'" not in operational, "service route must not remain under shared /ach child routing")

ach_parent = slice_between(app, "path: 'ach'", "path: 'payment-rail-capability-registry'")
require("canActivate: [roleGuard, permissionGuard]" in ach_parent, "shared /ach parent guards must remain unchanged")
require("roles: ['Admin', 'ACH.Operator']" in ach_parent, "shared /ach parent roles must remain unchanged")
require("permissions: ['CanReadAch']" in ach_parent, "shared /ach parent permission must remain unchanged")

require("[Authorize(Policy = FineGrainedPermissions.TestTools.Access)]" in controller, "controller must require TestTools.Access")
require("IsProduction" not in controller and "IsUatLike" not in controller, "controller must not gate by environment")
require("UAT_FEATURE_UNAVAILABLE" not in controller, "controller must not return environment-unavailable response")
require("NachaSimulatorRead" not in controller, "console must not inherit simulator-specific read permission")
require("[HttpPost" not in controller and "[HttpPut" not in controller and "[HttpDelete" not in controller and "[HttpPatch" not in controller, "console controller must remain GET-only")

require("api/ach/nacha/soap-uat-console" in spa_service, "SPA must call the console API only")
require("this.api.get" in spa_service, "SPA console service must use GET")
require(all(token not in spa_service for token in ("this.api.post", "this.api.put", "this.api.patch", "this.api.delete")), "SPA console service must not issue mutating HTTP calls")
require("INachaOperationalReadStore" in read_model, "console backend must use read store")
require("ProductiveExecution = false" in read_model and "WouldInvokeRealSoap = false" in read_model, "console must report no productive execution / real SOAP")
require(all(token not in read_model for token in ("IContrapartidaDispatchJobService", "ProcessTransactionAsync", "ProcessCycleAsync", "SaveChanges")), "console read model must not dispatch or persist")
require("AsNoTracking()" in read_store, "operational read store must use no-tracking reads")
require("SaveChanges" not in read_store, "operational read store must not persist")

require("[Route(\"api/uat/contrapartidas\")]" in uat_contrapartidas, "UatContrapartidas route must remain present")
require("IContrapartidaDispatchJobService" in uat_contrapartidas and "ProcessTransactionAsync" in uat_contrapartidas, "UatContrapartidas remains the live dispatch surface")
require("_hostEnvironment.IsProduction()" in uat_contrapartidas, "UatContrapartidas production gate must remain")
require("ACH_SOAP_LIVE_TESTS" in uat_contrapartidas, "UatContrapartidas live feature flag must remain")

require("RequireTestToolsAccessOnlyAsync(serviceTests.Id)" in menu_auth, "menu authorization must reconcile service tests to TestTools.Access")
require("serviceTests.IsActive = true;" in menu_auth, "service tests must be active after security reconciliation")
require("context.MenuItemRoles.RemoveRange(roleLinks);" in menu_auth, "menu must not use direct role authorization")
require("serviceTests.IsActive = false;" in canonical_menu, "canonical seeder must keep service tests inactive before security reconciliation")
require("RoleConfiguration.AdminRoleId" in grant and "RoleConfiguration.OperatorRoleId" not in grant, "only Admin may receive TestTools.Access")
require("UserId" not in grant, "no direct user grants may be introduced")

require("[Authorize(Policy = FineGrainedPermissions.TestTools.Access)]" in simulator_controller, "inbound simulator must retain TestTools.Access")
require("_options.AllowAutoImport || _options.AllowExternalTransmission" in simulator_service, "inbound simulator functional safety guard must remain")
require("ExternalTransmission = false" in simulator_service and "AutoImported = false" in simulator_service, "inbound simulator no-external-effects state must remain")

print(f"NAV3.3B static validation passed: {checks} assertions")
