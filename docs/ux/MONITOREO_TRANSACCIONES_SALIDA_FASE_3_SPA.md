# Monitoreo de transacciones de salida — Fase 3 SPA

## 1. Objetivo y línea base

La Fase 3 cierra la experiencia de consulta, la navegación persistida y la verificación del monitor dentro del stack Docker real. Se trabajó sobre la rama `ACH-Interbank-Postgresql`, con `4b40e8d197350bff989c44c94834126cebe21594` como `HEAD` inicial y con el commit funcional de Fase 2 `31d740ccc864c244fe8dfc0f211ceacc242e3b3a` confirmado como ancestro.

La implementación conserva `/api/transactions/outgoing-monitoring` y `/api/transactions/outgoing-monitoring/{id}` como contratos de lectura, y `/transactions/outgoing-monitoring` como ruta de la SPA. No se modificó el modelo funcional, no se ejecutaron servicios SOAP y no hubo movimientos monetarios.

## 2. Inventario inicial y brechas cerradas

| Elemento | Estado inicial | Brecha | Cierre |
| --- | --- | --- | --- |
| Permiso de consulta | Existente | Faltaba demostrar integración repetible | Seed y pruebas relacionales verifican una sola asignación |
| Permiso técnico | Existente | Faltaba prueba del seed | Seed y pruebas verifican asignación exclusiva a Admin |
| Opción de menú | Existente | La actualización recreaba todas sus relaciones | Actualización controlada que preserva identidad y relaciones correctas |
| Ruta y padre | Existentes | Faltaba evidencia posterior al reinicio | Ruta canónica y padre `Transacciones` comprobados en Docker |
| Filtros | Existentes | Faltaban validaciones cruzadas | Fechas, rango máximo e importes validados en formulario reactivo |
| Estados visuales | Parciales | Resultados podían coexistir con carga | Carga, error, vacío y resultados son mutuamente excluyentes |
| Detalle | Existente | Faltaban estados explícitos sin archivo y sin autorización | Mensajes operativos y respuesta 401/403 controlada |
| Diseño adaptable | Móvil básico | Faltaba tableta | Tarjetas adaptables comprobadas en 768×1024 y 390×844 |
| Verificación Docker | No documentada | Faltaba ejecución real | Build, seed repetido, reinicio, permisos y Playwright completados |

## 3. SPA

La pantalla usa Angular Material y formularios reactivos. Incluye encabezado humanizado, actualización manual, filtros con `mat-error`, consulta explícita, limpieza, tabla Material en escritorio, tarjetas en tableta y móvil, paginación de servidor y una única acción: ver detalle.

Los filtros eliminan valores vacíos, normalizan texto, vuelven a la primera página al consultar y validan:

- fecha inicial no posterior a la final;
- rango máximo de 90 días;
- importes no negativos;
- importe mínimo no superior al máximo.

La cuadrícula conserva el total informado por la API y no pagina colecciones localmente. La secuencia de solicitudes evita que la cancelación de una consulta anterior oculte prematuramente el indicador de carga de la consulta vigente. Los filtros se conservan al navegar al detalle y regresar.

## 4. Detalle y línea de tiempo

El detalle presenta resumen, seguimiento, resultado, situación posterior, archivos exactamente informados por la API, causal, advertencias y línea de tiempo. La aceptación permanece visible cuando existe una devolución posterior. Los eventos técnicos no se convierten en rechazos funcionales.

Cuando no existe archivo se muestra: `Esta transacción todavía no tiene un archivo NACHA-M asociado.` Cuando falta información externa se indica de forma prudente, sin inferir transmisión. La información técnica solo se renderiza cuando el contrato autorizado la incluye.

La línea de tiempo distingue creación, clasificación, ciclo, archivo, protección, transmisión comprobada, acuse, aceptación, certificación, rechazo, devolución, error técnico y revisión. Cada estado combina texto e icono; no depende únicamente del color.

## 5. Estados visuales, accesibilidad y diseño adaptable

Se verificaron carga inicial, actualización, consulta, cambio de página, error recuperable, acceso denegado, ausencia de transacciones y ausencia de archivo. Los mensajes permanecen en español y no exponen excepciones, JSON, XML ni URL internas.

La pantalla incorpora encabezados semánticos, leyenda accesible para la tabla, nombres concretos para acciones, `aria-busy`, foco visible y objetivos táctiles mínimos. Los puntos de quiebre muestran tarjetas por debajo de 900 px y una sola columna por debajo de 600 px. Playwright confirmó ausencia de desplazamiento horizontal global en 1440×900, 768×1024 y 390×844; la compilación y los estilos cubren 1280×720 con el mismo modo de escritorio.

## 6. Opción de menú, permisos y seed

| Propiedad | Valor canónico |
| --- | --- |
| Nombre visible | `Transacciones de salida` |
| Clave funcional estable | Ruta canónica `/transactions/outgoing-monitoring` |
| Identificador persistido comprobado | `4807` |
| Ruta | `/transactions/outgoing-monitoring` |
| Ícono | `monitoring` |
| Orden | `6` |
| Menú padre | `Transacciones`, identificador `6` |
| Estado | Activo |
| Permiso requerido | `OutgoingTransactions.Monitor.Read` |
| Permiso técnico | `OutgoingTransactions.Monitor.TechnicalDetail.Read` |

`OutgoingTransactionMonitoringSeeder` participa en `DbInitializer.SeedAllAsync`, ejecutado durante el arranque y mediante `POST /Maintenance/seed` con autorización administrativa. El seed localiza la opción por su ruta canónica estable, actualiza nombre, ruta, icono, orden, padre y estado sin borrar el registro canónico, y desactiva posibles duplicados históricos. Solo elimina relaciones incorrectas o repetidas y conserva las relaciones correctas existentes.

