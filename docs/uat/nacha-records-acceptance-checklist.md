# Checklist UAT — Registros NACHA-M ACH Colombia / CENIT

> Referencia UAT complementaria: `docs/uat/incoming-return-orphan-acceptance-checklist.md`.


## 1. Propósito
Este checklist UAT valida, con trazabilidad técnica y normativa, los siguientes registros NACHA-M:
- **1 File Header**
- **5 Batch Header**
- **6 Entry Detail**
- **7 Addenda**
- **8 Batch Control**
- **9 File Control**

Cobertura funcional obligatoria:
- devolución saliente;
- ROR productivo;
- ROR audit-mode **como interno/no externo** (sin intercambio externo ni declaración de aprobación productiva).

## 2. Estado actual
Estado vigente del frente NACHA-M en el proyecto:
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí.
- `CurrentLayout` se mantiene como **provisional/UAT**.
- `CurrentLayout` **no equivale** a aprobación normativa definitiva.
- CENIT Anexo A confirma causales **Rxx**, pero **no** confirma por sí solo layout NACHA-M completo campo-a-campo.

## 3. Alcance por cámara

### ACH Colombia
Se valida:
- conformidad de campos de registros 1/5/6/7/8/9;
- aceptación en homologación/UAT;
- aprobación campo-a-campo con evidencia trazable.

### CENIT
Se valida:
- causales Rxx contra Anexo A;
- layout completo por campo (sin asumir equivalencia automática con ACH);
- no extrapolar reglas ACH a CENIT;
- ciclos, fecha valor y liquidación (incluyendo CUD) cuando aplique;
- aceptación de archivo por cámara.

## 4. Checklist técnico por registro

