# Revalidación formal — Cockpit Angular Inbound NACHA-M (Prompt 6B)

Fecha de ejecución: **2026-04-24**

## Alcance validado

Se revalidó el feature `incoming-nacha-command-center` únicamente en modo cierre (sin agregar nuevas funcionalidades), confirmando:

1. rutas Angular del módulo inbound;
2. modelos TypeScript del Command Center;
3. servicio HTTP de ingestas/cola/acciones manuales;
4. pantallas de ingestas;
5. pantalla de cola dispatch;
6. pantalla de detalle de item de cola;
7. consumo de `AllowedActions` desde backend;
8. acciones manuales `retry|unblock|requeue|mark-failed-final`;
9. justificación obligatoria en modal;
10. generación de `idempotencyKey` por acción confirmada;
11. visualización de eventos/auditoría;
12. guards y permisos;
13. no duplicación de state machine en Angular;
14. no criptografía frontend para NACHA;
15. no storage sensible para el feature;
16. build Angular;
17. estado real de tests Angular.

## Evidencia técnica (inspección)

Comandos ejecutados:

```bash
git status --short
git log --oneline -8
find web/ach-interbank-ui/src/app/features/incoming-nacha-command-center -maxdepth 4 -type f | sort
rg -n "incoming-nacha-command-center|IncomingNacha|CommandCenter|AllowedActions|allowedActions|retry|unblock|requeue|mark-failed-final|idempotencyKey|justification" web/ach-interbank-ui/src/app/features/incoming-nacha-command-center -S
rg -n "permissionGuard|roleGuard|CanReadAch|CanManageAch|CanReadIncoming|CanRetryIncoming|CanUnblockIncoming|CanRequeueIncoming|CanMarkIncoming" web/ach-interbank-ui/src -S
rg -n "localStorage|sessionStorage|crypto\.subtle|window\.crypto|privateKey|pfx|SecretRef|secretRef|OpenEnvelopeAsync|RsaKeyProvider|identifier|\bIV\b|\bAES\b|\bRSA\b" web/ach-interbank-ui/src/app/features/incoming-nacha-command-center -S
```

Resultado de inspección:

- El módulo/rutas/pages/models/service del feature están presentes y referenciados.
- `AllowedActions` se consume en grillas y en habilitación de botones en detalle de cola.
- `justification` e `idempotencyKey` están en request de acciones manuales.
- El filtro de seguridad no detectó uso de `localStorage/sessionStorage`, `crypto.subtle` ni clases/artefactos criptográficos en este feature.
- El acceso del feature está protegido por `roleGuard` + `permissionGuard` en `app-routing.module.ts`.

## Validación de build y tests

Comandos ejecutados:

```bash
cd web/ach-interbank-ui
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

Resultados reales:

- `npm ci`: **OK**.
- `npm run build`: **OK**.
- `npm test`: **FAIL en runner**, por limitaciones de entorno y toolchain de Karma:
  - `No binary for ChromeHeadless browser on your platform. Please, set "CHROME_BIN" env variable.`
  - `TypeError: Cannot read properties of undefined (reading 'filter')` en `karma/lib/file-list.js`.
  - `Error: invalid rimraf options` durante cleanup de launcher.

## Conclusión de revalidación

El cockpit Angular inbound NACHA-M quedó **funcionalmente implementado** para operación de cola dispatch y acciones manuales auditadas, con obediencia a `AllowedActions` del backend y sin introducir lógica criptográfica ni storage sensible en el feature.

La única brecha actual de validación automática es la ejecución de pruebas browser en este runner por ausencia de `CHROME_BIN` y errores de runtime de Karma/rimraf fuera del alcance funcional del Prompt 6/6A/6B.
