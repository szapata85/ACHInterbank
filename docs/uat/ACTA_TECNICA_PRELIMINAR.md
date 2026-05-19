# Acta Tecnica Preliminar - UAT Funcional Sintetico ACH Interbank

Fecha: 2026-05-18 America/Bogota  
Version: 0.1 preliminar no firmada  
Rama: `fix/spa-docker-runtime-proxy-and-images`  
Commit: `261b1e0537e5d941f4d5f39c28bc4dc06d24f805`  
Ambiente: Docker Compose local, SPA `http://localhost:743`, API directa `http://localhost:843`  
Caracter: tecnico preliminar; no sustituye acta UAT formal de negocio, operaciones, seguridad ni auditoria.

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

Se ejecuto una prueba funcional sintetica controlada con usuario demo `admin`, sin imprimir password ni token completo, usando datos anonimizados:

- Documento sintetico `999999999`.
- Cuentas sinteticas `0000000001` y `0000000002`.
- Bancos sinteticos `Banco UAT Origen` y `Banco UAT Destino`.
- Referencia `UAT-SINT-001`.
- Monto `1000`.

No se usaron datos reales, cuentas reales, bancos productivos reales, certificados reales, NACHA-M productivo ni conexiones externas ACH Colombia/CENIT.

## Resultado

| Area | Resultado | Observacion |
|---|---|---|
| UAT tecnico autenticado | OK con observaciones | Login, token, menu y endpoints protegidos validados. |
| Datos maestros | OK con observaciones | Datos suficientes via API directa; algunos catalogos/configuraciones requieren cierre formal. |
| Transaccion sintetica | OK API directa | Creacion HTTP 201, persistencia y estado `Pending`. |
| Idempotencia | OK controlado con observacion | Reintento rechazado por duplicado con HTTP 400. |
| Trazabilidad | PARCIAL | Estado y timestamps presentes; evento inicial ausente. |
| Conciliacion basica | OK lectura | Endpoint responde 200 para ciclo/fecha sinteticos. |
| SPA Docker funcional | FALLA | Rutas funcionales raiz devuelven `index.html` en `:743`. |
| Productivo | NO-GO | Siguen pendientes actas, UAT bancario, homologaciones externas y brechas criticas. |

## Defectos Relevantes

| ID | Severidad | Estado | Resumen |
|---|---|---|---|
| DEF-UAT-016 | Bloqueante UAT SPA | Abierto | Rutas funcionales raiz no proxied por SPA Docker devuelven `index.html`. |
| DEF-UAT-017 | Alta/Media | Abierto | No se genera evento inicial de estado para la transaccion sintetica. |
| DEF-UAT-018 | Media | Abierto | Idempotencia controlada, pero contrato HTTP/idempotency key no esta formalizado. |
| DEF-UAT-019 | Media/Baja | Abierto | Catalogo/layout NACHA-M esperado no disponible como endpoint validado. |

## Decision Tecnica Preliminar

El UAT funcional sintetico queda **PARCIALMENTE OK** para el core API directo: datos maestros suficientes, transaccion sintetica creada, persistida, trazable parcialmente y con rechazo duplicado controlado.

No se puede declarar OK funcional E2E desde la SPA Docker hasta corregir el proxy/ruteo de rutas funcionales raiz y reintentar navegacion transaccional autenticada desde `http://localhost:743`.

Estado productivo: **NO-GO**.

## Condiciones Para Cierre Posterior

1. Corregir proxy SPA Docker o contrato de rutas para que pantallas funcionales consuman JSON y no `index.html`.
2. Definir y validar evento inicial obligatorio de estado o documentar formalmente la razon de ausencia.
3. Formalizar contrato de idempotencia: codigo HTTP esperado, clave idempotente y comportamiento de replay.
4. Revalidar catalogos NACHA-M/layouts si aplican al alcance.
5. Ejecutar UAT funcional E2E desde SPA con evidencia visual sanitizada.
6. Mantener productivo en **NO-GO** hasta aprobaciones humanas y validaciones externas.
