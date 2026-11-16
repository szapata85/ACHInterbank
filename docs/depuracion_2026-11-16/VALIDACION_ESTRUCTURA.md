# Validación estática de estructura — 2026-11-16

- Extracción de la versión mínima: correcta.
- `python scripts/validate-solution-projects.py`: `SOLUTION GRAPH: OK`, 7/7 proyectos y referencias internas presentes.
- `ZipFile.testzip()`: sin errores en ambas distribuciones.
- Comparación SHA-256 archivo por archivo: todos los archivos originales conservados o reubicados son idénticos.
- Fuente conservada: 2.278 `.cs`, 615 `.ts`, 8 `.csproj`, 24 `.sql`, 7 `.wsdl` y 1 `.xsd`.
- `dotnet build` y `dotnet test`: no ejecutados, SDK no disponible en el entorno.
- `npm ci`/`npm run build`/Playwright: no ejecutados. Node disponible v22, el proyecto requiere Node 24 LTS.

**No sustituye validación compilada ni UAT de cámaras.**
