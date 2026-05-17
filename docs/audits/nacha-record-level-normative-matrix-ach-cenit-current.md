# Matriz normativa vigente — Registros NACHA-M ACH Colombia / CENIT

## 1. Propósito
Esta matriz consolida la revisión campo-a-campo de los registros NACHA-M:
- 1 File Header
- 5 Batch Header
- 6 Entry Detail
- 7 Addenda
- 8 Batch Control
- 9 File Control

Cubre:
- devolución saliente;
- ROR productivo;
- ROR audit-mode **solo como interno/no externo**;
- validaciones de totales, hash, block count y padding.

## 2. Estado actual
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- La generación 1/5/6/7/8/9 existe técnicamente en servicios actuales.
- La aceptación productiva depende de validación campo-a-campo por cámara.
- CENIT Anexo A confirma principalmente causales Rxx, no necesariamente el layout completo por flujo.
- ACH Colombia V32 debe usarse para validación de layout donde aplique.

## 3. Fuentes revisadas

| Documento | Ruta | Cámara | Tipo de fuente | Alcance | Nivel de confianza | Observaciones |
|---|---|---|---|---|---|---|
| ACH Colombia V32 | `docs/normativa/md/ACH-Colombia-V32.md` | ACH | Fuente primaria | Reglas ACH/NACHA y operación | Alta (para ACH) | Requiere trazabilidad campo específico por flujo |
| CENIT Anexo A Causales | `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md` | CENIT | Fuente primaria | Causales Rxx | Alta (causales) | No extrapolar a layout NACHA completo |
| Matriz naming externo vigente | `docs/audits/external-filename-normative-matrix-ach-cenit-sta-current.md` | ACH/CENIT/STA | Matriz interna | Naming externo | Media/Alta | Mantiene NO-GO productivo de naming |
| Checklist UAT naming | `docs/uat/naming-returns-ror-acceptance-checklist.md` | ACH/CENIT/STA | Checklist UAT | Aceptación funcional/técnica/normativa | Media | Complementa, no cierra normativa final |
| Auditoría devoluciones/ROR | `docs/dev/devoluciones-ach-auditoria-cenit-ach-colombia.md` | ACH/CENIT | Auditoría técnica | Estado de implementación y riesgos | Media | Expone hardcodes y pendientes GO productivo |
| Matriz maestra trazable | `docs/audits/s1-matriz-maestra-trazable-funcional-normativa-2026-04-26.md` | Global | Auditoría | Gobierno de cierre normativo | Media | Referencia de control de salida NO-GO |
| Scorecard GO/NO-GO | `docs/audits/go-nogo-scorecard-funcional-normativo-2026-04-26.md` | Global | Auditoría | Decisión operativa | Alta | Mantiene NO-GO productivo |
| Código generador returns | `src/.../AchReturnsService.cs` | ACH/CENIT | Código de referencia | Implementación real 1/5/6/7/8/9 | Alta (técnico) | No sustituye fuente normativa |
| Código generador ROR | `src/.../AchReturnOfReturnFileGenerationService.cs` | ACH/CENIT | Código de referencia | Implementación real 1/5/6/7/8/9 | Alta (técnico) | Presenta hardcodes |

## 4. Convenciones de estado
- **Confirmado normativamente:** regla explícita en fuente primaria para cámara/flujo/campo.
- **Confirmado técnicamente:** implementado en código y/o test.
- **Inferido:** deducido por estructura NACHA o consistencia técnica sin cierre normativo explícito.
- **Pendiente confirmación normativa:** requiere validación formal de cámara/operador.
- **No encontrado:** no se identificó regla suficiente en fuentes revisadas.
- **Interno/no externo:** aplica solo a artifacts internos (ej. ROR audit-mode).
- **Bloqueado productivo:** aunque exista implementación, no hay autorización de salida productiva.

## 5. Matriz por registro y cámara

### 5.1 Registro 1 — File Header

