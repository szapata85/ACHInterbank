#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSERTIONS = 0


def require(condition: bool, message: str) -> None:
    global ASSERTIONS
    ASSERTIONS += 1
    if not condition:
        raise AssertionError(message)


def read(path: str) -> str:
    return (ROOT / path).read_text(encoding="utf-8")


def sha(path: str) -> str:
    return hashlib.sha256((ROOT / path).read_bytes()).hexdigest()


# Protected NAV4.1A / NAV4.1B / NAV4.2A and security/NAV2 files remain byte-identical.
protected = {
    "web/ach-interbank-ui/src/app/core/services/navigation.service.ts": "4df488ac45465a5283a02afe795fc64c4e5995a3415556d324275cc5e1d7ea93",
    "web/ach-interbank-ui/src/app/core/services/menu-route-surface.service.ts": "3a32dc7921e3c805e32fa9921e371109813aae9ba3a3173bda6a5b6b7030df72",
    "web/ach-interbank-ui/src/app/core/services/navigation-naming.service.ts": "de6d4a402690956914a67f26466c7ea6c1cfac16eb717dbeb4d4b6f56e59db19",
    "web/ach-interbank-ui/src/app/core/services/navigation-naming.util.ts": "17e066e755f589ee818b6fa48fbcf3963ba14886a35cdf3187e5381e80368895",
    "src/Cfa.ACHInterbank.Application/Navigation/Queries/GetMenuForCurrentUserHandler.cs": "f111e416c181fc1912c297e37012cf7462f49d0434c1ef6ec9416c89b5ed1a61",
    "src/Cfa.ACHInterbank.Persistence/Navigation/Seeders/NavigationInformationArchitectureSeeder.cs": "28371549f4b9150fdf738bee11454c98117ba420d1abae0e9971e2c49aee9f4c",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs": "e4f2b80b0b9e9c3aff636fb03e96b819019c07eeabc549bcfe4f789756775437",
    "src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs": "e2366e75faab4ee31498d0cb84a93899a2a928698b01e4401a38ab1cc9c18105",
    "web/ach-interbank-ui/src/app/core/guards/permission.guard.ts": "7d9541b3899543ad40802b18cb4e734a9c14e59077b732108b31396213afa73a",
    "web/ach-interbank-ui/src/app/core/guards/role.guard.ts": "520fcea2357e5cfe2b19d3ad1d0b284ee84d21149b359025126308210d6de31e",
}
for path, expected in protected.items():
    require(sha(path) == expected, f"protected file changed: {path}")

fine = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/FineGrainedGrantBackfillSeeder.cs")
legacy = read("src/Cfa.ACHInterbank.Persistence/Security/Seeders/LegacyFineGrainedRoleMigrationSeeder.cs")
require("BackfillOrder = 119" in fine, "FineGrainedGrantBackfillSeeder must remain Order 119")
require("MigrationOrder = 120" in legacy, "LegacyFineGrainedRoleMigrationSeeder must remain Order 120")

