# Validación ejecutada NACHA Config (evidencia real)

Fecha de ejecución: 2026-04-18 (UTC).

## Resumen de entorno

- Frontend: Node.js `v22.21.1`, npm `11.4.2`.
- Backend: `dotnet` no disponible en el entorno (`command not found`).
- Navegador headless: no hay binario Chrome/Chromium disponible para Karma.

## Comandos ejecutados y resultado

1. `npm ci` (web/ach-interbank-ui)
   - Resultado: **OK**.
2. `npx ng build` (web/ach-interbank-ui)
   - Resultado: **OK** (build completo con chunk lazy `features-nacha-config-admin`).
3. `npx ng test --watch=false --browsers=ChromeHeadless`
   - Resultado: **FAIL** por limitación de entorno:
     - `No binary for ChromeHeadless browser on your platform`.
     - error de Karma durante carga/procesamiento (`file-list` / `rimraf options`).
4. `npx ng test --watch=false --browsers=ChromeHeadless --include='src/app/features/nacha-config-admin/**/*.spec.ts'`
   - Resultado: **FAIL** por la misma limitación de entorno (Karma + ausencia de Chrome).
5. `dotnet build ACHInterbank.sln`
   - Resultado: **NO EJECUTABLE** (`dotnet: command not found`).
6. `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter "FullyQualifiedName~NachaConfig"`
   - Resultado: **NO EJECUTABLE** (`dotnet: command not found`).

## Smoke operativo integral (por capas) — Expected vs Real

| Paso | Flujo | Expected | Real |
|---|---|---|---|
| 1 | Abrir listado de perfiles | Lista visible en grilla | No ejecutable E2E (sin browser runner operativo) |
| 2 | Cargar catálogos de filtro | GET `catalogos-filtro` responde | Validado por contrato/compilación; no corrida E2E |
| 3 | Filtrar perfiles | Filtros estructurados + texto | No ejecutable E2E |
| 4 | Crear borrador | Alta exitosa con loading/disabled | No ejecutable E2E |
| 5 | Abrir workspace | Navegación al perfil | No ejecutable E2E |
| 6 | Editar perfil | Guarda con concurrencia | No ejecutable E2E |
| 7 | Editar secuencia | Guarda orden de registros | No ejecutable E2E |
| 8 | Editar variante | Selección guiada + guardar | No ejecutable E2E |
| 9 | Editar campo | Selección guiada + guardar | No ejecutable E2E |
| 10 | Editar regla | Selección guiada + guardar | No ejecutable E2E |
| 11 | Validar perfil | Muestra issues | No ejecutable E2E |
| 12 | Publicar bloqueado | Muestra `PUBLISH_BLOCKED` | No ejecutable E2E |
| 13 | Preview del resolvedor | Retorna perfil/layouts/traza | No ejecutable E2E |
| 14 | Ver historial | Grilla de cambios | No ejecutable E2E |
| 15 | Ver snapshots | Grilla de instantáneas | No ejecutable E2E |
| 16 | Conflicto de concurrencia | Alerta y recarga | No ejecutable E2E |

## Corrección aplicada durante validación

- Se corrigió un bloqueo de compilación de pruebas frontend fuera de NACHA Config (`transactionExternalId` requerido en `BulkAchTransactionItemResult`) para que la suite deje de fallar por ese error de tipos puntual.
