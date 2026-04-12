# Arquitectura objetivo — Módulo de Reportes ACH Interbank

## 1) Objetivo y alcance

Diseñar un módulo de reportes enterprise-ready para operación ACH con:

- Trazabilidad completa por cámara, ciclo, lote y archivo NACHA.
- Soporte a créditos, débitos, prenotificaciones, reversos y devoluciones.
- Reportes operativos + regulatorios + auditoría + histórico.
- Exportación PDF corporativa con branding dinámico.

## 2) Principios de arquitectura (obligatorios)

### Backend (.NET/C#)

- Controllers delgados, sin lógica pesada.
- Casos de uso en Application (queries/handlers) con validaciones de negocio.
- Repositorios/queries optimizados en Persistence con filtros server-side.
- DTOs específicos por reporte (evitar modelos genéricos ambiguos).
- Paginación obligatoria para vistas de consulta.
- Capa de PDF desacoplada (orquestador + documentos de plantilla).

### Frontend (Angular)

- Grids/listados homogéneos: `loading`, `empty`, `error`, `data`.
- Filtros claros por cámara/ciclo/estado/rango/lote/archivo.
- Server-side pagination/sorting/filtering.
- Misma experiencia de UX para todos los reportes.

### PDF corporativo

- Generación vía API (no en cliente).
- Branding dinámico (logo/colores institucionales).
- Layout no plano: header, título, filtros aplicados, tabla, totales, footer.
- Firma de metadatos: generado por, fecha/hora UTC, correlativo.

---

## 3) Catálogo de reportes y modelo de dominio

### 3.1 Reportes requeridos

1. Transacciones enviadas
2. Transacciones recibidas
3. Devoluciones
4. Rechazos
5. Archivos NACHA
6. Ciclos
7. Conciliación
8. Totales
9. Auditoría
10. Histórico
11. Por cámara
12. Por lote

### 3.2 Ejes de filtro estándar

- `DateFromUtc`, `DateToUtc`
- `ClearingHouseId` (ACH Colombia / CENIT)
- `CycleId`
- `BatchId`
- `NachaFileId`
- `TransactionType` (Credit, Debit, Prenotification, Reversal, Return)
- `State` / `ReturnReasonCode`
- `CustomerId` / `InstitutionId` (cuando aplique)

### 3.3 Reglas funcionales clave

- No romper histórico (consultas inmutables por fecha de corte).
- Los reportes de devolución/rechazo deben exponer causal (`Rxx/DEVxx`).
- Los reportes NACHA deben soportar resumen por registros tipo `1/5/6/7/8/9`.
- Conciliación debe comparar origen ACH vs estado operativo interno.

---

## 4) Arquitectura Backend objetivo

## 4.1 API layer (Controllers)

Mantener un único `ReportsController` y agregar endpoints versionables por recurso:

- `GET /api/reports/transactions/sent`
- `GET /api/reports/transactions/received`
- `GET /api/reports/returns`
- `GET /api/reports/rejections`
- `GET /api/reports/nacha-files`
- `GET /api/reports/cycles`
- `GET /api/reports/reconciliation`
- `GET /api/reports/totals`
- `GET /api/reports/audit`
- `GET /api/reports/history`
- `GET /api/reports/by-clearing-house`
- `GET /api/reports/by-batch`

Y para PDF:

- `GET /api/reports/{reportKey}/pdf`

> Todos con policy mínima `CanReadAch`, y policies granulares para auditoría/conciliación.

## 4.2 Application layer

Crear `Features/Reports` por reporte:

- `Queries/GetSentTransactionsReportQuery`
- `Queries/GetReceivedTransactionsReportQuery`
- ...
- `Queries/GetAuditReportQuery`

Cada query retorna:

- `PagedResult<TDto>` para UI.
- `ReportDataset<TDto>` para PDF (sin paginar o con chunking controlado).

Servicios transversales:

- `IReportQueryValidator` (rango de fechas, combinaciones válidas de filtros).
- `IReportAuthorizationService` (restricciones por rol/cámara/institución).
- `IReportOrchestrator` (normalización de filtros + dispatch + metadatos).

## 4.3 Persistence layer

- Repositorios/QueryServices especializados por reporte:
  - `ITransactionsReportReadRepository`
  - `IReturnsReportReadRepository`
  - `INachaFilesReportReadRepository`
  - `IReconciliationReportReadRepository`
  - `IAuditReportReadRepository`
- SQL parametrizado con:
  - filtros server-side
  - sorting seguro (whitelist)
  - paginación (`OFFSET/FETCH`)
  - proyecciones DTO directas

## 4.4 Contratos recomendados

- `ReportFilterBaseDto`
- `PagedReportRequestDto`
- `PagedReportResponseDto<T>`
- `ReportTotalsDto`
- `ReportAuditTrailDto`
- `GeneratedReportFile` (ya existente)

---

## 5) Estrategia de PDF corporativo

