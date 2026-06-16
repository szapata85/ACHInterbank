# Acta Tecnica Preliminar Final - ACH Interbank

Fecha: 2026-06-12 America/Bogota
Version: 0.8 preliminar no firmada - addendum G3.5-G3.6
Rama: `ACH-Interbank-Postgresql`
Commit base vigente: `e57211506d381acc43d398e72277911720e6323e`
Ambiente: Docker Compose local, SPA `http://localhost:743`, API directa `http://localhost:843`
Caracter: acta tecnica preliminar; no sustituye acta UAT formal de negocio, operaciones, seguridad ni auditoria.

## Participantes Requeridos Para Firma Formal

| Rol | Nombre/Firma | Estado |
|---|---|---|
| QA funcional | Pendiente | No firmado |
| Arquitectura ACH/NACHA-M | Pendiente | No firmado |
| Seguridad | Pendiente | No firmado |
| Operaciones | Pendiente | No firmado |
| Negocio | Pendiente | No firmado |
| Auditoria | Pendiente | No firmado |

## Alcance Ejecutado

Se ejecuto estabilizacion final controlada de UAT/readiness con datos sinteticos y evidencias tecnicas:

- CI local backend y frontend.
- Revalidacion del test BatchResolver previamente inestable.
- Correccion de dependencia vulnerable `System.Security.Cryptography.Xml`.
- Diagnostico/cierre tecnico de catalogos NACHA-M layouts por API/proxy.
- Diagnostico del rol `ACH.Operator` no visible en JWT/respuesta.
- Actualizacion documental UAT/readiness.

No se usaron datos reales, cuentas reales, bancos productivos reales, certificados reales, NACHA-M productivo ni conexiones externas ACH Colombia/CENIT.

## CI Y Validaciones

| Control | Resultado | Observacion |
|---|---|---|
| GitHub `dotnet-ci` | OK | Workflow remoto exitoso para commit `49b810f9`. |
| GitHub `angular-ci` | OK rama | Ultimo workflow remoto de rama exitoso en commit `469e2a60`; no se disparo para `49b810f9` por cambios sin impacto Angular. |
| `dotnet build ACHInterbank.sln -c Release` | OK | 0 errores. |
| `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release` | OK | 1091 OK, 1 omitida, 0 fallas tras ajustar fixture deterministico. |
| `npm run build` | OK | Build Angular OK; advertencias Browserslist no bloqueantes. |
| `npm test -- --watch=false --browsers=ChromeHeadless` | OK | 147 specs OK; quedan warnings template/testbed no bloqueantes. |
| `docker compose config --quiet` | OK | Compose valido. |
| `docker compose build achinterbank-spa` | OK | SPA reconstruida para aplicar Nginx. |
| `docker compose up -d` | OK | PostgreSQL healthy, API Up, SPA Up. |

## Runtime

| Componente | Estado | Evidencia |
|---|---|---|
| PostgreSQL | Healthy | `docker compose ps`; sin borrado de volumenes. |
| API | Up | Health checks y endpoints protegidos responden. |
| SPA Docker | Up | Nginx sirve SPA y proxya rutas API confirmadas. |
| Proxy Nginx | OK tecnico | `/auth`, `/navigation`, `/api`, `/health`, `/openapi`, `/scalar`, rutas funcionales y NACHA layouts salen por API. |
| Health | OK | `GET /health/live` y `GET /health/ready` por `:743` responden 200. |

## Resultado UAT

| Area | Resultado | Observacion |
|---|---|---|
| UAT tecnico autenticado | OK con observaciones | Login demo, token, menu, roles/permisos y endpoints read-only validados. `ACH.Operator` no esta asignado al usuario demo. |
| UAT funcional sintetico | PARCIALMENTE OK | Transacciones sinteticas, trazabilidad nueva, idempotencia documental y proxy SPA/API OK; faltan actas, evidencia visual y UAT bancario formal. |
| DEF-UAT-016 | Cerrado | Proxy funcional SPA corregido para rutas raiz. |
| DEF-UAT-017 | Cerrado funcionalmente | Nuevas transacciones generan evento inicial `Pending -> Pending`, `System`, `CREATED`; sin backfill historico. |
| DEF-UAT-018 | Cerrado documentalmente | Contrato actual observado documentado: duplicado retorna 400 JSON controlado, sin replay/idempotency key. |
| DEF-UAT-019 | Cerrado tecnicamente | Rutas reales `/nacha-layouts` y `/nacha-record-definitions` responden JSON por API directa y `:743`. |