# Every focal DRIFT uses the already-approved NAV2 destination label.
checks = {
    "web/ach-interbank-ui/src/app/features/operational-restrictions/operational-restrictions.component.html": [
        'routerLink="/clearing-houses"', "Volver a Cámaras compensadoras"],
    "web/ach-interbank-ui/src/app/features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html": [
        'routerLink="/administracion/mft-ach"', "Consultar Configuración del intercambio de archivos"],
    "web/ach-interbank-ui/src/app/features/clearing-houses/clearing-houses.component.html": [
        'routerLink="/operational-restrictions"', "Restricciones operativas"],
    "web/ach-interbank-ui/src/app/features/clearing-houses/transaction-policies.component.html": [
        "Volver a Cámaras compensadoras"],
    "web/ach-interbank-ui/src/app/features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.html": [
        'texto="Volver a Cola de procesamiento"'],
    "web/ach-interbank-ui/src/app/features/incoming-nacha-command-center/pages/incoming-nacha-queue-page.component.html": [
        'texto="Volver a Seguimiento de archivos"'],
    "web/ach-interbank-ui/src/app/features/incoming-nacha-command-center/pages/incoming-nacha-orphans-page.component.html": [
        "Volver a Seguimiento de archivos"],
    "web/ach-interbank-ui/src/app/features/incoming-nacha-command-center/pages/incoming-nacha-observability-page.component.html": [
        'texto="Volver a Seguimiento de archivos"'],
    "web/ach-interbank-ui/src/app/features/nacha-security/components/digital-envelope-tool.component.html": [
        '<a routerLink="/nacha-security/certificates">Certificados digitales</a>', "Ir a Certificados digitales"],
    "web/ach-interbank-ui/src/app/features/transactions/outgoing-monitoring/outgoing-transaction-monitoring-detail.component.html": [
        "Volver a Transacciones enviadas"],
    "web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.html": [
        "Ver Seguimiento de cargas", "Ir a Seguimiento de cargas"],
    "web/ach-interbank-ui/src/app/features/transactions/components/cycle-config-management/cycle-config-management.component.html": [
        "Volver a Cámaras compensadoras"],
    "web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.html": [
        "Cargar transacciones desde archivo", "Seguimiento de cargas"],
    "web/ach-interbank-ui/src/app/features/integrations/presentation/integration-service-card.component.html": [
        "Ver Conexión entidad participante"],
    "web/ach-interbank-ui/src/app/features/integrations/pages/contract-versions-page.component.html": [
        'routerLink="/integraciones"', "Volver a Conexiones"],
    "web/ach-interbank-ui/src/app/features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html": [
        "Ir a Cargar archivo NACHA-M"],
    "web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-list.component.html": [
        ">Ciclos de operación</a>", "Exportar archivo NACHA-M"],
    "web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-attempts-page.component.html": [
        "Volver a Bandeja de respuestas"],
    "web/ach-interbank-ui/src/app/features/catalogs/components/clearing-house-special-dates.component.html": [
        "Volver a Cámaras compensadoras"],
    "web/ach-interbank-ui/src/app/features/catalogs/components/catalogs-list.component.ts": [
        "{ label: 'Códigos de transacción', route: '/catalogs/transaction-codes' }"],
    "web/ach-interbank-ui/src/app/core/utils/nacha-error-presentation.util.ts": [
        "correctionLabel: 'Ir a Certificados digitales'"],
    "web/ach-interbank-ui/src/app/shared/components/clearing-house-context-navigation.component.ts": [
        'aria-label="Volver a Cámaras compensadoras"', 'matTooltip="Volver a Cámaras compensadoras"', "Volver a Cámaras compensadoras"],
}
for path, markers in checks.items():
    source = read(path)
    for marker in markers:
        require(marker in source, f"missing NAV4.2C canonical navigation copy {marker!r} in {path}")

# Known legacy aliases must not survive in the focal visible navigation controls.
legacy_nav = {
    "web/ach-interbank-ui/src/app/features/integrations/pages/contract-versions-page.component.html": ["Volver al Centro de integración CFA"],
    "web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.html": ["Carga masiva por archivo", "Seguimiento lotes"],
    "web/ach-interbank-ui/src/app/features/clearing-houses/clearing-houses.component.html": [">Restricciones operacionales CFA<"],
    "web/ach-interbank-ui/src/app/features/nacha-security/components/digital-envelope-tool.component.html": ["Ir a Certificados de seguridad NACHA-M", "Ir a la administración de certificados"],
    "web/ach-interbank-ui/src/app/features/catalogs/components/catalogs-list.component.ts": ["Códigos de transacción ACH"],
}
for path, markers in legacy_nav.items():
    source = read(path)
    for marker in markers:
        require(marker not in source, f"legacy navigation alias remains in focal control: {marker!r} in {path}")

# Route/handler behavior is unchanged: controls still navigate to the same destinations.
handler_markers = {
    "web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.ts": [
        "this.router.navigate(['/transactions/bulk-ingestion/upload'])", "this.router.navigate(['/transactions/bulk-ingestion/tracking'])"],
    "web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.ts": [
        "this.router.navigate(['/transactions/bulk-ingestion/tracking'])", "this.router.navigate(['/transactions/bulk-ingestion', batchId])"],
    "web/ach-interbank-ui/src/app/features/incoming-nacha-command-center/pages/incoming-nacha-queue-detail-page.component.ts": [
        "this.router.navigate(['/incoming-nacha-command-center/queue'])"],
    "web/ach-interbank-ui/src/app/features/incoming-nacha-command-center/pages/incoming-nacha-queue-page.component.ts": [
        "this.router.navigate(['/incoming-nacha-command-center'])"],
    "web/ach-interbank-ui/src/app/features/ach-cycles/components/ach-cycle-list.component.ts": [
        "this.router.navigate(['/ach-cycles/nacha/export'])"],
    "web/ach-interbank-ui/src/app/features/ach-responses/pages/ach-response-attempts-page.component.ts": [
        "this.router.navigate(['/ach-responses'])"],
}
for path, markers in handler_markers.items():
    source = read(path)
    for marker in markers:
        require(marker in source, f"navigation handler/destination changed: {marker!r} in {path}")

# Functional actions intentionally remain outside NAV4.2C.
bulk = read("web/ach-interbank-ui/src/app/features/transactions/components/bulk-ingestion-upload/bulk-ingestion-upload.component.html")
for marker in ["(click)=\"upload()\"", "'Enviar archivo'", "Abrir detalle de lote"]:
    require(marker in bulk, f"functional/contextual bulk action unexpectedly changed: {marker}")
