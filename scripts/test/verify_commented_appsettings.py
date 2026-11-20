#!/usr/bin/env python3
"""Guardas de comentarios, valores críticos y consumidores JSONC de ACHInterbank."""
from pathlib import Path
import re
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from jsonc_settings import load_jsonc

root = Path(__file__).resolve().parents[2]
p = root / 'src/Cfa.ACHInterbank.Api/appsettings.json'
s = p.read_text(encoding='utf-8')
settings = load_jsonc(p)
# Cada entrada JSON que sea una clave va precedida por un comentario de nivel correspondiente.
lines = s.splitlines()
key_pattern = re.compile(r'^([ \t]*)"[^"\\]+"[ \t]*:')
keys = [i for i, line in enumerate(lines) if key_pattern.match(line)]
assert len(keys) >= 200, f'Inventario de claves incompleto: {len(keys)}'
for index in keys:
    i = index - 1
    while i >= 0 and not lines[i].strip():
        i -= 1
    assert i >= 0 and lines[i].lstrip().startswith('// '), f'Falta comentario en línea {index+1}'
assert settings['Database'] == {'Provider': 'SqlServer', 'ApplyMigrations': True, 'ApplySeed': False}
assert settings['Quartz']['JobStore']['Mode'] == 'Persistent'
assert settings['Quartz']['JobStore']['TablePrefix'] == 'QRTZ_'
assert settings['Resilience']['ExternalHttp']['Soap']['MaxRetryAttempts'] == 0
assert settings['NachaInboundSimulator']['Enabled'] is False
assert settings['SoapIntegrationBootstrap']['OverwriteExistingSettings'] is False
assert settings['SoapTransport']['ControlledLocal']['TransportHost'] == ''
assert settings['ConnectionStrings']['SqlConnection'] == ''
assert settings['appSettings']['tokenManager']['secretKetJwt'] == ''

cli = (root / 'tools/AchInterbank.DockerCli/Program.cs').read_text()
publisher = (root / 'scripts/iis/publish-iis.ps1').read_text()
helper = (root / 'scripts/jsonc-settings.ps1').read_text()
vs = (root / 'scripts/configure-visualstudio-development.ps1').read_text()
factory = (root / 'src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContextFactory.cs').read_text()
assert 'JsonCommentHandling.Skip' in cli and 'JsonCommentHandling.Skip' in factory
assert 'Read-JsoncSettings' in publisher and 'Set-JsoncSetting' in publisher
assert 'Read-JsoncSettings' in vs and 'Set-JsoncSetting' in vs
assert 'ConvertTo-Json -Depth 100' not in publisher
assert 'ConvertTo-Json -Depth 100' not in vs
assert '^[ \\t]*//' in helper
print(f'PASS: {len(keys)} claves documentadas, JSONC válido y consumidores adaptados.')
