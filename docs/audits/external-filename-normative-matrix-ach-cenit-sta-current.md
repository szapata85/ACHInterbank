# Matriz oficial vigente de naming externo ACH/CENIT/STA

**Fecha de consolidación:** 2026-05-15 (UTC)
**Estado operativo:** Vigente para diagnóstico, UAT controlado y diseño de remediación.
**Decisión de readiness:** **NO-GO productivo** para naming externo hasta cierre normativo + UAT + firma.

> Este documento **reemplaza operativamente** a:
> - `docs/audits/external-filename-normative-matrix-ach-cenit-sta-2026-04-20.md`
> - `docs/audits/external-filename-normative-matrix-ach-cenit-sta-2026-04-20-v2.md`
>
> Ambos documentos quedan como **histórico de auditoría**.

---

## 1. Propósito

Consolidar en una sola fuente vigente las reglas de naming externo por cámara/rail y flujo, diferenciando:
- lo **confirmado normativamente**,
- lo **confirmado técnicamente en código**,
- lo **pendiente de confirmación normativa**,
- y lo **interno/no externo**.

---

## 2. Alcance

Incluye análisis documental y técnico para:
- transacciones salientes;
- devoluciones salientes;
- devolución de devolución productiva;
- devolución de devolución audit-mode interno;
- rechazos (total/parcial);
- respuestas;
- archivos STA;
- incoming recibido;
- sobre digital (cuando el nombre salga a intercambio externo).

---

## 3. Fuentes revisadas

| Documento | Versión | Fecha | Ruta repo | Sección/página identificada | Nivel de confianza |
|---|---|---|---|---|---|
| Manual ACH Colombia | V32 | 2025-01 | `docs/normativa/md/ACH-Colombia-V32.md` | Referencias de auditoría previa a 6.1.10.1/6.1.10.2/6.1.10.3 (naming) | Fuente primaria |
| CENIT Anexo A causales devolución | Asunto 1, Anexo A | 2023-11-28 | `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md` | Tabla causales Rxx (no naming explícito) | Fuente primaria |
| Matriz naming externa (v1) | Congelada v1 | 2026-04-20 | `docs/audits/external-filename-normative-matrix-ach-cenit-sta-2026-04-20.md` | Diagnóstico inicial y gaps | Histórico |
| Matriz naming externa (v2) | Consolidación v2 | 2026-04-20 | `docs/audits/external-filename-normative-matrix-ach-cenit-sta-2026-04-20-v2.md` | Cierre parcial con reglas ACH/CENIT/STA | Matriz interna |
| Auditoría técnica devoluciones | estado 2026-05-15 | 2026-05-15 | `docs/dev/devoluciones-ach-auditoria-cenit-ach-colombia.md` | Sección ROR + no regresión | Fuente secundaria |
| Matriz maestra S1 | 2026-04-26 | 2026-04-26 | `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md` | S1-12 y estado global | Matriz interna |
| Go/No-Go scorecard | 2026-04-26 | 2026-04-26 | `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md` | fila naming externo NO-GO crítico | Matriz interna |

---

## 4. Convenciones de estado

- **Confirmado normativamente**: regla soportada por fuente normativa primaria identificada.
- **Confirmado técnicamente**: regla observada implementada en código.
- **Inferido**: deducción técnica/documental no cerrada por norma primaria explícita.
- **Pendiente confirmación normativa**: no hay soporte normativo suficiente para bloqueo productivo.
- **Interno/no externo**: convención válida para evidencia/proceso interno, no para intercambio cámara.
- **Bloqueado productivo**: no habilitable en Go-Live hasta cierre normativo/operativo.

---

## 5. Matriz por cámara y flujo

> Regla de auditoría: **no inventar patrones** cuando no exista confirmación normativa explícita.

