#!/usr/bin/env python3
"""Guardas estáticas para la fuente única de configuración de la API."""
import json
from pathlib import Path

import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from jsonc_settings import load_jsonc

root = Path(__file__).resolve().parents[2]
api = root / 'src' / 'Cfa.ACHInterbank.Api'
settings_path = api / 'appsettings.json'
settings = load_jsonc(settings_path)

assert not (api / 'appsettings.Development.json').exists()
assert not (api / 'appsettings.Production.json').exists()
assert settings['Database'] == {'Provider': 'SqlServer', 'ApplyMigrations': True, 'ApplySeed': False}
assert settings['Quartz']['JobStore']['Mode'] == 'Persistent'
assert settings['Quartz']['JobStore']['Provider'] == 'SqlServer'
assert settings['NachaInboundSimulator']['Enabled'] is False
assert settings['NachaGeneration']['AllowNonHomologatedCenitDevelopment'] is False
assert settings['SoapIntegrationBootstrap']['OverwriteExistingSettings'] is False
assert all(not settings['SoapIntegrationBootstrap'][method]['Endpoint'] for method in (
    'ProcContrapartidas', 'ProcTransacciones', 'RegistrarRespuestaTransaccion'))
assert settings['SoapTransport']['ControlledLocal']['TransportHost'] == ''
assert settings['SoapEndpointSecurity']['WsAxonRespuestaTransacciones']['Mode'] == 'Unconfigured'
assert settings['ConnectionStrings']['SqlConnection'] == ''
assert settings['appSettings']['tokenManager']['secretKetJwt'] == ''
assert 'headersSecurity' not in settings['appSettings']
assert 'OpenApi' not in settings

cli = (root / 'tools/AchInterbank.DockerCli/Program.cs').read_text()
vs = (root / 'scripts/configure-visualstudio-development.ps1').read_text()
compose = (root / 'docker-compose.yml').read_text()
factory = (root / 'src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContextFactory.cs').read_text()
assert 'appsettings.json' in cli and 'appsettings.Development.json' not in cli
assert 'appsettings.json' in vs and 'appsettings.Development.json' not in vs
assert './src/Cfa.ACHInterbank.Api/appsettings.json:/app/appsettings.json:ro' in compose
assert 'ReadSettingFromFile(Path.Combine(basePath, "appsettings.json")' in factory
assert 'appsettings.Development.json' not in factory
print('PASS: fuente única, seguridad de defaults, consumidores y secciones trasladadas.')
