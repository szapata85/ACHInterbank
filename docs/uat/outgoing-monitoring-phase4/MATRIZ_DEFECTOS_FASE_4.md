# Matriz de defectos — Fase 4

| ID | Severidad | Causa raíz | Corrección | Regresión | Estado |
|---|---|---|---|---|---|
| F4-01 | Media | Ciclo futuro se mostraba como transacción recién creada | Estado funcional `Scheduled`, fecha y siguiente paso | Política + multi-motor + SPA | Corregido |
| F4-02 | Media | Integración exitosa sin respuesta se mostraba como éxito final de integración | Resultado `PendingResponse` basado en ausencia persistida de respuesta | Política + multi-motor + SPA | Corregido |
| F4-03 | Media | El contrato no exponía filtro por código de respuesta requerido por UAT | Filtro normalizado, validado y traducible en ambos motores | Multi-motor + Angular + Playwright | Corregido |
| F4-04 | Baja | Una base inicializada por una ruta histórica podía conservar `Dashboard` en la navegación | Seed idempotente actualiza la ruta estable `/dashboard` a `Panel principal` | Seeder + API de navegación + Playwright SQL Server/PostgreSQL | Corregido |

No se modificó lógica monetaria, transmisión, clasificación histórica ni asociación de archivos.