| Cámara/Rail | Flujo | Patrón esperado | Extensión | Campos obligatorios | Ejemplo sintético seguro | Implementación actual | Estado | Riesgo | Fuente documental | Próxima acción |
|---|---|---|---|---|---|---|---|---|---|---|
| ACH Colombia | Transacción normal saliente | `RRRRTTT.ZZZ.1` (según matriz v2) | sin sufijo adicional (en estándar citado) | entidad origen (RRRRTTT), secuencia ZZZ, correlación R1/FileId | `1234567.001.1` | `ExternalFileNamePolicy` + `ExternalFileNameBuilder` (ACH) | Confirmado normativamente + técnicamente (parcial por cobertura de flujo) | Medio | v2 + V32 | Validar aceptación real de cámara en UAT |
| CENIT | Transacción normal saliente | Pendiente estructura única consolidada en este repo | Pendiente | secuencia/fecha/código cámara por confirmar | `N/A` | `NachaExportController` usa legado para no-ACH + policy | Pendiente confirmación normativa | Alto | v1/v2 | Cerrar estructura oficial por cámara |
| ACH/CENIT | Devolución saliente | No confirmada como naming externo final | `.RET` (actual técnico) | cycleId + timestamp (técnico actual) | `RET_cycle-1_20260515120000.RET` | Hardcode `RET_{cycleId}_{timestamp}.RET` | Confirmado técnicamente / Bloqueado productivo | Crítico | auditoría técnica + código | Centralizar en policy por cámara |
| ACH/CENIT | Devolución de devolución productiva | No confirmada como naming externo final de cámara | `.ach` (actual técnico) | prefijo técnico + cámara + timestamp | `RORNACHA_7001_20260515120000.ach` | Prefijo `RORNACHA_` usado como convención técnica de generación productiva | Confirmado técnicamente / Pendiente normativo / Bloqueado | Crítico | auditoría técnica + código | Confirmación explícita cámara/operador |
| Interno | Devolución de devolución audit-mode | **Interno/no externo** | `.ach` (payload interno) | cámara + timestamp + líneas `ROR|`,`FLOW|` | `ROR_7001_20260515120000.ach` | Hardcode `ROR_{clearingHouseId}_{timestamp}.ach` | Interno/no externo | Medio (si se confunde con externo) | auditoría técnica + código | Mantenerlo fuera de contrato externo |
| STA | Rechazo total/parcial | Confirmado parcialmente para rechazo (D04/D05/campo conteo según matrices) | `.txt` (modelo técnico `STA.REJECT`) | conteo declarado, duplicidad, correlación conteo | `STA.REJECT.000123.txt` | `ExternalFileNameBuilder` soporta `StaReject` | Confirmado técnicamente + pendiente cierre integral normativo | Alto | v1/v2 | Congelar alcance: solo rechazo confirmado |
| CENIT/ACH | Respuesta | No encontrado consolidado | Pendiente | Pendiente | `N/A` | No identificado como política externa única | Pendiente | Alto | v1/v2 + scorecard | Levantar fuente normativa específica |
| CENIT/ACH | Operador (devolución por operador) | Pendiente de confirmación | Pendiente | causal/flujo/seq por confirmar | `N/A` | sin política unificada visible en devoluciones | Pendiente | Alto | V32 + auditorías | Definir matriz por flujo operador |
| ACH/CENIT | Incoming recibido | Debe validar nombre recibido (sin redefinir naming externo emisor) | según archivo recibido | nombre provisto + hash + tamaño + correlación | `entrante.ach` | `IncomingNachaIngestionAppService` usa policy para validar y registrar | Confirmado técnicamente | Medio | código + v1/v2 | Completar reglas hard por cámara |
| ACH/CENIT | Sobre digital | Si es salida externa: nombre base + `.ENV` sujeto a policy base | `.ENV` | nombre base externo válido + sobre | `1234567.001.1.ENV` | Export NACHA cifra con `filePolicy.ExternalFileName + .ENV`; flujo manual usa nombres operativos | Confirmado técnicamente / Pendiente normativo integral | Alto | código + scorecard | Separar manual interno vs envío cámara |
| STA | Archivos STA no rechazo | No confirmado en esta consolidación | Pendiente | pendiente | `N/A` | no regla completa en política actual | Pendiente / Bloqueado productivo | Crítico | v1/v2 | completar fuente normativa primaria |

### Notas obligatorias de gobernanza

1. `RORNACHA_` se clasifica como **convención técnica actual**, **no confirmada** como naming externo definitivo de cámara.
2. `RET_{cycleId}_{timestamp}.RET` se clasifica como **hardcode técnico actual**, pendiente de centralización y validación normativa por cámara.
3. El archivo ROR audit-mode (`ROR_...` con contenido `ROR|`/`FLOW|`) es **interno/no externo** y no debe usarse para intercambio con cámara.

---

## 6. Matriz implementación actual vs objetivo

