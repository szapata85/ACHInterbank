# Implantación por fases: Reportes QuestPDF en ACHInterbank

> Supuesto explícito: se toma como base el estado actual del branch (`a48eae0`) con MVP backend, UI Angular, navegación/permisos, plantillas reusables y hardening inicial ya integrados.

## Fase A — Backend MVP (`/api/reports/traceability/pdf`)

### Resumen
- Se habilitó endpoint `GET /api/reports/traceability/pdf`.
- Se reutiliza `IAchTraceabilityService` para datasource.
- Se retorna archivo `application/pdf` con nombre dinámico.

### Archivos cambiados (fase)
- `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs`
- `src/Cfa.ACHInterbank.Application/Reports/Interfaces/IReportGenerator.cs`
- `src/Cfa.ACHInterbank.Application/Reports/Models/GeneratedReportFile.cs`
- `src/Cfa.ACHInterbank.Application/Reports/Models/TraceabilityReportFilter.cs`
- `src/Cfa.ACHInterbank.Persistence/Reports/QuestPdfReportGenerator.cs`

### Pruebas ejecutadas
- Unit tests del controller agregados en `tests/Cfa.ACHInterbank.Tests/ReportsControllerTests.cs`.
- Ejecución en entorno local pendiente por ausencia de SDK .NET.

### Riesgos
- Dependencia de volumen de datos al no paginar contenido dentro del PDF.
- Riesgo de timeout bajo filtros amplios (mitigado más adelante en Fase E).

## Fase B — Frontend pages (`/reports` y `/reports/traceability`)

### Resumen
- Se creó módulo lazy `ReportsModule`.
- Se agregaron páginas:
  - Landing `/reports`
  - Formulario y descarga `/reports/traceability`
- Se implementó `ReportsApiService` con descarga de blob.

### Archivos cambiados (fase)
- `web/ach-interbank-ui/src/app/features/reports/reports.module.ts`
- `web/ach-interbank-ui/src/app/features/reports/reports-routing.module.ts`
- `web/ach-interbank-ui/src/app/features/reports/components/reports-home.component.*`
- `web/ach-interbank-ui/src/app/features/reports/components/traceability-report.component.*`
- `web/ach-interbank-ui/src/app/features/reports/services/reports-api.service.ts`

### Pruebas ejecutadas
- `npm run -s build` (Angular) en iteraciones previas.
- Validación visual intentada con Playwright en entregas previas (inestable en contenedor).

### Riesgos
- Dependencia del browser runtime para validación visual automatizada.
- UX depende de mensajes de error backend estables.

## Fase C — Navegación y permisos

### Resumen
- Se integró lazy-load en routing principal.
- Se agregó acceso de menú a `/reports`.
- Se aplicó control de acceso lectura ACH (`CanReadAch`).

### Archivos cambiados (fase)
- `web/ach-interbank-ui/src/app/app-routing.module.ts`
- `web/ach-interbank-ui/src/app/core/services/navigation.service.ts`

### Pruebas ejecutadas
- Build Angular en iteraciones previas.
- Validación funcional de ruta y guardas por revisión de configuración.

### Riesgos
- Configuraciones de menú backend podrían duplicar item si no se normaliza en origen.

## Fase D — Plantillas reusables + documentación

### Resumen
- Se creó base reusable para QuestPDF:
  - `BaseReportDocument<TModel>`
  - `ReportSectionComposer`
- Se publicó plantilla piloto `TraceabilityReportDocument`.
- Se agregó guía para nuevos desarrolladores.

### Archivos cambiados (fase)
- `src/Cfa.ACHInterbank.Persistence/Reports/Base/BaseReportDocument.cs`
- `src/Cfa.ACHInterbank.Persistence/Reports/Components/ReportSectionComposer.cs`
- `src/Cfa.ACHInterbank.Persistence/Reports/Documents/TraceabilityReportDocument.cs`
- `src/Cfa.ACHInterbank.Persistence/Reports/Models/TraceabilityReportDocumentModel.cs`
- `docs/reporting/how-to-create-a-report.md`

### Pruebas ejecutadas
- Validación estática de compilación conceptual por revisión de dependencias y contratos.

### Riesgos
- Si no se mantiene convención, nuevos reportes podrían volver a composición ad-hoc.

## Fase E — Hardening y pruebas

### Resumen
- Se incorporó observabilidad estructurada del endpoint:
  - usuario
  - filtros
  - fecha/hora
  - duración
  - tamaño de salida
- Se agregaron límites de rango de fecha y normalización segura.
- Se implementó timeout controlado con mensaje amigable.
- Se ampliaron pruebas unitarias de validación, timeout y baseline de performance.

### Archivos cambiados (fase)
- `src/Cfa.ACHInterbank.Api/Controllers/ReportsController.cs`
- `tests/Cfa.ACHInterbank.Tests/ReportsControllerTests.cs`

### Pruebas ejecutadas
- Intentos en entorno:
  - `dotnet build Cfa.ACHInterbank.sln`
  - `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj`
- Resultado actual: no ejecutables por falta de SDK .NET en contenedor.

### Riesgos
- Sin test run efectivo en entorno actual, la validación final depende de CI.
- Eventual tuning del timeout (30s) según carga real de producción.

## Próximos pasos operativos
1. Ejecutar pipeline CI con .NET SDK disponible y publicar resultados.
2. Medir p95/p99 de duración y tasa de timeouts por ventana de 7 días.
3. Definir política de límites por rol/perfil (operación vs auditoría).
4. Agregar pruebas de carga (k6/JMeter) con datasets representativos.
5. Extender plantilla para reporte #2 usando mismo framework reusable.
