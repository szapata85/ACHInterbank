# UAT asistida por IA — Punto 10: reportería, conciliación operativa y revisión contra terceros

## 1. Objetivo
Esta ejecución busca evidenciar técnicamente el comportamiento del punto 10 sobre el endpoint de exportación accounting-review.
No reemplaza usuarios reales, no constituye aprobación formal de Operaciones/Negocio y no habilita producción.

## 2. Alcance
- Endpoint `POST /api/reports/accounting-review/export`.
- Formatos: PDF/CSV/XLSX.
- Salida en español.
- Frontera no-contable explícita.
- Población desde servicios existentes (`IAchTransactionReportService`, `IAchReturnRejectionReportService`, `IAchNachaCycleReportService`, `IAchReconciliationReportService`, `IAchAuditHistoryReportService`).
- CUD tratado como warning operacional sin API.
- Revisión contra terceros soportada como evidencia operativa.

## 3. Fuera de alcance
- Contabilización.
- Asientos.
- Ledger/journal/posting.
- API contable.
- API CUD.
- Cierre productivo.
- Aprobación humana.

## 4. Casos automatizados
| ID | Caso | Formato | Validación principal | Resultado esperado |
|---|---|---|---|---|
| UAT-10-001 | Exportación PDF básica | PDF | ContentType/extensión/`%PDF`/frontera no-contable | Aprobado |
| UAT-10-002 | Exportación CSV básica | CSV | RESUMEN/FILAS/FRONTERA_NO_CONTABLE/NO contabiliza + exclusión ledger/journal/posting | Aprobado |
| UAT-10-003 | Exportación XLSX básica | XLSX | ZIP válido, hojas en español, sin macros ni fórmulas | Aprobado |
| UAT-10-004 | Datos salientes/entrantes | CSV | Referencias/montos/estados esperados, sin datos inventados | Aprobado |
| UAT-10-005 | Devoluciones y rechazos | CSV | Tipo visible, causal y descripción, sin contabilización | Aprobado |
| UAT-10-006 | Return of Return | CSV | `RetornoDeRetorno`, referencia ROR, sin asiento/posting | Aprobado |
| UAT-10-007 | Diferencias de conciliación | CSV | Diferencias de monto y conteo, frontera no-contable | Aprobado |
| UAT-10-008 | Evidencia NACHA | CSV | Evidencias con nombre de archivo, sin binarios/base64 | Aprobado |
| UAT-10-009 | Evidencia auditoría/trazabilidad | CSV | `audit-AchTransaction-*`, usuario/acción, sin secretos | Aprobado |
| UAT-10-010 | CUD solicitado sin fuente runtime | CSV | Warning CUD sin API, sin inventar evidencia CUD | Aprobado |
| UAT-10-011 | Protección formula injection | CSV/XLSX | Sanitización CSV y XLSX sin `<f>` | Aprobado |
| UAT-10-012 | Filtros/include flags combinados | CSV | Exclusión por flags y frontera no-contable | Aprobado |
| UAT-10-013 | Reporte vacío controlado | CSV | Archivo generado + warning sin datos fake | Aprobado |
| UAT-10-014 | Resumen UAT asistida | Texto en memoria | Totales + estado asistido + pendientes humanos + NO-GO | Aprobado |

## 5. Evidencia técnica automatizada
- Harness/prueba: `tests/Cfa.ACHInterbank.Tests/AccountingReviewUatEvidenceHarnessTests.cs`.
- Build: objetivo 0 warnings / 0 errors para el alcance backend.
- Suite completa CI: por validar en CI (si no hay job de publicación consolidado del artefacto UAT).
- Outputs en memoria (sin archivos físicos obligatorios).
- Sin DB real.
- Sin `AddPersistence()`.
- Sin servicios externos.

## 6. Resultado de UAT asistida
- **UAT asistida por IA:** APROBADA TÉCNICAMENTE (si tests pasan en esta ejecución).
- **GO UAT formal:** PENDIENTE APROBACIÓN HUMANA.
- **GO productivo:** NO-GO.

## 7. Evidencias que deben revisarse por humano
- [ ] Operaciones revisa PDF.
- [ ] Operaciones revisa CSV.
- [ ] Operaciones revisa XLSX.
- [ ] Negocio valida semántica de returns/rejections/ROR.
- [ ] Riesgo/Compliance valida frontera no-contable.
- [ ] Tecnología valida endpoint.
- [ ] Se firma acta UAT formal.

## 8. Riesgos residuales
- Datos UAT automatizados/mocks no reemplazan datos operativos reales.
- CUD no tiene API.
- Neteo/liquidez sin cierre E2E productivo.
- Falta firma humana.
- NO-GO productivo vigente.

## 9. Decisión recomendada
Con base en la UAT asistida por IA, el punto 10 queda listo para revisión humana de GO UAT controlado. No se recomienda GO productivo.