## 5.1 Componentes

- `BaseReportDocument<TModel>` (ya existente como base reusable).
- `ReportSectionComposer` (título, filtros, tablas, totales).
- `ReportBrandingProvider` (logo, colores, razón social).
- `ReportFooterComposer` (paginación, timestamp, usuario solicitante).

## 5.2 Flujo

1. API recibe request y normaliza filtros.
2. Application ejecuta query y arma `ReportDocumentModel`.
3. Persistence/QuestPDF compone documento corporativo.
4. API retorna archivo con `content-type=application/pdf`.

## 5.3 Estándar visual del PDF

- Header: logo dinámico + nombre entidad + reporte.
- Bloque de filtros aplicados (fechas, cámara, ciclo, lote, estado).
- Tabla principal con zebra rows y columnas alineadas.
- Totales y subtotales (monto, cantidad, fallidos, devueltos).
- Footer: usuario, fecha UTC, página X de Y.

---

## 6) Diseño Angular objetivo

## 6.1 Módulo

Extender `features/reports` con sub-páginas:

- `/reports/transactions/sent`
- `/reports/transactions/received`
- `/reports/returns`
- `/reports/rejections`
- `/reports/nacha-files`
- `/reports/cycles`
- `/reports/reconciliation`
- `/reports/totals`
- `/reports/audit`
- `/reports/history`
- `/reports/by-clearing-house`
- `/reports/by-batch`

## 6.2 Patrón UI de cada pantalla

- Toolbar de filtros + acciones (`Buscar`, `Limpiar`, `Descargar PDF`).
- Grid/listado principal con:
  - `loading`
  - `empty`
  - `error`
  - `data`
- Resumen de resultados (cantidad, totales monetarios).
- Paginación, ordenamiento y filtros server-side.

## 6.3 Servicios frontend

- `ReportsApiService` segmentado por endpoint.
- Modelos por reporte (`SentTransactionsRow`, `ReturnsRow`, etc.).
- Normalización de query params y mapping DTO→ViewModel.

---

## 7) Endpoints (detalle propuesto mínimo)

Ejemplo base:

- `GET /api/reports/returns?page=1&pageSize=25&dateFromUtc=...&dateToUtc=...&clearingHouseId=...&cycleId=...&returnReasonCode=R01`
  - `200`: `PagedReportResponseDto<ReturnRowDto>`
  - `400`: validación de filtros
  - `403`: autorización

- `GET /api/reports/returns/pdf?dateFromUtc=...&dateToUtc=...&clearingHouseId=...`
  - `200`: archivo PDF
  - `408`: timeout de generación
  - `500`: error controlado

---

## 8) Observabilidad, auditoría y calidad

- Logging estructurado por `reportKey`, usuario, filtros, duración y tamaño de salida.
- Métricas: p95/p99 de generación, tasa de timeouts, volumen por tipo de reporte.
- Trazabilidad de descarga (quién, cuándo, qué filtros, resultado).
- Pruebas:
  - Unit: validadores de filtros y handlers.
  - Integration: endpoints críticos con paginación/sorting.
  - Contract tests: shape de DTOs.
  - UI smoke: estados loading/empty/error + descarga PDF.

---

## 9) Archivos a modificar (plan de implementación)

## Backend API/Application

- `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs`
- `src/Cfa.ACHInterbank.Application/Reports/Interfaces/IReportGenerator.cs`
- `src/Cfa.ACHInterbank.Application/Reports/Models/*` (nuevos DTOs/filtros)
- `src/Cfa.ACHInterbank.Application/Features/Reports/*` (queries/handlers)

## Persistence/PDF

- `src/Cfa.ACHInterbank.Persistence/Reports/QuestPdfReportGenerator.cs`
- `src/Cfa.ACHInterbank.Persistence/Reports/Documents/*` (nuevos documentos)
- `src/Cfa.ACHInterbank.Persistence/Reports/Models/*` (models por PDF)
- `src/Cfa.ACHInterbank.Persistence/Reports/Components/ReportSectionComposer.cs` (si se amplía catálogo)

## Frontend Angular

- `web/ach-interbank-ui/src/app/features/reports/reports-routing.module.ts`
- `web/ach-interbank-ui/src/app/features/reports/services/reports-api.service.ts`
- `web/ach-interbank-ui/src/app/features/reports/components/*` (nuevas pantallas)
- `web/ach-interbank-ui/src/app/features/reports/models/*` (nuevos view models)

## Testing

- `tests/Cfa.ACHInterbank.Tests/*Reports*` (unit/integration)
- `web/ach-interbank-ui/src/app/features/reports/**/*.spec.ts` (component/service specs)

---

## 10) Cierre técnico

Esta arquitectura mantiene separación de capas, evita lógica duplicada, soporta operación ACH multi-cámara/multi-ciclo, preserva histórico y habilita reportes PDF corporativos con trazabilidad y gobernanza enterprise.