| Cámara | Flujo | Campo | Regla esperada | Implementación actual | Origen actual del valor | Estado de certeza | Riesgo | Fuente | Próxima acción |
|---|---|---|---|---|---|---|---|---|---|
| ACH | Devolución saliente | Immediate Destination | Código entidad destino válido | `ImmediateDestinationAchColombia` fijo | hardcoded | Pendiente confirmación normativa | Alto | ACH V32 + código | Validar valor con cámara/operador |
| CENIT | Devolución saliente | Immediate Destination | Código destino por cámara/flujo | Hereda constante actual | hardcoded | Pendiente confirmación normativa | Alto | Matriz current + código | Definir por `ClearingHouseId` |
| ACH/CENIT | Devolución/ROR | Immediate Origin | Código origen válido | Usa `ClearingHouse.OriginCode` normalizado | ClearingHouse | Confirmado técnicamente | Medio | Código | Confirmación normativa por cámara |
| ACH/CENIT | Devolución/ROR | File Creation Date/Time | Fecha/hora de generación | `yyMMdd` + `HHmm` | cálculo | Confirmado técnicamente | Bajo | Código | Verificar timezone operativo |
| ACH/CENIT | Devolución/ROR | File ID Modifier | Secuencia/modificador diario válido | `A` fijo en implementación auditada | hardcoded | Pendiente confirmación normativa | Medio | Código | Evaluar estrategia secuencial por cámara |
| ACH/CENIT | Devolución/ROR | Record Size / Blocking Factor / Format Code | 094 / 10 / 1 | Constantes en builder | hardcoded | Confirmado técnicamente / Inferido normativo | Bajo | Código + práctica NACHA | Validar trazabilidad documental |
| ACH | Devolución saliente | Immediate Destination Name | Nombre entidad destino | `ACH-RET` | hardcoded | Pendiente confirmación normativa | Medio | Código | Parametrizar por cámara si aplica |
| ACH/CENIT | Devolución saliente | Immediate Origin Name | Nombre banco origen | `ClearingHouse.Name` o default | ClearingHouse | Confirmado técnicamente | Medio | Código | Confirmar longitud/semántica por cámara |
| ACH | ROR productivo | Reference Code | Referencia cabecera | `ACHINTERBANK ROR` / fecha / `0001` | hardcoded | Pendiente confirmación normativa | Alto | Código | Definir mapping por cámara/flujo |

### 5.2 Registro 5 — Batch Header

| Cámara | Flujo | Campo | Regla esperada | Implementación actual | Origen actual del valor | Estado de certeza | Riesgo | Fuente | Próxima acción |
|---|---|---|---|---|---|---|---|---|---|
| ACH/CENIT | Devolución/ROR | Service Class Code | 200/220/225 según mezcla débito/crédito | Calculado por transaction codes | cálculo | Confirmado técnicamente | Medio | Código | Validar matriz por cámara |
| ACH/CENIT | Devolución saliente | Company Name | Nombre de compañía autorizado | `DEVOLUCIONES` fijo | hardcoded | Pendiente confirmación normativa | Alto | Código | Parametrizar por cámara/flujo |
| ACH/CENIT | Devolución/ROR | Company Identification | Identificador compañía válido | Returns: parámetro fijo de servicio; ROR: `BANCROR` | hardcoded/config | Pendiente confirmación normativa | Alto | Código | Catalogar por cámara |
| ACH/CENIT | Devolución/ROR | Standard Entry Class Code | SEC válido (ej. PPD) | Returns `PPD`; ROR implícito en texto armado | hardcoded | Pendiente confirmación normativa | Medio | Código | Confirmar SEC por flujo |
| ACH/CENIT | Devolución saliente | Company Entry Description | Descripción permitida | `RETORNO` fijo | hardcoded | Pendiente confirmación normativa | Alto | Código | Parametrizar por flujo |
| ACH/CENIT | ROR productivo | Company Entry Description | Descripción ROR válida | `DEV. DEV.` fija | hardcoded | Pendiente confirmación normativa | Alto | Código | Validar con cámara |
| ACH/CENIT | Devolución/ROR | Company Descriptive Date / Effective Entry Date | Fecha aplicable | Usa fecha de generación/proceso | cálculo/AchCycle | Inferido | Medio | Código | Confirmar regla por cámara |
| ACH/CENIT | Devolución/ROR | Settlement Date | campo reservado según norma | `000` en returns / implícito en ROR | hardcoded | Inferido | Medio | Código | Confirmar si aplica por cámara |
| ACH/CENIT | Devolución/ROR | Originator Status Code | código originador válido | `1` fijo | hardcoded | Pendiente confirmación normativa | Medio | Código | Confirmar con cámara |
| ACH/CENIT | Devolución/ROR | Originating DFI | DFI de origen | `OriginCode` normalizado | ClearingHouse | Confirmado técnicamente | Medio | Código | Confirmar largo/semántica por cámara |
| ACH/CENIT | Devolución/ROR | Batch Number | consecutivo por archivo/lote | ROR `0000001` fijo; returns usa constante de servicio | hardcoded/config | Pendiente confirmación normativa | Alto | Código | Diseñar secuencia por cámara |

