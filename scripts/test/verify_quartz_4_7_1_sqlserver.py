#!/usr/bin/env python3
"""Chequeo estático del bootstrap Quartz SQL Server 4.7.1 (sin Docker/SQL)."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
sql = (root / 'artifacts/sql/quartz/sqlserver-qrtz-schema.sql').read_text(encoding='utf-8')
compose = (root / 'docker-compose.yml').read_text(encoding='utf-8')
cli = (root / 'tools/AchInterbank.DockerCli/Program.cs').read_text(encoding='utf-8')

creates = re.findall(r'CREATE TABLE \[dbo\]\.\[(QRTZ_[^]]+)\]', sql)
adds = re.findall(r'ALTER TABLE \[dbo\]\.\[(QRTZ_[^]]+)\] ADD \[([^]]+)\] ([^;]+);', sql)
guards = re.findall(r"IF COL_LENGTH\(N'dbo\.(QRTZ_[^']+)', N'([^']+)'\) IS NULL\s*BEGIN\s*ALTER TABLE", sql)
assert len(creates) == len(set(creates)) == 12, f'Cantidad de tablas: {len(creates)}'
assert 'QRTZ_PAUSED_JOB_GRPS' in creates, 'Falta tabla nueva de Quartz 4.x'
assert len(adds) == 26, f'Columnas adicionadas: {len(adds)}'
assert len(guards) == len(adds), 'Todas las ampliaciones deben estar protegidas'
assert all((table, col) in guards for table, col, _ in adds)
assert sql.count('THROW 51005') == len(adds), 'Falta validacion de columnas'
assert not re.search(r'\bDROP\s+(?:TABLE|DATABASE)\b', sql, re.I), 'DDL destructivo'
assert 'artifacts/sql/quartz/sqlserver-qrtz-schema.sql:/schema/quartz.sql:ro' in compose
assert '"--force-recreate", "quartz-schema"' in cli, 'Debe reejecutarse el esquema en infra/migrate'
assert '<> 12' in sql and 'PerformSchemaValidation: "true"' in compose
print('OK: 12 tablas, 26 columnas protegidas, validaciones y Docker verificados (estatico).')
