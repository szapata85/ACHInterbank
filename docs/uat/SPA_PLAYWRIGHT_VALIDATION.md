# Validación Playwright de la SPA

Fecha de corte: 2026-07-18  
SPA objetivo: `http://localhost:743`  
API objetivo: `http://localhost:843`  
Proyecto Playwright: `chromium`

## Resultado ejecutivo

La línea base del crawler registró **68 aprobadas y 11 fallidas de 79**. Los fallos iniciales correspondían a 401 visibles en consola, cuerpos vacíos en rutas raíz y URLs legacy que nginx resolvía sin llegar a la pantalla 404. Después de la remediación, el crawler final registró **79/79 aprobadas** y la batería crítica registró **3 aprobadas, 1 omitida por opt-in Live y 0 fallidas**. Las capturas finales de los dos modos del simulador fueron inspeccionadas y no contienen secretos, cuentas reales ni payloads SOAP.

## Ambiente y mecanismo

- Se reutilizó la configuración del repositorio en `web/ach-interbank-ui/playwright.config.ts`.
- El runtime del navegador integrado de Codex no estaba disponible en esta sesión; se usó el Playwright real del repositorio con Chromium.
- Las pruebas del simulador usan un harness HTTP controlado para verificar contrato, permisos visuales y ausencia de efectos laterales. No representan carga bancaria ni respuesta real.
- La configuración conserva `trace: retain-on-failure`, `screenshot: only-on-failure` y `video: retain-on-failure`; los dos casos críticos agregan además una captura explícita en ejecución exitosa.

## Línea base

Comando:

```powershell
cd web/ach-interbank-ui
npx playwright test e2e/spa-routes-smoke.spec.ts --project=chromium --workers=1
```

Resultado inicial:

```text
79 pruebas
68 aprobadas
11 fallidas
```

Defectos observados:

- errores 401 registrados por pantallas de configuración/transacciones;
- cuerpo vacío en raíces de ciclos, navegación y transacciones;
- rutas NACHA-M legacy servidas por el fallback de nginx en lugar de mostrar `/not-found`.

Estos defectos motivaron guards por acción, redirects explícitos, eliminación de la reinyección hardcodeada de menú y aislamiento de las rutas HTTP bajo `/api`. El rerun completo confirmó su cierre: 79/79 rutas aprobadas.

## Specs dirigidos

| Spec | Escenario comprobado | Resultado disponible |
|---|---|---|
| `e2e/uat-functional-controlled.spec.ts` | Modo `Transacciones entrantes`, contrato explícito, nomenclatura ACHCOL `.OUT`, resumen, carga manual y ausencia de efectos SOAP/upload | Aprobado en el último reporte Playwright. |
| `e2e/nacha-simulator-differential-responses.spec.ts` | Cambio de modo, limpieza de estado, filtros/paginación backend, elegibilidad, selección y bloqueo 409 sin perfil homologado | Aprobado en el último reporte Playwright. |
| `e2e/nacha-differential-responses-live.spec.ts` | Preflight local, paquete explícito y health checks | **Omitido por diseño**: opt-in deshabilitado y ruta de paquete no configurada. |

Se reutilizó `uat-functional-controlled.spec.ts` para el modo entrante en vez de crear un spec duplicado con otro nombre.

El archivo `test-results/.last-run.json` observado al corte contiene:

```json
{
  "status": "passed",
  "failedTests": []
}
```

Ese registro corresponde a la batería crítica final; el crawler global se ejecutó y reportó por separado.

## Evidencia gráfica

| Pantalla | Evidencia | Contenido sensible |
|---|---|---|
| Transacciones entrantes | `web/ach-interbank-ui/test-results/uat-functional-controlled--414e0-ito-y-sin-efectos-laterales-chromium/simulador-transacciones-entrantes.png` | Harness controlado; no contiene cuentas reales ni secretos. |
| Respuestas diferenciales bloqueadas | `web/ach-interbank-ui/test-results/nacha-simulator-differenti-b1ddd-acion-sin-perfil-homologado-chromium/simulador-respuestas-diferenciales-bloqueado.png` | Harness controlado; no contiene payload SOAP ni información bancaria real. |

El reporte HTML está en `web/ach-interbank-ui/playwright-report/index.html`.

## Cobertura funcional dirigida

### Transacciones entrantes

- modo inicial y descripción correctos;
- insignias `SIMULACIÓN` y `UAT LOCAL`;
- banco originador externo;
- request con `simulationMode=IncomingTransactions`;
- nombre `0001283.001.20260718.1.OUT`, sin extensión `.ach`;
- resultado marcado como pendiente de carga manual;
- ausencia de requests a NachaUpload, `Proc_Transacciones`, `Proc_Contrapartidas` o SOAP en el harness.

### Respuestas diferenciales

- confirmación al cambiar con configuración temporal;
- limpieza de campos exclusivos de entrantes;
- request con `simulationMode=DifferentialResponses`;
- cámara, banco destino, estado y `pageSize` enviados al backend;
- operación elegible habilitada y no elegible deshabilitada;
- navegación a la segunda página consultada en servidor;
- selección por referencia original;
- preview `Blocked` y generación HTTP 409 por perfil no publicado;
- no aparece un resultado de archivo generado;
- no se hacen uploads, dispatch ni SOAP.

### Live opt-in

Estado al corte:

```text
ACH_DIFFERENTIAL_RESPONSES_LIVE_OPT_IN = disabled / no es true
ACH_DIFFERENTIAL_RESPONSES_PACKAGE_PATH = no configurado
Resultado = Live deshabilitado; prueba omitida
SOAP = no ejecutado
Upload = no ejecutado
```

## Consolidación final

| Batería | Comando | Resultado final |
|---|---|---|
| Crawler global | `npx playwright test e2e/spa-routes-smoke.spec.ts --project=chromium --workers=1` | **79 aprobadas, 0 omitidas, 0 fallidas**. |
| Playwright crítico completo | `npx playwright test e2e/uat-functional-controlled.spec.ts e2e/nacha-simulator-differential-responses.spec.ts e2e/nacha-differential-responses-live.spec.ts --project=chromium --workers=1` | **3 aprobadas, 1 omitida por opt-in Live, 0 fallidas**; exit code 0. |
| Angular completa | `npm test -- --watch=false --browsers=ChromeHeadless` | **414/414 aprobadas**, 0 fallidas. |
| Build Angular producción | `npm run build -- --configuration production` | **Aprobado**; bundle inicial 2.31 MB. |

El proceso Node de la batería crítica emitió, después del resumen aprobado, una advertencia de deprecación de `module.register()` y una aserción nativa `UV_HANDLE_CLOSING`; Playwright devolvió exit code 0 y no marcó casos fallidos. Se conserva como riesgo de tooling, no como evidencia funcional aprobatoria adicional.

## Fallos y límites que no puede cerrar Playwright

- El test diferencial comprueba el bloqueo seguro; no valida un archivo diferencial.
- No hubo carga formal por NachaUpload ni correlación real.
- No hubo invocación `RegistrarRespuestaTransaccion` ni verificación runtime de idempotencia/conciliación.
- La revisión manual y el CRUD de homologaciones no existen para ser probados E2E.
- No hay evidencia responsive completa de las tres pantallas NACHA Config Admin.
- El scheduler RAM no puede demostrar exclusión multiinstancia mediante una prueba de navegador.

Por estas razones, los casos dirigidos aprobados no cambian la decisión **NO-GO**.