### 5.3 Registro 6 — Entry Detail

| Cámara | Flujo | Campo | Regla esperada | Implementación actual | Origen actual del valor | Estado de certeza | Riesgo | Fuente | Próxima acción |
|---|---|---|---|---|---|---|---|---|---|
| ACH/CENIT | Devolución/ROR | Transaction Code | Código débito/crédito válido | Proviene de transacción | transacción | Confirmado técnicamente | Medio | Código | Validar catálogo por cámara |
| ACH/CENIT | Devolución/ROR | Receiving DFI + Check Digit | DFI receptor válido | Normaliza DFI y check digit en armado | transacción/cálculo | Confirmado técnicamente | Medio | Código | Confirmar semántica por cámara |
| ACH/CENIT | Devolución/ROR | DFI Account Number | Cuenta destino/origen según flujo | Usa cuenta de transacción | transacción | Confirmado técnicamente | Medio | Código | Revisar masking/UAT real |
| ACH/CENIT | Devolución/ROR | Amount | Monto en centavos | cálculo `(amount*100)` | cálculo/transacción | Confirmado técnicamente | Bajo | Código | Verificar prenotificación por cámara |
| ACH/CENIT | Devolución saliente | Individual Identification Number | ID individual/empresa | `tx.CompanyIdentification` | transacción | Inferido | Medio | Código | Confirmar normativa de campo |
| ACH/CENIT | Devolución saliente | Individual Name | Nombre individual/empresa | `tx.CompanyName` | transacción | Inferido | Medio | Código | Confirmar normativa de campo |
| ACH/CENIT | Devolución/ROR | Discretionary Data | Datos discretos | Fijo `R`/espacios según flujo | hardcoded | Pendiente confirmación normativa | Medio | Código | Confirmar uso permitido |
| ACH/CENIT | Devolución/ROR | Addenda Record Indicator | Indicador addenda | `1` fijo en flujos auditados | hardcoded | Confirmado técnicamente | Bajo | Código | Validar si hay casos sin addenda |
| ACH/CENIT | Devolución/ROR | Trace Number | Traza única | secuencia/número normalizado | cálculo/transacción | Confirmado técnicamente | Medio | Código | Confirmar chaining por cámara |

### 5.4 Registro 7 — Addenda / Return Addenda

