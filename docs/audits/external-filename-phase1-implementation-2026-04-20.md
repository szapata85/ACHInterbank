# ExternalFileNamePolicy — Implementación fase 1 segura (2026-04-20)

## 1. Reglas implementadas (HARD BLOCK)

### ACH (confirmadas por normativa)
1. Patrón de nombre: `RRRRTTT.ZZZ.1`.
2. Correlación `ZZZ` con campo 7 del Registro 1.
3. Tabla A-Z/0-9 para secuencia 001-036 (vía `INachaFileIdentifierMapService`).
4. Límite de 36 archivos diarios.
5. Regla PSE cuando aplica: rango 4..9 en campo 7 y correspondencia con nombre.

### CENIT/STA rechazo (confirmadas)
1. Campo 6 (número de registros de detalle) obligatorio para validación.
2. D04 duplicado por nombre.
3. D05 mismatch entre conteo declarado en nombre y contenido.

## 2. Reglas WARNING
1. Naming STA fuera de rechazo (sin bloqueo).
2. Duplicidad ACH por nombre externo completo fuera de alcance explícito normativo (sin bloqueo duro).
3. Reglas PSE no completamente cerradas por manual adicional.

## 3. Reglas AUDIT ONLY
1. Cámaras/flujos sin cierre normativo explícito para enforcement duro.
2. Correlaciones parciales que requieren confirmación normativa.

## 4. Reglas NO IMPLEMENTADAS (fase 1)
1. Enforcements STA completos fuera de rechazo.
2. Duplicidad universal ACH por nombre externo.
3. Reglas PSE derivadas de manuales no incorporados al repositorio.

## 5. Arquitectura aplicada (Clean Architecture)
- Application:
  - Interfaces de política, builder, validator, sequence, duplicate guard, auditoría y correlación.
- Domain:
  - Contratos de contexto/resultado/evidencia + enums de disposición.
  - Entidades persistentes `ExternalFileSequence`, `ExternalFileNameRegistry`, `ExternalFileNameValidationLog`.
- Persistence:
  - Implementaciones de servicios + configuraciones EF Core.
- API/Flujos:
  - Integración outbound en `NachaExportController`.
  - Integración inbound en `IncomingNachaIngestionAppService`.

## 6. Entidades y tablas agregadas
- `ExternalFileSequences`
- `ExternalFileNameRegistry`
- `ExternalFileNameValidationLog`

> Sin migración en esta fase; se mantiene Code First listo para generar migración en fase controlada.

## 7. Pruebas creadas
- `ExternalFileNamePolicyPhase1Tests` (unit + integration ligeras sobre SQLite/InMemory):
  1. Builder ACH genera `RRRRTTT.ZZZ.1`.
  2. Validator ACH bloquea mismatch ZZZ↔R1.
  3. Validator ACH aplica límite 36.
  4. Validator PSE aplica rango 4..9 cuando corresponde.
  5. Validator CENIT/STA rechazo aplica D05.
  6. Validator CENIT/STA rechazo aplica D04.
  7. WARNING no bloquea.
  8. AUDIT ONLY no bloquea.
  9. Persistencia en registry y validation log.
  10. Secuencia por scope.

## 8. Ejecución real
Comandos:
- `dotnet build ACHInterbank.sln -c Release`
- `dotnet test ... --filter "FullyQualifiedName~ExternalFileNamePolicyPhase1Tests|FullyQualifiedName~NachaExportControllerTests|FullyQualifiedName~IncomingNachaIngestionAppServiceTests"`
- `dotnet test ... --filter "FullyQualifiedName~BatchNumber|FullyQualifiedName~NachaFileBuilder|FullyQualifiedName~Mapping"`
- `dotnet test ... --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber"`

Resultados:
- Build: OK.
- Tests filename + integración mínima: 20/20 OK.
- Núcleo no-regresión: 60/60 OK.
- Filtro amplio NACHA/Mapping/BatchNumber: 154/154 OK.

## 9. Riesgos pendientes
- Estructura STA completa fuera de rechazo continúa en warning/audit.
- Reglas PSE completas siguen pendientes del manual adicional.
- No se ejecutó harness PostgreSQL en esta fase.
