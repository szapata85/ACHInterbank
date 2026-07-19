# Decisión final de preparación productiva de la SPA

Fecha de corte: 2026-07-18  
Rama local: `ACH-Interbank-Postgresql`

## Decisión

# NO-GO

No existe un perfil NACHA-M diferencial `RETORNO/ENTRADA` publicado y homologado ni un generador diferencial table-driven homologado. Por ello no fue posible producir, validar y cargar un archivo diferencial, correlacionarlo, invocar `RegistrarRespuestaTransaccion`, demostrar idempotencia o cerrar conciliación. Además, revisión manual y homologaciones continúan sin operaciones CRUD/resolución auditables, y Quartz conserva almacenamiento RAM con clustering deshabilitado. Estos son bloqueos internos de integridad y operación; no pueden degradarse a condiciones externas.

## Criterios evaluados

| Criterio | Resultado | Evidencia |
|---|---|---|
| Build frontend producción | Aprobado | `npm run build -- --configuration production`; bundle inicial 2.31 MB. |
| Build backend Release | Aprobado, 0 warnings y 0 errores | `dotnet build ACHInterbank.sln -c Release --no-restore -m:1`. |
| Rutas y menú | Remediado | 127 patrones efectivos auditados; crawler 79/79; menú backend como fuente de verdad; redirects canónicos y API aislada bajo `/api`. |
| Certificados | Ruta unificada | `/nacha-security/certificates`, con cámara/ambiente, inventario y alertas. |
| Separación ACHCOL/CENIT | Mejorada, no completa | Filtro de causales y trazabilidad CENIT; capacidades exclusivas conservadas. |
| Respuestas ACH | Consulta/dashboard mejorados | Un agregado backend; policies y loaders corregidos. |
| Revisión manual | **Bloqueada** | No existe resolución con causal, comentario, auditoría, idempotencia y concurrencia. |
| Homologaciones de estado | **Bloqueadas** | Pantalla y API de lectura, sin CRUD administrativo auditable. |
| Conciliación | **Bloqueada** | No existe resolución/reproceso gobernado ni evidencia diferencial E2E. |
| Scheduler | **Bloqueado** | `Quartz:JobStore:Mode=RAM`, `Clustered=false`. |
| Simulador entrante | Remediado y probado de forma dirigida | Modos explícitos, nombres normativos, trace propio y sin auto-upload. |
| Simulador diferencial | **Bloqueado de forma segura** | HTTP 409 sin perfil/generador homologado; no se creó artefacto. |
| SOAP diferencial | **No ejecutado** | Opt-in deshabilitado y paquete ausente; cero uploads y cero llamadas. |
| Idempotencia diferencial | **No demostrada** | No existe archivo diferencial para primera/segunda carga. |

## Avances verificables

- Menú runtime basado en datos persistidos, sin reinyección hardcodeada de rutas.
- Guards de lectura/gestión separados y denegación autenticada a `/unauthorized`.
- Ciclos conservan modelos de dominio distintos y una sola entrada canónica de menú.
- Certificados tienen una ruta canónica y filtros explícitos de cámara/ambiente.
- Dashboard de respuestas usa una única agregación backend en lugar de nueve solicitudes.
- Causales y trazabilidad CENIT filtran la cámara real.
- Herramientas UAT se ocultan y los endpoints responden 404 fuera del ambiente permitido.
- Simulador con permisos finos por modo y Live sin fallback genérico.
- Errores SOAP dejan de registrar respuestas completas.
- No se generaron migraciones ni se modificaron golden files.
- No se realizaron commits, push ni PR.
- Las rutas SPA que colisionaban con prefijos de API (`/ach-cycles`, `/transactions`, `/navigation` y aliases legacy) vuelven a entregar el shell HTML; los clientes Angular consumen los aliases `/api/*` y la autenticación del API permanece activa.

## Bloqueos internos

### Severidad crítica

1. **Perfil/generador diferencial inexistente o no homologado.** No puede generarse una respuesta NACHA-M normativa sin inventar posiciones o semántica.
2. **Flujo diferencial E2E no acreditado.** Faltan generación, validación table-driven, NachaUpload, clasificación, correlación, persistencia, transición, `RegistrarRespuestaTransaccion`, conciliación y segunda carga.
3. **Revisión manual no resolutiva.** No hay caso de uso autorizado para causal, comentario, auditoría, control de concurrencia e idempotencia.
4. **Scheduler no apto para múltiples instancias.** RAM y `Clustered=false` no previenen ejecución duplicada ni preservan historial operativo.

### Severidad alta

5. **Mappings de estado sin administración.** No hay CRUD por cámara, vigencia, prioridad, unicidad y auditoría.
6. **Conciliación solo consultiva.** No permite resolver o reprocesar diferencias con gobierno.
7. **NACHA Config Admin incompleto.** No se cerraron publicación/edición ni validación responsive integral.
8. **Generalización por cámara parcial.** Causales de rechazo, políticas y devoluciones CENIT siguen sin un modelo genérico/capability completo.

