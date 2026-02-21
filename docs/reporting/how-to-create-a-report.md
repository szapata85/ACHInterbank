# Cómo crear un nuevo reporte con QuestPDF

Esta guía define una base reusable para que un desarrollador nuevo cree reportes rápido, copiando una plantilla y ajustando solo **datasource + columnas**.

## Convención de nombres

- Documento: `<NombreReporte>ReportDocument` (ej: `DailySettlementReportDocument`)
- Modelo de documento: `<NombreReporte>ReportDocumentModel`
- Método en generador: `Generate<NombreReporte>PdfAsync(...)`
- Archivo PDF: `ACH_<NombreReporte>_yyyyMMdd_HHmmss.pdf`

## Estructura recomendada

Ubicación en `Cfa.ACHInterbank.Persistence/Reports`:

- `Base/BaseReportDocument<TModel>`: layout corporativo (header/footer + estilos base)
- `Components/ReportSectionComposer`: bloques reusables
  - título + metadatos
  - filtros
  - tabla
- `Models/<NombreReporte>ReportDocumentModel`
- `Documents/<NombreReporte>ReportDocument`
- `QuestPdfReportGenerator`: orquestación/datasource + invocación del documento

## Pasos rápidos (plantilla)

1. **Crear el model del documento**
   - Copia `TraceabilityReportDocumentModel`.
   - Define `Filter`, `Rows` y `GeneratedAtUtc`.

2. **Crear el documento del reporte**
   - Copia `TraceabilityReportDocument`.
   - Hereda de `BaseReportDocument<TModel>`.
   - En `ComposeBody` reutiliza `ReportSectionComposer` para:
     - `ComposeTitleAndMetadata`
     - `ComposeFiltersBlock`
     - `ComposeDataTable`

3. **Conectar datasource**
   - Reutiliza un servicio de aplicación existente (ej. `IAchTraceabilityService`).
   - Evita lógica de acceso a datos dentro del documento; el documento solo renderiza.

4. **Actualizar el generador**
   - En `QuestPdfReportGenerator`, consultar datos + mapear al model.
   - Instanciar el documento y retornar `GeneratedReportFile`.

5. **Exponer endpoint (si aplica)**
   - Validar filtros básicos (fechas, rangos, etc.).
   - Mantener autorización consistente con lectura ACH.

## Checklist de calidad

- [ ] El documento usa `BaseReportDocument<TModel>` (no define header/footer ad-hoc).
- [ ] Usa `ReportSectionComposer` para secciones estándar.
- [ ] Soporta caso sin datos con mensaje claro.
- [ ] Mantiene separación de capas (Api/Application/Persistence).
- [ ] No rompe endpoints existentes.
- [ ] Incluye pruebas mínimas del endpoint (éxito + validación básica).
- [ ] Nombre de archivo PDF sigue convención.
- [ ] Textos del reporte y filtros están claros en español.

## Referencia de implementación

Usa como piloto:

- `Documents/TraceabilityReportDocument`
- `Models/TraceabilityReportDocumentModel`
- `Base/BaseReportDocument<TModel>`
- `Components/ReportSectionComposer`