## Defectos Abiertos Relevantes

| ID | Severidad | Estado | Resumen |
|---|---|---|---|
| DEF-UAT-003 | Alta | En analisis | SPA consume endpoints de interoperabilidad NACHA security sin backend equivalente confirmado. |
| DEF-UAT-005 | Alta | Abierto | `.env` versionado requiere revision/rotacion si contiene secretos reutilizables. |
| DEF-UAT-007 | Media | Cerrado | El stack local no requiere un servicio externo de secretos; la custodia real queda bajo mecanismo corporativo aprobado. |
| DEF-UAT-009 | Media | Abierto | Health checks no cubren Quartz ni dependencias externas. |
| DEF-UAT-010 | Bloqueante | Abierto | CENIT/CUD sin evidencia operacional homologada. |
| DEF-UAT-014 | Media | Abierto | Evidencia visual automatizada pendiente por limitacion de browser integrado. |
| DEF-UAT-015 | Media | Cerrado para UAT controlado | Usuario demo `admin` evidencia roles `Admin` y `ACH.Operator` por seed/migracion controlada; evaluar usuario operador separado antes de productivo si seguridad lo exige. |
| DEF-UAT-020 | Alta/Media | Abierto | Validacion NACHA-M campo-a-campo y homologacion externa de registros 1/5/6/7/8/9 pendiente. |

## Riesgos

- Productivo no puede aprobarse sin UAT formal firmado.
- CENIT/CUD, sobre digital, certificados, naming externo y backup/restore siguen sin evidencia de homologacion.
- `ACH.Operator` requiere decision: asignarlo al usuario demo por seed/migracion controlada o declarar que `Admin` cubre UAT tecnico.
- El contrato de idempotencia actual usa HTTP 400; migrar a 409, `Idempotency-Key` o replay requiere aprobacion de arquitectura/API.
- La validacion tecnica NACHA-M no reemplaza firma normativa campo-a-campo ni vector externo.

## Decision Tecnica Preliminar

El ambiente queda **apto para continuar UAT tecnico/funcional controlado con observaciones**, usando datos sinteticos/anonimizados y sin conexiones externas reales.

Estado productivo: **NO-GO**.

## Addendum tecnico G3.5-G3.6

| Fase | Estado | Commit | Evidencia/observacion |
|---|---|---|---|
| G3.5 | GO tecnico con observaciones | `7c3cbb21bc35dd253334b7edac1deef16efadabc` | Naming `RRRRTTT.ZZZ.N`; ciclo desde `AchCycles.CycleName` con exactamente un entero positivo |
| G3.5.1 | GO tecnico con observaciones | `ebf7a8a5569cbfc5f4d2c74a919f83b86b479c3e` | OpenBao/HashiCorp Vault retirado; KeyVault inventariado aparte |
| G3.5.2 | Cerrado | `c7a5ad50f66914e5802fc6df9f07567f28871dbd` | Migraciones deshabilitadas por defecto; sin dependencia residual activa |
| G3.6A | GO tecnico | `e57211506d381acc43d398e72277911720e6323e` | SPA/API/PostgreSQL/Quartz reales; 2/2; `Proc_Transacciones` dry-run |
| G3.6B | GO tecnico con observacion | `e57211506d381acc43d398e72277911720e6323e` | 2/2; `Proc_Contrapartidas` dry-run; correlacion por `AchCycleId`, no causalidad |

Validacion asociada:

- Build Release: 0 warnings, 0 errores.
- Backend: 1652 aprobadas, 1 omitida.
- Angular: 347/347.
- G3.6 Playwright: 4/4.
- Quartz real: evidenciado mediante `TaskExecutionLog`.

Este addendum no declara homologacion externa, SOAP real, movimiento monetario ni autorizacion productiva. Los artefactos locales `test-results` no se anexan ni se interpretan como evidencia normativa.

## Condiciones Para Cierre Formal

1. Ejecutar UAT formal con datos anonimizados representativos y actas firmadas.
2. Adjuntar evidencia visual sanitizada de SPA autenticada si el comite la exige.
3. Resolver o aceptar formalmente `ACH.Operator` para el usuario demo/roles UAT.
4. Validar NACHA-M 1/5/6/7/8/9 campo-a-campo y con evidencia externa o waiver.
5. Cerrar CENIT/CUD, sobre digital, certificados, backup/restore y secretos.
6. Mantener productivo en **NO-GO** hasta aprobaciones humanas y validaciones externas.