La lectura funcional se asigna a `Admin` y `ACH.Operator`; el detalle técnico se asigna exclusivamente a `Admin`. La navegación requiere simultáneamente relación del rol con el menú y permiso vigente. La ruta Angular usa `permissionGuard` y la API continúa siendo la autoridad final.

## 7. Evidencia de idempotencia en Docker

El stack construido utilizó `achinterbank-api:2026.07.24`, `achinterbank-spa:2026.07.24` y SQL Server `2025-latest`. La primera y segunda ejecución explícita de `POST /Maintenance/seed` finalizaron correctamente. Una tercera ejecución ocurrió mediante el bootstrap posterior al reinicio de API y SPA.

En las tres comprobaciones se conservó el identificador `4807`. Las lecturas de base de datos mostraron:

- una opción con la ruta canónica;
- un permiso `OutgoingTransactions.Monitor.Read`;
- un permiso `OutgoingTransactions.Monitor.TechnicalDetail.Read`;
- una relación de la opción con el permiso de lectura;
- una asignación funcional para `Admin` y una para `ACH.Operator`;
- una asignación técnica únicamente para `Admin`.

Después del reinicio, la API de navegación devolvió una sola ocurrencia de la ruta y la SPA mantuvo disponible la opción. No se realizaron inserciones ni actualizaciones SQL manuales; las consultas SQL fueron exclusivamente de lectura.

Las pruebas `OutgoingTransactionMonitoringSeederTests` demuestran creación, triple ejecución sin duplicados, identidad estable, corrección controlada de un registro previo, padre y permiso correctos, y visibilidad condicionada por autorización.

## 8. Construcción y salud Docker

Se ejecutó `docker compose build achinterbank-api achinterbank-spa` y luego `docker compose up -d achinterbank-api achinterbank-spa`. Las imágenes nuevas se usaron al recrear los servicios, sin eliminar volúmenes.

| Elemento | Evidencia |
| --- | --- |
| SQL Server | Contenedor saludable y volumen persistente conservado |
| API | `http://localhost:843/health/live` y `/health/ready`: HTTP 200 JSON |
| SPA | `http://localhost:743`: HTTP 200 HTML |
| Proxy | `http://localhost:743/health/ready`: HTTP 200 JSON |
| Autorización API | Monitor sin credenciales: HTTP 401 |
| Reinicio | API y SPA saludables; opción `4807` y conteos sin cambios |

No hubo respuestas 502 ni HTML inesperado en endpoints JSON comprobados.

## 9. Playwright contra Docker

La ejecución final fijó `E2E_BASE_URL=http://localhost:743` y `E2E_API_URL=http://localhost:843`; por ello Playwright no inició `ng serve`. Se usaron la SPA, la API, la autenticación y SQL Server reales, sin mocks ni interceptación de respuestas inventadas.

Los cuatro escenarios aprobaron:

1. escritorio: menú, filtros, paginación, detalle, archivo exacto y línea de tiempo;
2. móvil: tarjetas y ausencia de desplazamiento horizontal;
3. tableta: filtros, resultados y ausencia de desplazamiento horizontal;
4. permisos reales: usuario temporal sin roles, menú oculto, ruta redirigida y API 403, seguido de desactivación del usuario.

La instrumentación verificó ausencia de errores de consola, respuestas 5xx y solicitudes inesperadamente fallidas durante el monitor. Las capturas de escritorio, detalle, tableta y móvil se conservaron como resultados locales de Playwright y no se agregaron al repositorio.

## 10. Pruebas ejecutadas

| Validación | Resultado |
| --- | --- |
| Build de solución .NET Release | Correcto, 0 advertencias y 0 errores |
| Pruebas focalizadas backend del monitor | 16 aprobadas, 0 fallidas, 0 omitidas |
| Pruebas nuevas del seed | 4 aprobadas, 0 fallidas, 0 omitidas |
| Build Angular de producción | Correcto |
| Pruebas Angular focalizadas | 17 aprobadas, 0 fallidas |
| Regresión Angular completa | 681 aprobadas, 0 fallidas |
| Regresión backend general | 2106 aprobadas, 0 fallidas, 7 omitidas preexistentes |
| Playwright final contra Docker | 4 aprobadas, 0 fallidas, 0 omitidas |

La ejecución backend focalizada inicial por nombre incluyó accidentalmente las dos pruebas relacionales externas del monitor y confirmó su guardia obligatoria de configuración. Se corrigió únicamente el filtro de ejecución a `Category!=OutgoingMonitorMultiDb`; no se debilitó ni modificó esa suite.

## 11. Elementos reservados para Fase 4

La Fase 3 no certifica procesos financieros ni salida a producción. Quedan reservados para la validación integral de Fase 4:

- creación y recorrido operativo integral de nuevos casos financieros;
- certificación normativa de respuestas, rechazos y devoluciones;
- procesamiento y transmisión reales de archivos NACHA-M;
- ejecuciones Live de servicios monetarios o de registro de respuestas;
- pruebas monetarias, de no duplicación integral y de rendimiento masivo;
- matriz UAT y decisión de salida a producción.

## 12. Veredicto

**FASE 3 SPA CERRADA.** El monitor, su navegación persistida, permisos, seed idempotente, experiencia adaptable y ejecución real en Docker quedaron comprobados. No existen pendientes dentro del alcance de Fase 3 y la solución queda preparada para la validación integral de Fase 4 sin reabrir las reglas funcionales cerradas en Fase 2.
