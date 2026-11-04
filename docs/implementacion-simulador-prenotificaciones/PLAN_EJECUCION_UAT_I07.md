# I07-R — guía de ejecución de pruebas de homologación pendientes

Este documento es una **guía reproducible**, no evidencia de ejecución. Entorno exigido: UAT/local aislado, SDK .NET, dependencias Angular, base de datos de prueba y usuarios con/sin permisos.

## Orden seguro

1. `dotnet build ACHInterbank.sln -c Release` y `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release`.
2. Desde `web/ach-interbank-ui`: `npm ci`, `npm run build`, ejecutar los tests unitarios con el comando definido por `package.json`; luego Playwright con UAT autenticado.
3. Preparar prenotificaciones SALIENTES reales de prueba para ACHCOL y CENIT; verificar exportación Transmitted/Acknowledged, intento activo y plazo.
4. Ejecutar `PRE-01..PRE-14` en ambos operadores: consultar y previsualizar; generar devolución con causal/fecha/ciclo válido; descargar por JWT; cargar **manualmente** en NachaUpload; verificar trace/adenda 99/importe cero/estado/intento; reupload duplicado; duplicidad y concurrencia.
5. Repetir sin devolución antes del plazo, exactamente al vencimiento y después. Hora virtual debe ser exclusivamente lectura. Validar job canónico, feriados y política propia de cada cámara.
6. Ejecutar con feature flag apagado, sesión expirada y permisos incompletos: HTTP 401/403 o bloqueos correctos sin efectos.
7. Capturar pantalla escritorio/tablet/móvil (375px, 768px y ancho de escritorio): filtros, opción, errores, modales, foco, lectores de pantalla y descarga.

## Condición de cierre

Cada PRE requiere resultado PASS o FAIL, comando, fecha, operador, insumos sintéticos, evidencia (hash, SQL/log, screenshot), y responsable. **No** convertir PASS_STATIC en PASS funcional. Si existe FAIL, corregirlo, volver a ejecutar la regresión y generar un nuevo ZIP completo con `MEMORY_STATE.md`/estado y hash.
