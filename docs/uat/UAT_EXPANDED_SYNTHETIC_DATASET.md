# Dataset sintetico ampliado UAT - Fase 6D.2

Productivo permanece NO-GO. Todos los datos son sinteticos/anonimizados; no se usan cuentas, documentos, certificados ni payloads reales.

## Dataset ampliado propuesto

| ID dataset | Camara | Escenario | Tipo | Fuente/fixture/evidencia | Sensibilidad | Estado | Observacion |
| --- | --- | --- | --- | --- | --- | --- | --- |
| DS-EXP-ACH-OUT-002 | ACH Colombia | UAT-ACH-001/007/009 | Salida NACHA-M con lotes mixtos | Derivado controlado de golden semirreal | Anonimizado | Propuesto | Validar naming/totales en UAT |
| DS-EXP-ACH-IN-002 | ACH Colombia | UAT-ACH-002 | Entrada NACHA-M con addenda y estados variados | Fixture UAT sintetico a cargar | Anonimizado | Propuesto | Requiere ambiente UAT |
| DS-EXP-ACH-RET-002 | ACH Colombia | UAT-ACH-003/004 | `.RET` con causal homologada y ambigua | Fixture UAT sintetico | Sanitizado | Propuesto | Ambiguo debe ir a manual review |
| DS-EXP-CEN-OUT-002 | CENIT | UAT-CEN-001 | Salida CENIT con perfil sintetico | Fixture UAT sintetico | Anonimizado | Propuesto | Contraste CENIT pendiente |
| DS-EXP-CEN-IN-002 | CENIT | UAT-CEN-002 | Entrada CENIT con respuestas diferenciales | Fixture UAT sintetico | Anonimizado | Propuesto | Requiere carga UAT |
| DS-EXP-CEN-RET-002 | CENIT | UAT-CEN-003/004 | `.RET` CENIT con causales Anexo A/B | Fixture UAT sintetico | Sanitizado | Propuesto | Homologacion formal pendiente |
| DS-EXP-PRENOTE-APP | Ambas | UAT-ACH-005, UAT-EXP-004 | Prenotificacion aprobada | Reporte sintetico UAT | Sanitizado | Propuesto | No monetario |
| DS-EXP-PRENOTE-REJ | Ambas | UAT-ACH-006, UAT-EXP-004 | Prenotificacion rechazada | Reporte sintetico UAT | Sanitizado | Propuesto | No monetario/manual review si ambiguo |
| DS-EXP-ROR-002 | Ambas | UAT-CEN-006 | ROR con original trace enmascarado | Evidencia ROR sintetica | Sanitizado | Propuesto | Sin ejecucion monetaria |
| DS-EXP-CONC-MANUAL | Ambas | UAT-EXP-001 | Conciliacion manual review | Consola conciliacion | Sanitizado | Propuesto | Causal ambigua controlada |
| DS-EXP-CONC-INCONS | Ambas | UAT-EXP-002 | Conciliacion inconsistente | Consola conciliacion | Sanitizado | Propuesto | No muta estados |
| DS-EXP-CEN-CYCLE | CENIT | UAT-CEN-005, UAT-EXP-003 | Ciclo/cola/neteo sintetico | Dashboard/read-store UAT | Sanitizado | Propuesto | Sin movimiento real |
| DS-EXP-SOAP-READONLY | Ambas | UAT-TRV-003 | SOAP/UAT read-only | Playwright SOAP/UAT | Sanitizado | Disponible | SOAP real bloqueado |
| DS-EXP-CI-GUARDS | Ambas | UAT-EXP-005 | Guardas NO-GO/no mutacion/no hash | Playwright CI artifacts | No sensible | Disponible | Evidencia automatizada |

## Reglas de uso

- No modificar golden files existentes.
- No usar datos reales ni secretos.
- Marcar toda causal no homologada como `manual review`.
- No ejecutar SOAP real ni movimientos monetarios.
- No usar legacy como fuente oficial.