file_exchange = read("web/ach-interbank-ui/src/app/features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html")
for marker in [">Descargar</button>", ">Reintentar</button>", ">Reprocesar</button>", ">Archivar</button>", ">Retirar archivo</button>"]:
    require(marker in file_exchange, f"functional file-exchange action unexpectedly changed: {marker}")

# Technical terms remain legitimate where they describe protocol/domain content.
for path, marker in [
    ("web/ach-interbank-ui/src/app/features/ach-colombia-mft-administration/ach-colombia-mft-administration.component.ts", "MFT"),
    ("web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.html", "SOAP"),
    ("web/ach-interbank-ui/src/app/features/uat/components/nacha-inbound-simulator/nacha-inbound-simulator.component.html", "NACHA-M"),
    ("web/ach-interbank-ui/src/app/features/ach-colombia-file-exchange/ach-colombia-file-exchange.component.html", "SHA-256"),
]:
    require(marker in read(path), f"legitimate technical term unexpectedly removed: {marker} from {path}")

# No new global route->label naming mechanism; the existing backend-first service remains unchanged.
for path in [
    "web/ach-interbank-ui/src/app/features/catalogs/components/catalogs-list.component.ts",
    "web/ach-interbank-ui/src/app/core/utils/nacha-error-presentation.util.ts",
    "web/ach-interbank-ui/src/app/shared/components/clearing-house-context-navigation.component.ts",
]:
    source = read(path)
    for forbidden in ["routeToLabel", "routeLabelMap", "switch (route", "switch(this.router.url", "switch (this.router.url"]:
        require(forbidden not in source, f"new route->label taxonomy detected in {path}: {forbidden}")

# Report landing keeps its existing 10 canonical child cards; no route/order/card behavior changed.
reports_home = read("web/ach-interbank-ui/src/app/features/reports/components/reports-home.component.ts")
report_markers = [
    ("Transacciones enviadas", "/reports/sent"), ("Transacciones recibidas", "/reports/received"),
    ("Devoluciones", "/reports/returns"), ("Rechazos", "/reports/rejections"),
    ("Archivos NACHA-M", "/reports/files"), ("Ciclos", "/reports/cycles"),
    ("Conciliación", "/reports/reconciliation"), ("Trazabilidad ACH", "/reports/traceability"),
    ("Histórico", "/reports/history"), ("Auditoría", "/reports/audit"),
]
for label, route in report_markers:
    require(f"title: '{label}'" in reports_home and f"route: '{route}'" in reports_home, f"report card lost canonical label/route: {label} {route}")
require(reports_home.count("route: '/reports/") == 10, "reports landing must retain exactly 10 report child cards")

# Generic history back stays generic and contextual routes keep their own copy.
unauthorized = read("web/ach-interbank-ui/src/app/shared/components/status/unauthorized.component.ts")
require("window.history.back()" in unauthorized and ">Volver<" in unauthorized, "generic history-back control was specialized incorrectly")
require("Volver a usuarios" not in unauthorized, "generic back invented a destination")
users_list = read("web/ach-interbank-ui/src/app/features/admin/components/users-list.component.html")
for marker in ["Crear usuario", "Editar usuario", "Administrar perfiles de acceso"]:
    require(marker in users_list, f"contextual user navigation changed unexpectedly: {marker}")

# Inventory and documentation carry the exact focal classification.
inventory = read("docs/ai/navigation-menu/phases/NAV4_2C_NAVIGATION_CONTROL_INVENTORY.md")
for marker in [
    "**114 definiciones de control**", "**33 ALIGNED**", "**27 DRIFT**", "**48 CONTEXTUAL_NAVIGATION**", "**6 FUNCTIONAL_ACTION_OUT_OF_SCOPE**",
    "`NavigationNamingService` **no fue modificado ni inyectado en nuevos componentes**",
]:
    require(marker in inventory, f"NAV4.2C inventory missing marker: {marker}")
require(len(re.findall(r"^\| \d+ \|", inventory, flags=re.MULTILINE)) == 114, "NAV4.2C inventory must contain 114 numbered control rows")

# NAV4.2B heading/metadata regression scanner must stay green (0 mismatches in its scope).
prior = subprocess.run(["python3", str(ROOT / "scripts/validate-nav4-2b-menu-backed-copy.py")], cwd=ROOT, capture_output=True, text=True)
require(prior.returncode == 0, f"NAV4.2B regression validator failed: {prior.stdout}\n{prior.stderr}")
require("PASS NAV4.2B static validation" in prior.stdout, "NAV4.2B regression validator did not report PASS")

print(f"PASS NAV4.2C static validation — {ASSERTIONS} assertions")