| ID | Cámara | Flujo | Registro | Campo/control | Validación | Evidencia requerida | Estado | Observaciones |
|---|---|---|---|---|---|---|---|---|
| T1-01 | ACH/CENIT | Devolución/ROR | 1 | Record Type Code = 1 | Debe iniciar con `1` en posición de registro | Archivo NACHA + inspección posicional | Pendiente | Control base obligatorio |
| T1-02 | ACH/CENIT | Devolución/ROR | 1 | Immediate Destination | Valor autorizado por cámara/flujo | Evidencia de fuente normativa + archivo | Pendiente | No asumir valor único global |
| T1-03 | ACH/CENIT | Devolución/ROR | 1 | Immediate Origin | Corresponde a originador autorizado | Evidencia de catálogo + archivo | Pendiente | Validar por cámara |
| T1-04 | ACH/CENIT | Devolución/ROR | 1 | File Creation Date | Formato/norma de fecha correcto | Archivo + evidencia de reloj operativo | Pendiente | Revisar timezone operativo |
| T1-05 | ACH/CENIT | Devolución/ROR | 1 | File Creation Time | Formato/norma de hora correcto | Archivo + logs de generación | Pendiente | Revisar cutoff |
| T1-06 | ACH/CENIT | Devolución/ROR | 1 | File ID Modifier | Secuencia/modificador válido | Archivo + política de secuencia | Pendiente | `A` fijo requiere validación |
| T1-07 | ACH/CENIT | Devolución/ROR | 1 | A094101 | Presencia/estructura esperada según layout vigente | Archivo + verificación posicional | Pendiente | Tratar como control de formato |
| T1-08 | ACH/CENIT | Devolución/ROR | 1 | Record Size | Debe ser consistente con layout (94) | Archivo + validator/golden test | Pendiente | Confirmación externa requerida |
| T1-09 | ACH/CENIT | Devolución/ROR | 1 | Blocking Factor | Debe cumplir bloque (10) | Archivo + cálculo block count | Pendiente | Relacionado con registro 9 |
| T1-10 | ACH/CENIT | Devolución/ROR | 1 | Format Code | Valor de formato esperado | Archivo + regla normativa | Pendiente | Validar por cámara |
| T1-11 | ACH/CENIT | Devolución/ROR | 1 | Destination Name | Nombre destino aprobado | Archivo + aprobación de cámara | Pendiente | No hardcode productivo sin aprobación |
| T1-12 | ACH/CENIT | Devolución/ROR | 1 | Origin Name | Nombre originador aprobado | Archivo + aprobación operativa | Pendiente | Revisar truncamiento/padding |
| T5-01 | ACH/CENIT | Devolución/ROR | 5 | Service Class Code | Debe corresponder a tipo de lote | Archivo + evidencia de regla | Pendiente | Validar por mezcla débito/crédito |
| T5-02 | ACH/CENIT | Devolución/ROR | 5 | Company Name | Company Name autorizado | Archivo + aprobación cámara | Pendiente | Evitar suposición cross-cámara |
| T5-03 | ACH/CENIT | Devolución/ROR | 5 | Company Identification | ID compañía vigente y autorizado | Archivo + catálogo/acta | Pendiente | Crítico para aprobación externa |
| T5-04 | ACH/CENIT | Devolución/ROR | 5 | SEC Code | SEC permitido para el flujo | Archivo + regla normativa | Pendiente | Validar por flujo |
| T5-05 | ACH/CENIT | Devolución/ROR | 5 | Company Entry Description | Texto permitido | Archivo + evidencia UAT | Pendiente | Revisar abreviaturas aceptadas |
| T5-06 | ACH/CENIT | Devolución/ROR | 5 | Effective Entry Date | Fecha efectiva correcta | Archivo + matriz de ciclos | Pendiente | Alinear con ciclo |
| T5-07 | ACH/CENIT | Devolución/ROR | 5 | Originator Status Code | Valor permitido por norma | Archivo + fuente primaria | Pendiente | Confirmación por cámara |
| T5-08 | ACH/CENIT | Devolución/ROR | 5 | Originating DFI | DFI de origen correcto | Archivo + data de cámara | Pendiente | Validación campo-a-campo |
| T5-09 | ACH/CENIT | Devolución/ROR | 5 | Batch Number | Consecutivo válido | Archivo + política de secuencia | Pendiente | No dejar fijo para productivo |
| T6-01 | ACH/CENIT | Devolución/ROR | 6 | Transaction Code | Código válido para el tipo de movimiento | Archivo + catálogo transaccional | Pendiente | Validar mapping por cámara |
| T6-02 | ACH/CENIT | Devolución/ROR | 6 | Receiving DFI | DFI receptor válido | Archivo + fuente normativa | Pendiente | Confirmación externa |
| T6-03 | ACH/CENIT | Devolución/ROR | 6 | Account Number | Cuenta con formato/longitud permitida | Archivo + caso UAT | Pendiente | Enmascaramiento según política |
| T6-04 | ACH/CENIT | Devolución/ROR | 6 | Amount | Monto correcto en unidad esperada | Archivo + cálculo reconciliado | Pendiente | Conciliar con controles 8/9 |
| T6-05 | ACH/CENIT | Devolución/ROR | 6 | Individual ID | ID individual/empresa válido | Archivo + evidencia de origen | Pendiente | Revisar reglas de contenido |
| T6-06 | ACH/CENIT | Devolución/ROR | 6 | Individual Name | Nombre conforme a layout | Archivo + evidencia UAT | Pendiente | Revisar truncamiento |
| T6-07 | ACH/CENIT | Devolución/ROR | 6 | Addenda Indicator | Indicador consistente con addenda existente | Archivo + registro 7 correlativo | Pendiente | Debe ser coherente |
| T6-08 | ACH/CENIT | Devolución/ROR | 6 | Trace Number | Traza única y consistente | Archivo + bitácora/golden test | Pendiente | Validar unicidad |
| T7-01 | ACH/CENIT | Devolución/ROR | 7 | Addenda Type Code | Tipo de addenda permitido | Archivo + fuente normativa | Pendiente | Diferenciar cámara/flujo |
| T7-02 | ACH/CENIT | Devolución/ROR | 7 | Return Reason Code | Causal válida (Rxx/DEVxx según cámara) | Archivo + catálogo/Anexo A | Pendiente | CENIT: confirmar Rxx |
| T7-03 | ACH/CENIT | Devolución/ROR | 7 | Original Trace | Traza original presente y correcta | Archivo + referencia transacción | Pendiente | Crítico para trazabilidad |
| T7-04 | ACH/CENIT | Devolución/ROR | 7 | Original Receiving DFI | DFI original conforme regla | Archivo + evidencia normativa | Pendiente | Verificar obligatoriedad |
| T7-05 | ACH/CENIT | Devolución/ROR | 7 | Sequence Number | Secuencia válida | Archivo + validación posicional | Pendiente | Correlación con registro 6 |
| T7-06 | ACH/CENIT | Devolución/ROR | 7 | Causal Rxx / DEVxx | Regla de causal por cámara y flujo | Archivo + fuente primaria + acta | Pendiente | No extrapolar ACH a CENIT |
| T8-01 | ACH/CENIT | Devolución/ROR | 8 | Entry/Addenda Count | Conteo exacto de entradas/addendas | Archivo + script/verificación manual | Pendiente | Debe cuadrar con 6/7/9 |
| T8-02 | ACH/CENIT | Devolución/ROR | 8 | Entry Hash | Hash de entradas conforme método esperado | Archivo + cálculo reproducible | Pendiente | Control crítico NACHA-M |
| T8-03 | ACH/CENIT | Devolución/ROR | 8 | Total Debit | Total débitos correcto | Archivo + conciliación | Pendiente | Cuadra con data origen |
| T8-04 | ACH/CENIT | Devolución/ROR | 8 | Total Credit | Total créditos correcto | Archivo + conciliación | Pendiente | Cuadra con data origen |
| T8-05 | ACH/CENIT | Devolución/ROR | 8 | Company Identification | Igual al lote/cabecera según regla | Archivo + evidencia de consistencia | Pendiente | Control de identidad |
| T8-06 | ACH/CENIT | Devolución/ROR | 8 | Originating DFI | Debe corresponder a cámara/flujo | Archivo + aprobación externa | Pendiente | Validar por cámara |
| T8-07 | ACH/CENIT | Devolución/ROR | 8 | Batch Number | Debe corresponder al lote | Archivo + chequeo de correlación | Pendiente | Evitar fijo sin política |
| T9-01 | ACH/CENIT | Devolución/ROR | 9 | Batch Count | Conteo de lotes correcto | Archivo + verificación | Pendiente | Relacionado con 5/8 |
| T9-02 | ACH/CENIT | Devolución/ROR | 9 | Block Count | Conteo de bloques correcto | Archivo + cálculo bloque 10 | Pendiente | Crítico para aceptación |
| T9-03 | ACH/CENIT | Devolución/ROR | 9 | Entry/Addenda Count | Conteo global correcto | Archivo + reconciliación | Pendiente | Debe cuadrar con 8 |
| T9-04 | ACH/CENIT | Devolución/ROR | 9 | Entry Hash | Hash global correcto | Archivo + cálculo reproducible | Pendiente | Debe cuadrar con 8 |
| T9-05 | ACH/CENIT | Devolución/ROR | 9 | Total Debit | Total global débito correcto | Archivo + conciliación | Pendiente | Coherencia financiera |
| T9-06 | ACH/CENIT | Devolución/ROR | 9 | Total Credit | Total global crédito correcto | Archivo + conciliación | Pendiente | Coherencia financiera |
| T9-07 | ACH/CENIT | Devolución/ROR | 9 | Padding | Padding correcto hasta múltiplo de 10 | Archivo + conteo de líneas | Pendiente | Control estructural final |

