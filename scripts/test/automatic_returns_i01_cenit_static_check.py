#!/usr/bin/env python3
"""I01-CENIT controles de fuente independientes (NO son pruebas .NET/DB)."""
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[2]
files = {
    "service": "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitAutomaticReturnSettingsService.cs",
    "controller": "src/Cfa.ACHInterbank.Api/Controllers/CenitAutomaticReturnSettingsController.cs",
    "model": "src/Cfa.ACHInterbank.Application/ACH/Models/CenitAutomaticReturnSettings.cs",
    "interface": "src/Cfa.ACHInterbank.Application/ACH/Interfaces/ICenitAutomaticReturnSettingsService.cs",
    "dbconfig": "src/Cfa.ACHInterbank.Persistence/Configuration/ClearingHouseConfigConfiguration.cs",
    "domain": "src/Cfa.ACHInterbank.Domain/Models/ACH/ClearingHouseConfig.cs",
    "sqlserver": "src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261102210000_AchColombiaAutomaticReturnConfigurationI01.cs",
    "postgres": "src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261102233000_CenitAutomaticReturnConfigurationI01.cs",
    "pgsnapshot": "src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/AchDbContextModelSnapshot.cs",
    "di": "src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs",
    "tests": "tests/Cfa.ACHInterbank.Tests/CenitAutomaticReturnSettingsI01Tests.cs",
}
text = {key: (ROOT / name).read_text(encoding="utf-8-sig") for key, name in files.items()}
checks = []
def check(label, predicate):
    checks.append((label, bool(predicate)))

check("modo default en modelo compartido", '"DESACTIVADO"' in text["domain"])
check("modo default y longitud 20 en EF", '.HasDefaultValue("DESACTIVADO")' in text["dbconfig"] and '.HasMaxLength(20)' in text["dbconfig"])
check("concurrencia EF", '.IsConcurrencyToken()' in text["dbconfig"])
check("restricción CHECK en EF", 'CK_ClearingHouseConfigs_AutomaticReturnMode' in text["dbconfig"])
check("ruta exclusiva CENIT", '[Route("api/cenit/devoluciones-automaticas/configuracion")]' in text["controller"])
check("GET exige ConfigRead", bool(re.search(r'\[HttpGet\]\s*\[Authorize\(Policy = P1Policies.ConfigRead\)\]', text["controller"])))
check("PUT exige ConfigManage", bool(re.search(r'\[HttpPut\]\s*\[Authorize\(Policy = P1Policies.ConfigManage\)\]', text["controller"])))
check("controlador autenticado", '[Authorize]' in text["controller"])
check("400 para valor inválido", 'BadRequest(' in text["controller"] and 'ArgumentException' in text["controller"])
check("409 para conflicto", 'Conflict(' in text["controller"] and 'DbUpdateConcurrencyException' in text["controller"])
check("valores estrictos", 'mode is Disabled or WithApproval or Automatic' in text["service"])
check("aislamiento por código CENIT", 'RegulatoryCycleScheduleCatalog.CenitCode' in text["service"] and 'AchColombiaCode' not in text["service"])
check("no acepta fila ajena", 'config.Id != house.ClearingHouseId' in text["service"])
check("detecta fila compartida", 'item.Id != house.Id && item.ClearingHouseId == config.Id' in text["service"])
check("revisión anterior obligatoria", 'request.ExpectedMode' in text["service"] and '!= request.ExpectedMode' in text["service"])
check("cambio de modo auditado por DB", 'context.SaveChangesAsync' in text["service"] and 'AuditLogs' in text["service"])
check("procesamiento siempre desconectado", text["service"].count('ProcessingConnected: false') == 2)
check("sin motor / envío en el servicio", all(term not in text["service"] for term in ('ReturnFileRunner', 'GenerateAutomaticallyAsync', 'SendAsync(', 'ExportAsync(', 'Quartz', 'IHostedService')))
check("interface propia CENIT", 'ICenitAutomaticReturnSettingsService' in text["interface"])
check("contrato incluye ExpectedMode", 'string ExpectedMode' in text["model"])
check("registro DI", 'AddScoped<ICenitAutomaticReturnSettingsService, CenitAutomaticReturnSettingsService>' in text["di"])
check("SQL Server conserva migración de configuración", 'defaultValue: "DESACTIVADO"' in text["sqlserver"] and 'CK_ClearingHouseConfigs_AutomaticReturnMode' in text["sqlserver"])
check("Postgres añade columna + CHECK", 'migrationBuilder.AddColumn<string>' in text["postgres"] and 'CK_ClearingHouseConfigs_AutomaticReturnMode' in text["postgres"])
check("Postgres es migración EF identificada", '[Migration("20261102233000_CenitAutomaticReturnConfigurationI01")]' in text["postgres"])
check("snapshot Postgres refleja columna", 'b.Property<string>("AutomaticReturnMode")' in text["pgsnapshot"] and '.IsConcurrencyToken()' in text["pgsnapshot"])
check("pruebas incluyen auditoría", 'AuditLogs' in text["tests"])
check("pruebas incluyen doble escritura", 'ConcurrentTrackedWritesFailByEntityConcurrencyToken' in text["tests"])
check("pruebas incluyen ACH y CENIT", 'AchColombiaAutomaticReturnSettingsService' in text["tests"] and 'CenitAutomaticReturnSettingsService' in text["tests"])
check("pruebas incluyen conservación de permisos", 'ReadAndWriteRequireExistingConfigPolicies' in text["tests"])
for name, ok in checks:
    print(f'{"PASS" if ok else "FAIL"}: {name}')
print(f'I01-CENIT: {sum(ok for _, ok in checks)}/{len(checks)} controles estáticos')
if not all(ok for _, ok in checks):
    raise SystemExit(1)
