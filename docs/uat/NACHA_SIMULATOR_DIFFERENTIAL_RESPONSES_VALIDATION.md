# Validación del simulador NACHA-M y respuestas diferenciales

Fecha de corte: 2026-07-18  
Ruta SPA: `/uat/nacha-inbound-simulator`  
Clasificación ejecutada: **simulación UAT/local controlada, no LIVE**  
Resultado diferencial: **bloqueado de forma segura; no homologado**

## Arquitectura inspeccionada

```text
SPA /uat/nacha-inbound-simulator
→ NachaInboundSimulatorService
→ api/uat/nacha-inbound-simulator
→ NachaInboundSimulatorController
→ INachaInboundSimulationService
→ NachaInboundSimulationService
→ perfiles NACHA-M / transacciones persistidas
→ artefacto y manifest por Execution ID (solo cuando la generación es válida)
```

El flujo formal posterior de un archivo recibido sigue siendo NachaUpload y el procesador de ingesta. El simulador no modifica directamente estados financieros ni llama SOAP durante preview/generación.

## Separación de modos

| Modo | Semántica | Estado al corte |
|---|---|---|
| `Transacciones entrantes` | Banco externo origina una operación nueva y CFA recibe | Conservado y explicitado; origen externo, receptor CFA, identificadores/trace nuevos y nomenclatura corregida. |
| `Respuestas diferenciales` | Banco destino responde a una operación CFA existente | UI, contrato, selección paginada y guardas implementados; generación bloqueada hasta disponer de perfil y generador homologados. |

El cambio de modo confirma cuando hay estado incompatible, limpia selección y campos exclusivos, actualiza título, descripción, columnas, validaciones y resumen. Los DTO transportan `simulationMode`; no se reutiliza silenciosamente el contrato de una nueva transacción.

## Ambiente, permisos y feature flags

| Control | Development local | Producción |
|---|---|---|
| `NachaInboundSimulator.Enabled` | `true` | `false` |
| `Mode` | `UAT` | `Disabled` |
| `AllowExternalTransmission` | `false` | `false` |
| `AllowAutoImport` | `false` | `false` |
| `DifferentialResponsesEnabled` | `true` | `false` |
| `RequirePublishedDifferentialProfile` | `true` | `true` |

Permisos backend separados:

- `NachaSimulator.Read`;
- `NachaSimulator.GenerateIncoming`;
- `NachaSimulator.GenerateDifferential`;
- `NachaSimulator.Download`;
- `NachaSimulator.Live`.

El endpoint devuelve 404 fuera de un ambiente UAT-like habilitado. El permiso Live no usa fallback genérico.

## Selección y elegibilidad diferencial

La consulta backend es paginada y filtra en base de datos por:

- cámara;
- banco destino;
- fecha;
- estado;
- tipo;
- trace/referencia de búsqueda;
- página y tamaño de página.

Solo considera operaciones persistidas CFA → entidad externa de la cámara elegida. La validación exige:

- CFA como origen `IsDefaultSource=true`;
- banco respondiente igual al destino original y distinto de CFA;
- cámara coincidente;
- operación no retornada y en estado elegible;
- trace number e identificador externo suficientes;
- trace único dentro de la selección;
- ausencia de respuesta previa incompatible.

Las filas no elegibles se muestran deshabilitadas con motivo. No se correlaciona únicamente por fecha y valor.

## Perfil y generador diferencial

El contexto oficial disponible solo acredita perfiles `ORIGINAL/SALIDA`; los perfiles seed no están homologados para una respuesta `RETORNO/ENTRADA`. No existe un generador diferencial table-driven homologado que represente la respuesta sin convertirla en una nueva operación monetaria.

Por ello el backend detiene el flujo con códigos explícitos:

- `DIFFERENTIAL_PROFILE_NOT_PUBLISHED` cuando no existe perfil publicado/homologado;
- `DIFFERENTIAL_GENERATOR_NOT_HOMOLOGATED` si se intenta avanzar sin un generador apto.

Este bloqueo es el comportamiento seguro. No se sustituyó configuración por posiciones hardcodeadas ni se usó un archivo de salida CFA como si fuera una respuesta bancaria.

## Archivos, hashes y manifest

### Resultado generado en modo diferencial

```text
Archivo diferencial: NO GENERADO
SHA-256 diferencial: NO EXISTE
Manifest diferencial: NO GENERADO
Lotes/detalles diferenciales: NO GENERADOS
```

No hay nombre, hash, manifest, carga o banco respondiente que pueda declararse para una ejecución diferencial.

### Fixtures reales inspeccionados sin modificación

| Archivo | Clasificación dirigida | Tamaño | SHA-256 |
|---|---|---:|---|
| `docs/referencias-reales/ACHInterbank/0001283.001.20260714.23` | `OUTBOUND_CFA`; no es respuesta diferencial | 71.020 bytes | `8E18D416227CAE8321328D1E1D9243C28E05D2DDBE8FB3292C2E49C8A1C1FACA` |
| `docs/referencias-reales/tercero-ACHCOL/0001283.001.20250331.1.OUT` | `INBOUND_TRANSACTION` ACHCOL | 44.520 bytes | `F090B5D4BFAB75FE04CD19313EA1ED467D0205F0FC603DE255CF9688C4753518` |
| `docs/referencias-reales/tercero-CENIT/0001283.002.20250331.1` | `INBOUND_TRANSACTION` CENIT | 2.120 bytes | `3566E425E7786B841482612C6EBC507ECD4E41996A1B8391D1CF5BE7F29468BE` |

