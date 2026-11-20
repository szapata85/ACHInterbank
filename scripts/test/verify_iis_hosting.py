#!/usr/bin/env python3
"""Chequeo estatico portable del soporte IIS; no sustituye build ni UAT."""
from pathlib import Path
import json
import xml.etree.ElementTree as ET

import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from jsonc_settings import load_jsonc

root = Path(__file__).resolve().parents[2]
api = (root / 'src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs').read_text(encoding='utf-8-sig')
settings = load_jsonc(root / 'src/Cfa.ACHInterbank.Api/appsettings.json')
prod = (root / 'web/ach-interbank-ui/src/environments/environment.prod.ts').read_text()
publish = (root / 'scripts/iis/publish-iis.ps1').read_text()
sql_migration = (root / 'src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261118110000_QuartzSqlServer471Schema.cs').read_text()

assert settings['SpaHosting']['Enabled'] is False, 'Docker/default deben conservar SPA deshabilitada'
assert settings['Database']['ApplyMigrations'] is True, 'EF Core migrations deben habilitarse por appsettings.json'
assert settings['Database']['ApplySeed'] is False
assert settings['Quartz']['JobStore']['Mode'] == 'Persistent'
assert settings['Quartz']['JobStore']['Provider'] == 'SqlServer'
assert "apiBaseUrl: ''" in prod, 'SPA y API requieren mismo origen en modo IIS'
assert 'app.UseDefaultFiles();' in api and 'app.UseStaticFiles(' in api
assert 'app.MapFallback(' in api and 'IsReservedServerPath(path)' in api
assert 'IsMissingStaticAssetPath(path)' in api
assert 'Path.GetExtension(value)' in api
assert 'string[] prefixes = ["/api", "/auth", "/health", "/openapi", "/scalar"]' in api
assert 'app.MapPublicHealthEndpoints();' in api and 'app.MapControllers();' in api
assert 'wwwroot/index.html' in api
assert 'AppContext' not in publish, 'No generar rutas basadas en entorno de quien verifica'
assert "Set-JsoncSetting -Path $publishedSettingsPath -Section 'SpaHosting' -Name 'Enabled' -Value $true" in publish, 'El publicador IIS debe habilitar SPA preservando comentarios JSONC'
assert 'RemoveChild($environmentVariables)' in publish
for key in ("'SpaHosting__Enabled'", "'ASPNETCORE_ENVIRONMENT'", "'Database__ApplyMigrations'", "'Database__ApplySeed'"):
    assert key not in publish, 'No inyectar variables de entorno desde el publicador IIS'
assert 'npm.cmd ci' in publish and 'dotnet publish' in publish
assert 'SqlServer471' in sql_migration and 'SplitSqlServerBatches' in sql_migration
assert (root / 'scripts/iis/publish-iis.cmd').is_file()
assert (root / 'docs/deployment/IIS_WINDOWS_SERVER.md').is_file()
ET.parse(root / 'src/Cfa.ACHInterbank.Api/Properties/PublishProfiles/FolderProfile1.pubxml')
print('IIS static package contract OK (no runtime or recovery certification).')
