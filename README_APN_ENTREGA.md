# Entrega APN — Auditoría de prenotificaciones ACH Interbank

Cada ZIP es **completo y acumulativo**, con raíz `ACHInterbank_ajustado/`. Para cargar a Sources utilice el paquete del incremento deseado, o el final I07 si requiere todo el trabajo; no es necesario combinar ZIPs. Mantenga la versión final como base de continuidad.

**Inicio:** `MEMORY_STATE_APN.md`, luego `docs/auditoria-prenotificaciones/PLAN_AUDITORIA_APN.md`, `MEMORY_STATE.md`, `STATUS_IMPLEMENTACIONES.md`, `HALLAZGOS.md` y los informes por incremento.

**Importante:** se ejecutó auditoría estática/documental y verificaciones Python offline. **No se modificó código productivo**. No existe certificación .NET/SQL/UAT de prenotificaciones en esta entrega. No alterar ni asumir que se completó la homologación.

**Calendarios vigentes de referencia:** ACH Colombia **7 ciclos**; CENIT **5 ciclos**. Para ACH Colombia, el manual V35 con 5 ciclos conserva solo carácter histórico respecto a los horarios.


## APN-I08 (corrección incremental)

Se incorpora corrección de fuente en `ClearingHouseValidationPolicyService` que exige la ventana completa y toma como umbral el fin de liberación de archivos. Ver `docs/auditoria-prenotificaciones/incrementos/APN-I08_CORRECCION_VENCIMIENTOS.md`. No certifica runtime; los vencimientos canónicos históricos permanecen en seguimiento.
