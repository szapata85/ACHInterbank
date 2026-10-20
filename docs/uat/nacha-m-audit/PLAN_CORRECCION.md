# Plan de corrección NACHA-M por ejecuciones independientes

Este plan no autoriza implementación, migraciones, seeds, transmisión ni cambio de estado productivo. Cada ejecución debe comenzar con aprobación de alcance y terminar con evidencia anonimizada y revisión humana.

## Orden recomendado

1. Cerrar documentos faltantes y aprobar matrices normativas.
2. **Ejecución 2:** corregir metamodelo, perfiles y motor table-driven.
3. **Ejecución 3:** corregir persistencia, secuencias, fechas e idempotencia.
4. **Ejecución 4:** construir pruebas normativas y Compliance Gate.
5. Sólo después: homologación UAT externa y decisión humana separada. No implica GO productivo.

## Precondición documental

Antes de codificar reglas CENIT se debe obtener:

- ficha técnica NACHA-M CENIT vigente y aplicable a CFA;
- Manual de Especificaciones del Formato STA citado por el Anexo 2;
- contrato/especificación de naming y dirección de los archivos `.OUT` de ACH Colombia;
- confirmación de vigencia/alcance del MAN-004 V32 para CFA;
- decisión aprobada sobre `ReferenceCode` y representación del ciclo.

Sin estas fuentes, sólo puede implementarse el bloqueo fail-closed, no valores inferidos.

## EJECUCIÓN 2 — Corrección del motor table-driven

### Objetivo

Convertir las matrices aprobadas en perfiles realmente normativos, cerrados y trazables, sin sustituir Opción C por lógica imperativa.

### Archivos probables

- Domain: `Models/ACH/Config/NachaConfigCoreEntities.cs` y configuraciones EF asociadas.
- Application: DTOs/contratos de config y trazabilidad.
- Persistence:
  - `NachaFileBuilder.cs`;
  - `NachaFixedWidthRecordRenderer.cs`;
  - `NachaConfigValidationService.cs`;
  - `NachaConfigResolver.cs`;
  - `Mapping/NachaCanonicalMapper.cs`;
  - `Seeders/NachaConfigOfficialProfilesSeeder.cs`;
  - naming rule seeder/policy;
  - estrategia T7 y validator semántico.
- Api/SPA de administración de perfiles, sólo si el metamodelo exige exponer nuevos atributos.

### Cambios table-driven

1. Añadir atributos first-class: `DataType`, `RequiredPolicy`, `AllowedValues`, normalizador, política de overflow/truncamiento, sensibilidad, estrategia de máscara, `RuleId`, entidad emisora, documento, versión, fecha, numeral, severidad y cross-rule.
2. Impedir publicación de perfil sin fuente completa para reglas críticas.
3. Ejecutar `CfgFieldRule` y reglas cruzadas antes de devolver bytes.
4. Corregir ACHCOL:
   - T1 fecha 8 y offsets 24–106;
   - T5 fechas 8, status/origen/lote/reservado;
   - T6 monto 18, ID/nombre, indicador 87, Trace 88–102, reservado;
   - T7 variantes por flujo y cruce con T6;
   - T8/T9 totales 18, lote y reservados;
   - nombre/FileId según MAN-004.
5. Bloquear CENIT hasta disponer de perfil homologado. Luego crear campos desde su propia fuente, no clonando ACHCOL.
6. Emitir T6 y sus T7 asociados en secuencia transaccional.
7. Hacer explícitos encoding, ausencia de EOL, record length y block factor por perfil.
8. Retirar truncamiento silencioso; overflow debe fallar con RuleId y sin valor sensible.
9. Hacer que cámara desconocida falle, en vez de caer por defecto a ACH.
10. TABLE_DRIVEN obligatorio para salida oficial; LEGACY sólo en tests/herramientas históricas aisladas.

### Cambios de seguridad

- Clasificar cuenta, identificación, nombre, NIT, valor, referencia, Trace, código cliente y correladores como sensibles.
- Trace: persistir longitud, patrón, hash o últimos cuatro cuando esté autorizado; nunca la línea reconstruible.
- Eliminar valores sensibles de errores y warnings; usar RuleId, campo, posición, correlación opaca.
- Retención y RBAC de auditoría; pruebas que impidan reconstrucción.

