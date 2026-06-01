# Fase 6A - Evaluacion seeds y migraciones

Los catalogos `CatClearingHouse`, `CatFlowType`, `CatDirection`, `CatRecordCode`, `CatConfigStatus`, `CatDataSourceType` y `CatRuleType` tienen seeds deterministicos de catalogo.

El seeder `NachaConfigBackfillSeeder`:

- crea perfiles solo si no existen perfiles;
- backfillea desde legacy;
- crea perfil `LEGACY_ACH_SALIDA_ORIGINAL_V1_0`;
- usa ACH, flow ORIGINAL, direction SALIDA;
- usa timestamps runtime (`DateTime.UtcNow`);
- no crea perfil CENIT completo;
- no representa seed oficial deterministico de Opcion C.

| Pregunta | Respuesta |
|---|---|
| Existen perfiles publicados para ACH Colombia | Parcial por backfill legacy |
| Existen perfiles publicados para CENIT | No evidenciado |
| Son deterministas | Catalogos si; profiles backfill no |
| Son idempotentes | Backfill evita duplicar si existen perfiles, pero no cubre escenarios parciales |
| Cubren registros 1/5/6/7/8/9 | Parcial, depende de legacy |
| Tienen fields completos | Parcial, depende de legacy |
| Seeds faltantes | Perfiles oficiales ACH y CENIT, variants, fields, rules, versiones normativas |

Opcion C no debe depender de un backfill runtime legacy. Debe tener perfiles oficiales controlados, versionados y revisables por camara.