No se encontró entre esos tres fixtures un archivo acreditable como `DIFFERENTIAL_RESPONSE`. Los originales no fueron modificados y ninguno se usó para fingir una respuesta bancaria.

La generación entrante fue corregida para producir nombres:

- CENIT: `^\d{7}\.\d{3}\.\d{8}\.\d+$`;
- ACH Colombia: `^\d{7}\.\d{3}\.\d{8}\.\d+\.OUT$`;
- nunca `.ach` ni alias derivados del lote.

Cada ejecución entrante usa un subdirectorio por `ExecutionId`, calcula SHA-256 y conserva metadata/manifest. Esto no constituye evidencia de un manifest diferencial.

## Estado del flujo E2E diferencial

| Etapa exigida | Resultado verificable |
|---|---|
| Seleccionar operaciones CFA | Implementado y probado con harness controlado. |
| Configurar respuesta | UI/DTO implementados. |
| Validar elegibilidad | Implementado; bloquea correctamente sin perfil homologado. |
| Generar NACHA-M diferencial | **No ejecutado; bloqueado.** |
| Validar con motor table-driven | **No ejecutado; no hay artefacto.** |
| Cargar mediante NachaUpload | **No ejecutado.** |
| Clasificar como diferencial | **No ejecutado sobre un archivo generado.** |
| Correlacionar y persistir | **No ejecutado E2E.** |
| Actualizar estado original | **No ejecutado E2E.** |
| `RegistrarRespuestaTransaccion` | **No invocado.** |
| Conciliación | **No validada E2E.** |
| Revisión manual de huérfana | La bandeja existe, pero no existe resolución funcional. |
| Segunda carga/idempotencia | **No ejecutada; no existe primer archivo diferencial.** |

## SOAP

Método funcional esperado para una respuesta diferencial: `RegistrarRespuestaTransaccion`.

Métodos que no deben ejecutarse como consecuencia de una respuesta: `Proc_Contrapartidas` y `Proc_Transacciones`.

Resultado real de esta misión:

```text
ACH_DIFFERENTIAL_RESPONSES_LIVE_OPT_IN = disabled / no es true
ACH_DIFFERENTIAL_RESPONSES_PACKAGE_PATH = no configurado
Invocaciones RegistrarRespuestaTransaccion = 0
Invocaciones Proc_Contrapartidas por diferencial = 0
Invocaciones Proc_Transacciones por diferencial = 0
Uploads diferenciales = 0
```

No se inspeccionó ni imprimió un envelope real. El cliente SOAP fue ajustado para no registrar el cuerpo completo de una respuesta de error. El código de construcción mantiene `METODO` fuera del envelope y no usa `PLValidarUsuarioBV`, pero su verificación runtime diferencial queda pendiente al no existir ejecución.

## Idempotencia, transiciones y conciliación

Existen guardas y pruebas unitarias previas para duplicados de ingesta/respuestas huérfanas. No equivalen al escenario requerido de cargar dos veces el mismo archivo diferencial generado.

Quedan sin evidencia E2E:

- primera carga con una sola persistencia y una sola notificación SOAP;
- segunda carga sin SOAP, estado o conciliación duplicados;
- mismo trace con respuesta distinta y archivo distinto;
- transición válida/inválida con historial;
- respuesta sin correlación y resolución autorizada;
- fallo SOAP temporal/negativo y reproceso;
- conciliación posterior a respuesta.

No se declara idempotencia diferencial aprobada.

## Evidencia Playwright

- Batería crítica final: **3 aprobadas, 1 omitida por opt-in Live y 0 fallidas**.
- `e2e/uat-functional-controlled.spec.ts`: modo entrante y ausencia de efectos laterales en harness.
- `e2e/nacha-simulator-differential-responses.spec.ts`: selección diferencial y bloqueo 409 seguro.
- `e2e/nacha-differential-responses-live.spec.ts`: omitido mientras el opt-in y paquete estén ausentes.
- Captura diferencial: `web/ach-interbank-ui/test-results/nacha-simulator-differenti-b1ddd-acion-sin-perfil-homologado-chromium/simulador-respuestas-diferenciales-bloqueado.png`.

## Criterio de cierre

Para levantar el bloqueo se requiere, en este orden:

1. Definir, publicar y homologar un perfil NACHA-M `RETORNO/ENTRADA` por cámara.
2. Implementar el generador diferencial con el motor table-driven y validarlo contra ese mismo motor.
3. Correlacionar por referencias persistidas sin fabricar identificadores.
4. Implementar/aprobar transición, huérfanas, auditoría e idempotencia.
5. Ejecutar carga formal por NachaUpload en ambiente local controlado.
6. Acreditar una sola invocación `RegistrarRespuestaTransaccion` y cero movimientos monetarios nuevos.
7. Repetir el archivo y acreditar cero SOAP/estado/conciliación duplicados.
8. Cerrar conciliación y revisión manual con evidencia persistida.

Hasta cumplir estos puntos, la clasificación es **NO-GO**.