### Riesgos

- Cambiar snapshots que hoy codifican el layout erróneo.
- Romper flujos legacy/inbound si comparten modelos.
- Elegir una variante de T7 incorrecta sin decisión funcional.
- Incompatibilidad SPA/API si cambia DTO.

### Dependencias

- Matrices normativas firmadas.
- Aprobación de dueños ACH, CENIT, Compliance y Seguridad.
- Decisión sobre compatibilidad de perfiles ya persistidos.

### Pruebas

- Unitarias descriptor/renderer por campo y RuleId.
- Posiciones exactas T1/5/6/7/8/9.
- Negativas de overflow, caracteres, obligatorio, allowed values y source faltante.
- T6/T7 intercalado y cruce de sufijo.
- Totales/hash/bloques/padding en límites.
- Trace imposible de reconstruir.
- Perfil CENIT placeholder bloqueado.

### Criterios de aceptación

- 100% de reglas críticas ACHCOL en matriz tienen RuleId, fuente, implementación y prueba verde.
- Cero `Substring`/`PadLeft`/`PadRight` normativos fuera del renderer/core aprobado, salvo parser legacy aislado y deprecado.
- Cero truncamiento silencioso.
- Archivo sintético ACHCOL coincide byte a byte con oracle aprobado.
- CENIT no genera hasta que su perfil pase publicación normativa.
- Ninguna evidencia contiene datos sensibles completos.

### Rollback

- Feature flag de activación sólo para UAT, sin reactivar HYBRID en LIVE.
- Restaurar versión anterior del artefacto y perfil como histórico no publicado.
- No alterar golden existentes sin conservar versión y aprobación; crear nueva versión de perfil/snapshot.

## EJECUCIÓN 3 — Persistencia y consecutivos

### Objetivo

Separar inequívocamente lote por archivo, consecutivo externo diario, FileId, ciclo y fecha operacional, con idempotencia y concurrencia portables.

### Archivos probables

- Domain: `AchBatch`, `BatchNumberSequence`, modelos `ExternalFileNames`, nueva entidad de asignación por archivo si se aprueba.
- Persistence:
  - `DailyResetBatchNumberGenerator.cs`;
  - `BatchNumberSequenceStore.cs`;
  - `NachaFileBuilder.ResolveBatchNumberAssignmentAsync`;
  - services Postgres/SQL Server de filename;
  - configuraciones EF y `AchDbContext`;
  - migraciones separadas SQL Server/PostgreSQL;
  - parser/ingestion sólo para correlación, sin ampliar alcance funcional.

### Cambios de dominio/persistencia

1. Crear concepto `FileBatchOrdinal` que inicia 1 por archivo y se asigna en el orden normativo, independiente de PK y contador diario.
2. Conservar `ExternalDailySequence` para ZZZ/FileId con scope aprobado.
3. Persistir `OperationalDate`, `ClearingHouse`, `Participant`, `FileId/IdempotencyKey`, versión de perfil y ordinal de lote.
4. Reutilizar asignación sólo si coincide todo el scope.
5. Servicio de reloj y conversión `America/Bogota`; no usar `DateTime.Today` ni asumir `DateTime.Date` UTC.
6. Índices únicos para nombre/idempotency y lote dentro de archivo.
7. Límite de siete dígitos y agotamiento fail-closed.
8. SQL Server: transacción/locks y token de concurrencia verificados.
9. PostgreSQL: upsert/locking y token verificados.
10. Auditoría sin datos sensibles; rollback transaccional sin saltos reutilizables que causen duplicado funcional.

### Migraciones

- Sólo después de aprobación explícita.
- Generar migraciones equivalentes para ambos providers desde el modelo EF Code First.
- Revisar SQL, índices, defaults, tipos `date`/timestamp y downgrade.
- No seed de datos reales ni perfiles LIVE.

### Riesgos

- Colisión durante despliegue con filas existentes.
- Reintentos que consumen ZZZ o lote y dejan huecos.
- Diferencias de aislamiento SQL Server/PostgreSQL.
- Cambio de día durante generación larga.

### Dependencias