## 5. Checklist funcional UAT

| ID | Flujo | Cámara | Caso | Archivo generado | Resultado esperado | Evidencia | Responsable | Estado |
|---|---|---|---|---|---|---|---|---|
| F-01 | Devolución saliente | ACH | Devolución saliente ACH | Sí | Aceptación en UAT/homologación ACH | Archivo + respuesta operador + acta | QA/Operaciones | Pendiente |
| F-02 | Devolución saliente | CENIT | Devolución saliente CENIT | Sí | Aceptación en UAT/homologación CENIT | Archivo + respuesta operador + acta | QA/Operaciones | Pendiente |
| F-03 | ROR productivo | ACH | ROR productivo ACH | Sí | Archivo válido en circuito UAT ACH | Archivo + validación cámara | QA/Operaciones | Pendiente |
| F-04 | ROR productivo | CENIT | ROR productivo CENIT | Sí | Archivo válido en circuito UAT CENIT | Archivo + validación cámara | QA/Operaciones | Pendiente |
| F-05 | ROR audit-mode interno | ACH/CENIT | ROR audit-mode interno | Sí (interno) | Generación interna sin exposición externa | Evidencia de modo interno + logs | QA/Tecnología | Pendiente |
| F-06 | Devolución/ROR | ACH/CENIT | Archivo con warning `CurrentLayout` | Sí | Warning documentado sin declarar GO productivo | Logs + acta UAT | QA/Compliance | Pendiente |
| F-07 | Devolución/ROR | ACH/CENIT | Error estructural simulado | Sí (inválido) | Rechazo esperado y trazable | Archivo inválido + resultado validator | QA/Tecnología | Pendiente |
| F-08 | Devolución/ROR | ACH/CENIT | Validación de hash | Sí | Hash consistente en 8/9 | Cálculo independiente + archivo | QA/Tecnología | Pendiente |
| F-09 | Devolución/ROR | ACH/CENIT | Validación de block count | Sí | Block count correcto | Cálculo independiente + archivo | QA/Tecnología | Pendiente |
| F-10 | Devolución/ROR | ACH/CENIT | Validación de padding | Sí | Padding correcto al cierre | Conteo de líneas + archivo | QA/Tecnología | Pendiente |

## 6. Checklist normativo