| Cámara | Flujo | Campo | Regla esperada | Implementación actual | Origen actual del valor | Estado de certeza | Riesgo | Fuente | Próxima acción |
|---|---|---|---|---|---|---|---|---|---|
| ACH/CENIT | Devolución/ROR | Addenda Type Code | Tipo addenda válido | Returns `99`; ROR inicia `799` | hardcoded | Pendiente confirmación normativa | Alto | Código | Confirmar layout por cámara |
| ACH/CENIT | Devolución/ROR | Return Reason Code | causal válida (Rxx/DEVxx) | Toma causal de flujo/selección | transacción/request | Confirmado técnicamente | Medio | Código + Anexo A | Validar catálogo por cámara |
| ACH/CENIT | Devolución/ROR | Original Entry Trace Number | traza original | Se incluye en addenda | transacción | Confirmado técnicamente | Medio | Código | Validar formato exacto por cámara |
| ACH/CENIT | Devolución/ROR | Original Receiving DFI | DFI original si aplica | No explícito como campo separado en todos layouts auditados | no aplica/inferido | Pendiente confirmación normativa | Alto | Código | Confirmar obligación por cámara |
| ACH/CENIT | Devolución/ROR | Addenda Sequence Number | secuencia addenda | Derivada de nueva traza/posición | cálculo | Inferido | Medio | Código | Confirmar definición normativa |
| ACH/CENIT | Devolución/ROR | Entry Detail Sequence Number | secuencia del registro 6 | Derivada `[^7..]` u equivalente | cálculo | Confirmado técnicamente | Medio | Código | Confirmar semántica por cámara |
| CENIT | Devolución/ROR | Causales Rxx | catálogo de causales | Consumidas en flujo | request/transacción | Confirmado normativamente (causales) | Medio | CENIT Anexo A | No extrapolar a layout completo |
| ACH | Devolución/ROR | Validación por causal y formato | regla de retorno ACH | Parcialmente implementado | inferido | Pendiente confirmación normativa | Alto | ACH V32 | Validación campo-a-campo en UAT |

### 5.5 Registro 8 — Batch Control

| Cámara | Flujo | Campo | Regla esperada | Implementación actual | Origen actual del valor | Estado de certeza | Riesgo | Fuente | Próxima acción |
|---|---|---|---|---|---|---|---|---|---|
| ACH/CENIT | Devolución/ROR | Service Class Code | consistente con registro 5 | calculado | cálculo | Confirmado técnicamente | Bajo | Código | Test golden por cámara |
| ACH/CENIT | Devolución/ROR | Entry/Addenda Count | conteo correcto | calculado | cálculo | Confirmado técnicamente | Bajo | Código | Validación externa |
| ACH/CENIT | Devolución/ROR | Entry Hash | suma DFI truncada | calculado `% 10_000_000_000` | cálculo | Confirmado técnicamente | Medio | Código | Confirmar regla exacta por cámara |
| ACH/CENIT | Devolución/ROR | Total Debit / Total Credit | totales por signo | calculado desde entries | cálculo | Confirmado técnicamente | Bajo | Código | Pruebas UAT con data real |
| ACH/CENIT | Devolución/ROR | Company Identification | debe coincidir lote | fijo/constante en servicios | hardcoded/config | Pendiente confirmación normativa | Alto | Código | Parametrizar por cámara |
| ACH/CENIT | Devolución/ROR | MAC/Reserved | según norma | espacios/reservado | hardcoded | Inferido | Medio | Código | Confirmar obligatoriedad |
| ACH/CENIT | Devolución/ROR | Originating DFI / Batch Number | consistencia con tipo 5 | usa originCode + batch fijo/constante | ClearingHouse + hardcoded | Pendiente confirmación normativa | Alto | Código | Configuración por cámara |

### 5.6 Registro 9 — File Control

| Cámara | Flujo | Campo | Regla esperada | Implementación actual | Origen actual del valor | Estado de certeza | Riesgo | Fuente | Próxima acción |
|---|---|---|---|---|---|---|---|---|---|
| ACH/CENIT | Devolución/ROR | Batch Count | cantidad de lotes | `1` en servicios auditados | hardcoded/cálculo | Confirmado técnicamente | Medio | Código | Validar multi-lote futuro |
| ACH/CENIT | Devolución/ROR | Block Count | total bloques de 10 | calculado con ceiling | cálculo | Confirmado técnicamente | Bajo | Código | Verificación externa |
| ACH/CENIT | Devolución/ROR | Entry/Addenda Count | conteo global | calculado | cálculo | Confirmado técnicamente | Bajo | Código | Verificación UAT |
| ACH/CENIT | Devolución/ROR | Entry Hash / Totales | consistencia global | calculado | cálculo | Confirmado técnicamente | Bajo | Código | Verificación externa |
| ACH/CENIT | Devolución/ROR | Reserved | relleno | espacios | hardcoded | Inferido | Bajo | Código | Confirmar si aplica restricción |
| ACH/CENIT | Devolución/ROR | Padding 9 | múltiplos de 10 registros | agrega líneas `999...` | cálculo | Confirmado técnicamente | Bajo | Código | Mantener test de integridad |