- Decisión normativa de scope por cámara.
- Identidad estable del archivo antes de asignar lotes.
- Política de retry y estado de generación.

### Pruebas

- Dos archivos del mismo día: cada lote inicia 1; ZZZ sí incrementa.
- Varios lotes por archivo y T5/T8.
- Reintento idempotente conserva nombre y ordinales.
- Concurrencia de 50+ reservas por provider.
- Rollback/crash antes y después de persistir.
- Cambio 23:59:59/00:00:00 Bogotá, UTC y DST no aplicable pero timezone válido.
- Cámara/participante/ciclo separados.
- Límite 36 de ACHCOL y límite de lote.

### Criterios de aceptación

- Sin colisiones ni duplicados en SQL Server/PostgreSQL.
- Mismo idempotency key produce mismo resultado; distinto archivo no reutiliza lote persistido.
- Lote ACHCOL inicia 1 por archivo y T5/T8 coinciden.
- Fecha operacional siempre deriva de America/Bogota.
- Migraciones up/down revisadas y backup/restore ensayado en entorno desechable.

### Rollback

- Migración reversible y script de verificación previo.
- Dual-read temporal sólo para UAT, sin doble asignación.
- Restaurar artefacto y esquema con downgrade probado; conservar auditoría.

## EJECUCIÓN 4 — Pruebas y Compliance Gate

### Objetivo

Convertir la matriz aprobada en un gate automatizado y evidencia reproducible por cámara antes de cualquier solicitud de GO.

### Archivos probables

- `tests/Cfa.ACHInterbank.Tests`:
  - nuevos tests normativos por RuleId;
  - nuevos golden sintéticos versionados por cámara/flujo;
  - properties/negative/concurrency/security tests.
- scripts de validación offline que sólo acepten fixtures sintéticos.
- Playwright para administración/publicación/aprobación, no para transmitir archivos.
- Docker Compose de test para SQL Server/PostgreSQL.
- documentación UAT y actas de aprobación.

### Cobertura

1. Física: bytes, encoding, BOM, EOL, 106, bloque, padding, último byte.
2. Estructural: todos los campos/posiciones/longitudes/alineación/relleno/allowed values.
3. Semántica: cámara, origen/destino, fecha/ciclo, SEC, transacción, descripción.
4. Cruzada: T5/T8, T6/T7, FileId/nombre, hash/totales/conteos.
5. Negativas: cada regla crítica debe fallar cerradamente y mostrar sólo RuleId/campo.
6. Propiedades: cualquier tamaño de lote dentro de límites, montos límite, caracteres.
7. Concurrencia/idempotencia para ambos providers.
8. Seguridad: trace/log/API no contienen datos completos y no reconstruyen registros.
9. Compatibilidad: parser genera el mismo modelo canónico desde golden aprobados.
10. Human gate: publicación requiere dos aprobadores y adjunta hash de matriz/evidencia.

### Riesgos

- Reusar archivos reales como golden.
- Actualizar snapshots automáticamente para “hacer pasar” pruebas.
- Confundir green build con compliance.

### Dependencias

- Ejecuciones 2 y 3 cerradas.
- Vectores sintéticos aprobados por cámaras/dueño funcional.
- Entornos locales desechables y secretos de test no reales.

### Criterios de aceptación

- Build Release sin warnings/errores.
- Suite completa verde en unit/integration offline.
- Matriz: ninguna regla crítica en `No cumple`, `No demostrado` o `Pendiente de definición oficial`.
- Evidencia byte a byte firmada/hash, sin datos sensibles.
- UAT externa/homologación registrada por cámara.
- Aprobación humana de Operaciones, Arquitectura, Seguridad y Compliance.

### Criterio de GO/NO-GO

- Cualquier regla crítica sin fuente o prueba mantiene **NO-GO**.
- Un build verde nunca cambia por sí solo la decisión.
- GO debe emitirse por cámara y flujo, con vigencia y versión concretas.

### Rollback

- Revertir sólo artefactos de la ejecución y perfil no publicado; conservar evidencia/auditoría.
- Invalidar el gate si cambia cualquier hash de norma, perfil o golden.
- Volver a NO-GO automáticamente ante regresión crítica.

