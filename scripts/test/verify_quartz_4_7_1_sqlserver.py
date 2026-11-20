#!/usr/bin/env python3
"""Verificación estática del esquema Quartz 4.7.1 incluido en EF Core SQL Server."""
from pathlib import Path
import re

root = Path(__file__).resolve().parents[2]
sql_file = root / 'artifacts/sql/quartz/sqlserver-qrtz-schema.sql'
embedded_sql_file = root / 'src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/Sql/QuartzSqlServer471.sql'
migration_file = root / 'src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261118110000_QuartzSqlServer471Schema.cs'
project_file = root / 'src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj'
sql = sql_file.read_text(encoding='utf-8')
embedded_sql = embedded_sql_file.read_text(encoding='utf-8')
migration = migration_file.read_text(encoding='utf-8')
project = project_file.read_text(encoding='utf-8')
compose = (root / 'docker-compose.yml').read_text(encoding='utf-8')
cluster = (root / 'docker-compose.scheduler-cluster.sqlserver.yml').read_text(encoding='utf-8')
cli = (root / 'tools/AchInterbank.DockerCli/Program.cs').read_text(encoding='utf-8')

creates = re.findall(r'CREATE TABLE \[dbo\]\.\[(QRTZ_[^]]+)\]', embedded_sql)
adds = re.findall(r'ALTER TABLE \[dbo\]\.\[(QRTZ_[^]]+)\] ADD \[([^]]+)\] ([^;]+);', embedded_sql)
guards = re.findall(r"IF COL_LENGTH\(N'dbo\.(QRTZ_[^']+)', N'([^']+)'\) IS NULL\s*BEGIN\s*ALTER TABLE", embedded_sql)
assert len(creates) == len(set(creates)) == 12, f'Tablas Quartz: {len(creates)}'
assert 'QRTZ_PAUSED_JOB_GRPS' in creates
assert len(adds) == len(guards) == 26, f'Columnas Quartz: {len(adds)}'
assert all((table, col) in guards for table, col, _ in adds)
assert embedded_sql.count('THROW 51005') == len(adds)
assert not re.search(r'\bDROP\s+(?:TABLE|DATABASE)\b', embedded_sql, re.I)
assert sql == embedded_sql, 'SQL original y snapshot de la migracion difieren: revisar/versionar una migracion nueva'
assert 'EmbeddedResource Include="Sql/QuartzSqlServer471.sql"' in project
assert 'LogicalName="ACHInterbank.Migrations.QuartzSqlServer471.sql"' in project
assert '[Migration("20261118110000_QuartzSqlServer471Schema")]' in migration
assert 'GetManifestResourceStream(ResourceName)' in migration
assert 'SplitSqlServerBatches(script)' in migration
assert 'migrationBuilder.Sql(batch)' in migration
assert 'migrationBuilder.DropTable' not in migration
assert 'quartz-schema:' not in compose
assert 'quartz-schema:' not in cluster
assert 'service_completed_successfully' not in compose
assert 'service_completed_successfully' not in cluster
assert 'Database__ApplyMigrations: "${DATABASE_APPLY_MIGRATIONS:-true}"' in compose
assert 'Database__ApplyMigrations: "true"' in cluster
assert 'sqlserver:' in compose and 'condition: service_healthy' in compose
assert 'scheduler-sqlserver:' in cluster and 'condition: service_healthy' in cluster
assert 'EnsureSqlServer(root, composeEnv);' in cli
assert 'EnsureQuartz(' not in cli and '"--force-recreate", "quartz-schema"' not in cli
assert 'PerformSchemaValidation: "true"' in compose

# GO aparece como directiva de lote en el script y nunca debe llegar a SQL Server.
batches = re.split(r'(?im)^\s*GO\s*$', embedded_sql)
assert len([b for b in batches if b.strip()]) > 20
assert all(not re.search(r'(?im)^\s*GO\s*$', b) for b in batches if b.strip())
print('OK: migración EF Core embebida, 12 tablas, 26 columnas, sin contenedor Quartz (SQL Server).')