## 6. Matriz implementación actual

| Flujo | Clase | Método | Registro | Campo | Valor actual | Origen del valor | Diferenciado por cámara | Riesgo | Recomendación |
|---|---|---|---|---|---|---|---|---|---|
| Devolución saliente | `AchReturnsService` | `BuildType1HeaderLine` | 1 | Destination/Origin names | `ACH-RET`, `ClearingHouse.Name` | hardcoded + ClearingHouse | Parcial | Medio/Alto | Parametrizar labels por cámara |
| Devolución saliente | `AchReturnsService` | `BuildType5HeaderLine` | 5 | Company Name/Entry Desc | `DEVOLUCIONES`, `RETORNO` | hardcoded | No | Alto | Catálogo por cámara/flujo |
| Devolución saliente | `AchReturnsService` | `BuildType6ReturnLine` | 6 | Transaction/DFI/Amount/Trace | desde tx + cálculo | transacción/cálculo | Parcial | Medio | Añadir validaciones por rail |
| Devolución saliente | `AchReturnsService` | `BuildType7ReturnAddendaLine` | 7 | Reason/Traces | causal + trazas | request/transacción | Parcial | Alto | Matriz de subcampos por cámara |
| Devolución saliente | `AchReturnsService` | `BuildType8/9ControlLine` | 8/9 | Hash/Totals/Block | calculados | cálculo | Sí (por data) | Bajo/Medio | Golden tests por cámara |
| ROR productivo | `AchReturnOfReturnFileGenerationService` | `BuildType1` | 1 | Header textual | `ACH COLOMBIA`, `ACHINTERBANK ROR` | hardcoded | No | Alto | Mover a config por cámara |
| ROR productivo | `AchReturnOfReturnFileGenerationService` | `BuildType5` | 5 | Company/Desc/Batch | `BANCROR`, `DEV. DEV.`, `0000001` | hardcoded | No | Alto | Parametrizar por rail |
| ROR productivo | `AchReturnOfReturnFileGenerationService` | `BuildType6/7` | 6/7 | tx/reason/traces | flujo + cálculo | transacción/cálculo | Parcial | Medio/Alto | Validar layout por cámara |
| ROR productivo | `AchReturnOfReturnFileGenerationService` | `BuildType8/9` | 8/9 | hash/totals/padding | cálculo | cálculo | Sí (por data) | Bajo/Medio | Pruebas de aceptación cámara |
| Genérico NACHA | `NachaFileBuilder` | builder principal | 1..9 | motor genérico | mapping engine | config+mapping | Parcial | Medio | Alinear servicios específicos a capa común |
| Validación semántica | `NachaSemanticValidator` | validación semántica | varios | coherencia semántica | reglas técnicas | cálculo/reglas | Parcial | Medio | Extender por `ClearingHouseId` |
| Pruebas | `AchReturnsFileByClearingHouseTests` / `AchReturnOfReturnFileGenerationServiceTests` | varios | 1..9 parciales | comportamiento observable | tests | Sí (parcial) | Medio | ampliar golden por cámara/campo |

## 7. Brechas por cámara

### ACH Colombia
- **Confirmado técnicamente:** generación 1/5/6/7/8/9, hash/totales/padding.
- **Confirmado normativamente:** parcial; requiere trazabilidad campo específico contra ACH V32 por flujo.
- **Pendiente confirmación:** cabecera/lote descriptivo, company id, batch number, reglas exactas de addenda por flujo.
- **Hardcodes temporales:** `DEVOLUCIONES`, `RETORNO`, valores fijos en ROR.
- **Riesgo homologación:** medio/alto si cámara exige layout/semántica distinta por flujo.