### Riesgos adicionales concretos

- La instalación npm reportó 22 vulnerabilidades en dependencias (2 bajas, 6 moderadas y 14 altas); no se aplicó una actualización destructiva automática.
- Los perfiles seed disponibles no son artefactos de homologación oficial.
- La prueba Playwright dirigida usa un harness controlado y no sustituye datos persistidos ni servicios locales reales.
- EF Core reporta una FK sombra `IncomingNachaProcessingEvent.IncomingNachaFileIngestionId1`, valores por defecto de enums y propiedades decimales de lote/transacción sin precisión explícita; deben corregirse con una migración compatible y pruebas de datos antes de producción.
- El runner Node emitió una aserción nativa `UV_HANDLE_CLOSING` después de devolver Playwright exit code 0; no afectó los resultados, pero requiere actualización/estabilización del tooling E2E.

## Evidencia de pruebas

Resultados ya observados:

| Prueba | Resultado |
|---|---|
| Restore .NET | Aprobado. |
| Build .NET Release, línea base | Aprobado, 0 warnings y 0 errores. |
| Tests .NET dirigidos iniciales | 285/285 aprobados. |
| Tests .NET dirigidos de simulador/postparse/navegación/SOAP | 31/31 aprobados después de recompilar. |
| Tests backend de respuestas | 82/82 aprobados. |
| Tests backend de aislamiento por cámara | 11/11 aprobados. |
| Build Angular producción, línea base | Aprobado. |
| Tests Angular dirigidos iniciales | 216/216 aprobados. |
| Tests Angular de navegación | 26/26 aprobados. |
| Tests Angular de respuestas | 79/79 aprobados. |
| Test Angular del simulador | 6/6 aprobados. |
| Playwright crawler, línea base | 68/79 aprobados; 11 fallidos. |
| Playwright dirigido de simulador | Último registro `passed`, sin failed tests; dos capturas disponibles. |
| Live diferencial | Omitido: opt-in deshabilitado y paquete no configurado. |

Resultados del *working tree* final:

| Batería | Resultado final exacto |
|---|---|
| `dotnet build ACHInterbank.sln -c Release --no-restore -m:1` | Aprobado; 0 warnings, 0 errores. |
| `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build` | 1847 aprobados, 1 omitido diagnóstico, 0 fallidos; total 1848. |
| `npm run build -- --configuration production` | Aprobado; bundle inicial 2.31 MB. |
| `npm test -- --watch=false --browsers=ChromeHeadless` | 414/414 aprobados; 0 fallidos. |
| Playwright crawler final | 79/79 aprobados; 0 omitidos, 0 fallidos. |
| Playwright crítico final | 3 aprobados, 1 omitido por opt-in Live, 0 fallidos. |
| Docker API/SPA modificados | Imágenes construidas; contenedores API y SPA recreados y `healthy`; SQL Server reutilizado sin recrear. |
| `/health/live` y `/health/ready` final | HTTP 200 en ambos; SPA y los dos WSDL locales también HTTP 200. |

Una suite final aprobada no elimina los bloqueos funcionales anteriores; una suite fallida agregaría un bloqueo adicional.

## Bloqueos externos

No se aportaron ni validaron certificados productivos, credenciales productivas, endpoints externos, conectividad de red financiera, ventanas ACH Colombia/CENIT ni aprobación formal UAT. No se intentó acceso externo. Sin embargo, la decisión actual no depende de esos elementos: el repositorio conserva bloqueos internos críticos que deben cerrarse primero.

## Procedimiento para levantar el NO-GO

1. Homologar y publicar un perfil `RETORNO/ENTRADA` por cámara.
2. Implementar el generador diferencial exclusivamente con el motor table-driven y agregar pruebas de posiciones, controles y nomenclatura.
3. Implementar transición/correlación persistida, respuestas huérfanas y auditoría sin fabricar identificadores.
4. Completar revisión manual, mappings y conciliación con permisos, concurrencia e idempotencia.
5. Migrar Quartz a un store persistente, habilitar clustering y demostrar exclusión multiinstancia/historial.
6. Ejecutar generación → validación → NachaUpload → correlación → `RegistrarRespuestaTransaccion` → conciliación en ambiente local controlado.
7. Repetir el mismo archivo y probar cero SOAP, estado, auditoría funcional y conciliación duplicados.
8. Probar transición posterior válida e inválida, huérfana, fallo temporal SOAP y respuesta SOAP negativa.
9. Corregir el mapeo EF de la FK sombra y la precisión decimal mediante una migración compatible, con pruebas SQL Server/PostgreSQL y de datos existentes.
10. Repetir la batería completa de build, tests, crawler, responsive y health checks después de cerrar los bloqueos funcionales.
11. Solo entonces evaluar certificados/conectividad/UAT externos y emitir una nueva decisión.