| ID | Cámara | Documento fuente | Sección/página | Campo NACHA | Regla | Evidencia | Estado | Firma requerida |
|---|---|---|---|---|---|---|---|---|
| N-01 | ACH | ACH Colombia V32 | Campo a documentar en UAT | 1/5/6/7/8/9 | Validación campo-a-campo contra fuente primaria | Matriz trazable + acta | Pendiente | Negocio + Operaciones + Compliance + Técnica |
| N-02 | CENIT | CENIT Anexo A (limitado a causales) | Causales Rxx aplicables | Registro 7 | Confirmar causales Rxx/DEVxx según flujo | Evidencia de causal + aceptación cámara | Pendiente | Operaciones + Compliance |
| N-03 | ACH/CENIT | Matriz NACHA current | Referencias de matriz vigente | 1/5/6/7/8/9 | Usar matriz current como control documental UAT, no como aprobación final | Checklist firmado + evidencias técnicas | Pendiente | Técnica + QA |
| N-04 | ACH/CENIT | Confirmación externa pendiente | N/A | 1/5/6/7/8/9 | Aprobación formal por cámara/operador antes de productivo | Correo/acta de aprobación externa | Pendiente | Negocio + Operaciones + Compliance |

## 7. Evidencia técnica esperada
Se debe adjuntar, como mínimo:
- salida de **golden tests**;
- salida de **validator tests**;
- archivos NACHA de muestra (por cámara y flujo);
- hash SHA-256 (si aplica al proceso de control documental);
- logs de warnings (incluyendo `CurrentLayout`);
- acta de UAT;
- aprobación de cámara/operador.

## 8. Criterios de salida de NO-GO productivo
La salida de **NO-GO productivo** solo procede cuando se cumplen **todos**:
1. Campo validado contra fuente normativa.
2. Cámara validó archivo.
3. ACH y CENIT diferenciados.
4. Causales Rxx confirmadas.
5. Hash/totales confirmados.
6. Padding/block count confirmado.
7. Batch number/consecutivo confirmado.
8. Origin/Destination confirmado.
9. Company ID confirmado.
10. Registro 7 confirmado.
11. Golden tests por cámara.
12. UAT aprobado.
13. Firma negocio.
14. Firma operaciones.
15. Firma compliance/riesgo.
16. Aprobación técnica.

> Hasta cumplir los 16 criterios, la decisión vigente se mantiene en **NO-GO productivo**.

## 9. Acta mínima sugerida
Formato mínimo del acta:
- Fecha
- Cámara
- Ambiente
- Flujo
- Archivo
- Registros validados
- Resultado
- Evidencia adjunta
- Hallazgos
- Decisión
- Firmas

## 10. Riesgos residuales
Riesgos que deben permanecer visibles hasta cierre formal:
- `CurrentLayout` provisional.
- Hardcodes ACH caracterizados.
- Layout CENIT pendiente de cierre completo.
- Riesgo de extrapolar reglas ACH a CENIT.
- Riesgo de validar solo técnicamente sin aprobación normativa.
- Riesgo de aceptar warnings como productivos.
- Riesgo en ciclos/liquidación/CUD si aplica.


## Referencia cruzada de causales (registro 7 / Return Reason Code)

Para validaciones del registro 7 (addenda) y `Return Reason Code`, usar en conjunto esta matriz:
- `docs/audits/cause-code-normative-matrix-ach-cenit-sta-current.md`

Regla de control: no extrapolar causales ACH hacia CENIT y mantener separación entre Rxx/DEVxx vs Dxx/Ixxx vs internos.

## 11. Decisión vigente de control
- **GO técnico:** sí.
- **GO UAT controlado:** sí.
- **NO-GO productivo:** sí (se mantiene).

- Checklist UAT complementario de causales (registro 7 / Return Reason Code): `docs/uat/cause-code-acceptance-checklist.md`.

- Referencia cruzada de trazabilidad del archivo de devolución saliente: `docs/audits/outbound-return-state-traceability-matrix-current.md`.
- Checklist UAT de estado/evento/idempotencia de devolución saliente: `docs/uat/outbound-return-state-traceability-acceptance-checklist.md`.


## Referencia cruzada record 7 / addenda 99

Para control funcional de devolución entrante (registro 7 / addenda 99) y huérfanas:

- `docs/audits/incoming-return-e2e-orphan-matrix-current.md`

## Referencia cruzada total vs partial

Para la frontera semántica canónica entre `RejectedTotal`, `RejectedPartial`, `Accepted`, orphan/unresolved, manual audit-only y la distinción formal frente a devolución parcial por monto, ver:

- `docs/audits/total-vs-partial-rejection-matrix-current.md`

## Referencia cruzada checklist UAT total vs partial

Para la validación UAT paso-a-paso de `Accepted`, `RejectedTotal`, `RejectedPartial`, orphan/unresolved, manual audit-only, separación de códigos y relación con ROR/contabilidad, ver:

- `docs/uat/rejection-total-partial-acceptance-checklist.md`