### CENIT
- **Confirmado técnicamente:** generación 1/5/6/7/8/9 en flujos auditados.
- **Confirmado por Anexo A:** principalmente causales Rxx.
- **Pendiente confirmación layout/campos:** cabecera/lote/identificadores/consecutivos específicos por flujo.
- **Riesgo de usar valores ACH en CENIT:** alto (hardcodes con texto/identificadores orientados a ACH).
- **Relación pendiente ciclos/fecha valor/liquidación:** requiere evidencia adicional de fuente normativa/operativa.

## 8. Hardcodes y riesgo

| Hardcode | Ubicación | Flujo | Cámara afectada | Riesgo | ¿Puede quedar para UAT? | ¿Puede quedar para productivo? | Acción recomendada |
|---|---|---|---|---|---|---|---|
| `ACH COLOMBIA` | `AchReturnOfReturnFileGenerationService.BuildType1` | ROR productivo | CENIT (alto), ACH (medio) | Alto | Sí (controlado) | No | Parametrizar por cámara |
| `ACHINTERBANK ROR` | `BuildType1` ROR | ROR productivo | Ambas | Alto | Sí | No | Mover a config/catalogo |
| `BANCROR` | `BuildType5/8` ROR | ROR productivo | Ambas | Alto | Sí | No | Parametrizar Company Identification |
| `DEV. DEV.` | `BuildType5` ROR | ROR productivo | Ambas | Alto | Sí | No | Parametrizar Entry Description |
| `DEVOLUCIONES` | `BuildType5HeaderLine` returns | Devolución saliente | Ambas | Medio/Alto | Sí | No (sin confirmación) | Parametrizar por cámara/flujo |
| `RETORNO` | `BuildType5HeaderLine` returns | Devolución saliente | Ambas | Medio/Alto | Sí | No (sin confirmación) | Parametrizar por cámara/flujo |
| `0000001` | `BuildType5/8` ROR | ROR productivo | Ambas | Alto | Sí | No | Implementar secuencia por cámara |
| `000101006` (fallback) | ROR origin code fallback | ROR productivo | Ambas | Medio | Sí | No sin validación | Sustituir por config obligatoria |
| `FileID=A` fijo | headers | Devolución/ROR | Ambas | Medio | Sí | No sin validación | Definir política de secuencia |

## 9. Criterios de salida de NO-GO productivo para registros NACHA-M
1. Campo validado contra fuente normativa.
2. Campo diferenciado por cámara cuando aplique.
3. Configuración por `ClearingHouseId` definida.
4. Golden test por cámara.
5. Archivo aceptado en UAT/homologación.
6. Totales/hash validados.
7. Padding/block count validado.
8. Registro 7 validado por causal y flujo.
9. Batch Number/consecutivo validado.
10. Company Identification validado.
11. Origin/Destination validado.
12. Firma negocio/operaciones/compliance.
13. Acta UAT.

## 10. Plan de remediación

| Commit propuesto | Objetivo | Alcance | Riesgo | Rollback |
|---|---|---|---|---|
| `docs(nacha): add record-level normative matrix by rail` | Consolidar matriz de campo por cámara | Documentación | Bajo | Revert doc |
| `test(nacha): add golden record tests for returns and ROR by rail` | Congelar salida actual por cámara | Tests backend | Medio | Revert tests |
| `refactor(nacha): introduce rail-specific NACHA header config` | Parametrizar header/lote por cámara | Config + servicios | Medio/Alto | Feature flag/fallback actual |
| `refactor(nacha): replace hardcoded ROR header constants with config` | Retirar hardcodes ROR críticos | Servicio ROR + config | Medio | Fallback a constantes actuales |
| `refactor(nacha): route outbound returns through NACHA record config` | Alinear returns a configuración de rail | Servicio returns + config | Medio | Ruta legacy |
| `feat(nacha): validate record-level fields by clearing house` | Validaciones por `ClearingHouseId` | Validator/semantic checks | Alto | Downgrade a warning mode |
| `docs(uat): add NACHA record acceptance checklist` | Checklist operativo final de registros | Documentación UAT | Bajo | Revert doc |

- Checklist UAT de registros NACHA-M por cámara: `docs/uat/nacha-records-acceptance-checklist.md` (define validación 1/5/6/7/8/9 para ACH/CENIT y preserva NO-GO productivo).