| Flujo | Clase actual | Método actual | Patrón actual | Usa ExternalFileNamePolicy | Debe usar ExternalFileNamePolicy | Brecha | Prioridad |
|---|---|---|---|---|---|---|---|
| NACHA saliente | `NachaExportController` | `Export`, `ExportEncrypted` | ACH vía policy; no-ACH parte de legado + policy | Sí (parcial por coexistencia legado) | Sí | Homogeneizar por cámara sin legado ambiguo | P1 |
| Incoming recibido | `IncomingNachaIngestionAppService` | `IngestAsync` | nombre provisto validado por policy | Sí | Sí | Endurecer reglas hard por cámara | P1 |
| Devolución saliente | `AchReturnsService` | `GenerateReturnsFileAsync` | `RET_{cycleId}_{timestamp}.RET` | No | Sí | Hardcode fuera de policy | P0 |
| ROR audit-mode | `AchReturnOfReturnFileGenerationService` | `GenerateAsync` | `ROR_{clearingHouseId}_{timestamp}.ach` | No | No (interno) | Riesgo de confusión con externo | P2 |
| ROR productivo | `AchReturnOfReturnFileGenerationService` | `GenerateNachaAsync` | convención técnica `RORNACHA_...` | No uniforme | Sí | Falta política externa formal por cámara | P0 |
| Política central | `ExternalFileNamePolicy` | `GenerateExternalNameAsync` | builder+validator+audit | Sí | Sí | ampliar cobertura de flujos | P1 |
| Builder | `ExternalFileNameBuilder` | `BuildAsync` | ACH + STA reject + fallback interno | Sí | Sí | ampliar tipos/flows y eliminar fallback ambiguo para externos | P1 |
| Soporte | `ExternalFileNameSupport` | Parse/build helpers | regex ACH + STA reject | Sí | Sí | cobertura limitada para otros flujos | P1 |
| Sobre digital operativo | `NachaSecurityOperationService` | `GenerateNachaInternalAsync`, `ManualEncryptAsync` | generado: base policy + `.ENV`; manual: nombre operativo | Parcial | Sí para salida externa / No para manual interno | separar frontera interno vs externo | P1 |

---

## 7. Decisiones normativas pendientes

### CENIT
- Confirmar naming externo para devoluciones salientes.
- Confirmar naming externo para ROR productivo.
- Confirmar naming de rechazos/respuestas fuera del alcance parcial STA ya validado en matrices históricas.
- Confirmar dependencia de ciclos/liquidación en campos del nombre (fecha/secuencia).

### ACH Colombia
- Ratificar patrón V32 aplicable por flujo y por rail operativo vigente.
- Confirmar alcance exacto de correlación con R1 para devoluciones/ROR/operador.
- Confirmar política de secuencia para subflujos fuera de originación estándar.
- Confirmar naming de operador/devolución/ROR para aceptación externa.

### STA
- Mantener confirmado el alcance de rechazo (D04/D05/correlación de conteo) según matrices previas.
- No extender a otros flujos STA sin fuente normativa primaria explícita en este repo.

---

## 8. Reglas internas vs externas

### Nombres internos permitidos
- Artefactos de auditoría técnica (`ROR_...`, payload `ROR|...` y `FLOW|...`).
- Nombres operativos de cifrado/descifrado manual para uso interno de soporte.

### Nombres externos productivos
- Solo aquellos con patrón normativo confirmado por cámara y flujo, y validados por política central.

### Nombres temporales
- Prefijos/prototipos técnicos (`RORNACHA_`, `RET_{cycleId}_{timestamp}.RET`) mientras no exista cierre normativo final.

### Nombres no aptos para intercambio
- Cualquier nombre interno/audit-mode.
- Cualquier patrón técnico no confirmado por cámara.

---

## 9. Criterios de aceptación para salir de NO-GO naming

- [ ] Patrón confirmado por cámara y flujo (ACH/CENIT/STA).
- [ ] Implementación centralizada de naming externo en flujos outbound críticos.
- [ ] Golden tests por cámara/flujo con evidencia reproducible.
- [ ] Duplicidad externa validada y auditada en `ExternalFileNameRegistry`.
- [ ] Evidencia UAT de archivos aceptados por contraparte/cámara.
- [ ] Firma negocio + compliance + operaciones.

---

## 10. Plan de remediación por commits

1. `docs(naming): consolidate external filename matrix by rail`
2. `refactor(naming): centralize outbound returns filename policy`
3. `feat(naming): add ROR productive filename policy`
4. `test(naming): add golden tests for external filenames by rail`
5. `feat(naming): validate duplicate external filenames`
6. `docs(uat): add filename acceptance checklist`

---

## 11. Vigencia y gobernanza documental

- Este documento es la **fuente de referencia vigente** para UAT y para cambios futuros de código de naming.
- Las matrices v1/v2 permanecen como **histórico** de auditoría.
- La vigencia de este documento **no implica GO productivo**.
- Se mantiene decisión: **NO-GO productivo para naming externo** hasta cierre normativo, técnico y operativo.
