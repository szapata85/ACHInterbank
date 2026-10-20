# DELTA DEVOLUCIONES ACH COLOMBIA V35

## 1. Objetivo

Revalidar exclusivamente los pendientes ACH Colombia de B4 contra V35, sin modificar implementación ni reabrir CENIT.

## 2. Fuente V35

`docs/normativa/md/ACH-Colombia-V35.md`: *Manual de Servicio ACH Transferencias Interbancarias para Entidad Participante*, DDS-DIS-MAN-004, Versión 35, abril de 2026. El documento identifica elaboración, revisión, aprobación y la última versión aprobada en BSC. Codebase Memory: `search_code` focalizado sobre `ACHInterbank-normativa`; no se reindexó.

## 3. Temas revisados

Vigencia, ROR, naming, layout, Addenda, correlación, DFI, parcialidad, plazos, ciclos y causales. CENIT permanece sin cambios.

## 4. Delta V31/V32 → V35

| ID | Tema devolución | Regla anterior | Regla V35 | Impacto ACHInterbank |
|---|---|---|---|---|
| D1 | Vigencia | V31/V32 ambigua | V35 abril 2026 sustituye la referencia | Eliminar pendiente de vigencia |
| D2 | ROR | ROR participante pendiente | D28 solo operador: devolución de una devolución | No habilitar ROR participante |
| D3 | Correlación | `OriginalTraceRef` no demostrado | Addenda 7/4 = secuencia original, 15 posiciones | Mapear nombre interno al campo oficial |
| D4 | Parcialidad | Pendiente | Importe original; débito sin pagos parciales | Separar parcial de archivo |
| D5 | Causales | Catálogo parcial | R30-R35 explícitas; D28/D29 operatorias | Actualizar policy en JOB técnico |

## 5. Return of Return

**NO APLICA** como ROR de participante. V35 no define originador, receptor, plazo, ciclo, cantidad, causal, Addenda o naming para ese flujo. D28 del Anexo 3 es una devolución por operador; no debe interpretarse como ROR.

## 6. Naming

Norma V35 → esperado → brecha actual: `RRRRTTT.ZZZ.1` (Ruta, Tránsito, consecutivo diario) → usarlo para archivos ordinarios; `.RET` solo queda demostrado para retornos/salidas y devolución por operador → el naming AS-IS requiere ajuste técnico posterior. No se generaliza `.RET`.

## 7. Layout y Addenda

Devolución ordinaria: registros 1/5/6/7/8/9, Addenda 99 obligatoria, una devolución por transacción, secuencia nueva, preservación de lote/detalle/control salvo excepciones y sin retornar Addenda original.

## 8. Correlación

Fuente normativa oficial: Registro Adenda 7, campo 4, longitud 15; debe coincidir con campo 11 del detalle original. `OriginalTraceRef` es nombre interno, no denominación normativa. **CONFIRMADO.**

## 9. DFI

El detalle de la devolución identifica como receptor al participante originador original; la Addenda conserva el código del participante receptor de la transacción original. Roles ACH Colombia confirmados.

## 10. Parcialidad

La devolución monetaria usa el valor original; la prenotificación usa cero. Débito no admite pagos parciales. La devolución parcial de archivo por operador es distinta y no prueba parcialidad por entrada.

## 11. Plazos y ciclos

Máximo cuatro ciclos desde la original para crédito, débito y prenotificación. Crédito tardío: calidad; débito tardío: no permitido. Reclamo del usuario Receptor: primer ciclo del siguiente día hábil. Anexo 23: 60 días hábiles para solución REV/DEV, no envío.

## 12. Causales

Anexo 9: R29-R35 quedan explícitas en V35, con aplicabilidad por crédito/débito/prenotificación. Anexo 3: D28 “Devolución de una devolución” y D29 “Devolución débito tardía”. No se transcribe el catálogo completo.

## 13. Impacto futuro en ACHInterbank

Actualizar en JOBs técnicos: policy de naming, mapeo de Addenda 7/4 a `OriginalTraceRef`, roles DFI, importe total y separación D28/operatoria. No cambiar código en B4.0.

## 14. Pendientes externos reales

Ninguno para los pendientes normativos ACH Colombia revisados por V35. La aceptación/certificación operativa futura no es una pregunta normativa remanente de B4.1.

## 15. Estado actualizado de B4

- **B4 global:** PARCIALMENTE CERRADA.
- **ACH Colombia — devolución recibida:** HOMOLOGADA.
- **ACH Colombia — devolución originada:** HOMOLOGADA.
- **ACH Colombia — ROR:** NO APLICA.
- **B4.1:** NO necesario para estos pendientes.
- **JOB 2:** HABILITADO. **JOB 3:** HABILITADO. **JOB 4:** PARCIALMENTE HABILITADO. **JOB 6 ROR:** HABILITADO. **JOB 7:** PARCIALMENTE HABILITADO. JOB 5 y JOB 8 quedan parcialmente habilitados por dependencia de implementación, no de evidencia normativa V35.
