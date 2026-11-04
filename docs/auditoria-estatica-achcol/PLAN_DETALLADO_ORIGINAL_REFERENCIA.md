# Plan de auditoría estática exhaustiva — ACH Colombia

**Proyecto:** ACH Interbank · **Fecha de corte:** 2026-11-01 · **Cámara:** ACH Colombia (`ACHCOL`) · **Alcance:** revisión normativa, arquitectura, código, datos y pruebas existentes; **no equivale a homologación ni certificación externa**.

## 1. Objetivo, criterio de prueba y límites

Objetivo: verificar que **cada regla aplicable** al servicio ACH Colombia y a sus **siete ciclos operativos** esté implementada en la ruta productiva real, sea coherente en todos los componentes y deje evidencia de aplicación, rechazo, reintento o excepción. No se considerará suficiente la presencia de una constante, columna, parámetro, comentario o test aislado.

**Cadena de evidencia obligatoria por regla:** fuente/numeral y versión → interpretación operacional inequívoca → identificador `ACHCOL-*` → configuración vigente por fecha → código y llamada productiva → persistencia/estado → resultado y control de errores → prueba automática positiva/negativa/de borde → evidencia trazable. Separar **regla documental**, **implementación estática**, **prueba ejecutada** y **certificación**.

Reglas de auditoría: no modificar productivo durante la revisión inicial; no inferir vigencias no sustentadas; no reutilizar políticas CENIT; marcar `NO_DEMOSTRADO` ante ausencia de evidencia; marcar `CONFLICTO_DOCUMENTAL` si las fuentes discrepan; clasificar los resultados como `CUMPLE_ESTATICA`, `CUMPLE_PARCIAL`, `NO_CUMPLE`, `NO_DEMOSTRADO`, `NO_APLICA` o `BLOQUEADO_NORMA`. Para `CUMPLE_ESTATICA` se requiere ruta productiva localizada y control completo, aunque las pruebas y la homologación sigan pendientes.

## 2. Baseline de fuentes y conflictos que deben cerrarse

| Fuente | Regla / papel | Observación inicial |
|---|---|---|
| Manual de Servicio ACH, código `DDS-DIS-MAN-004`, versión 35, abril de 2026 | Fuente principal para tipos de operación, envío/recepción, devoluciones, prenotificaciones, compensación, cuadre, seguridad y formatos NACHA-M | Numeral 2.4.1 describe **cinco** ciclos |
| `Nuevos Ciclos de Proceso y Horarios.pdf`, páginas 1–3 | Nueva tabla de **siete** ciclos, ventanas, productos y devoluciones | La prosa escribe `cinco (7)` y el ciclo 4 aparece como `11:01pm`; la tabla es de siete filas y los horarios circundantes implican 11:01 **a. m.**; confirmar fuente de aprobación |
| `docs/operations/ACHCOL_CICLOS_7_VIGENCIA_2026-10-14.md` del ZIP | Decisión interna: política `ACHCOL-7C-20261014`, corte 14-10-2026 | El archivo documenta decisión y código; **no sustituye por sí solo la confirmación oficial de ACH Colombia** |
| `docs/normativa/nacha-m/MATRIZ_REGLAS_ACHCOL.md` del ZIP | Matriz histórica de detalle NACHA-M y correcciones | Inicia en versión **32** (enero de 2025). Revalidar **toda** regla heredada contra versión **35**, en especial flujos, transporte MFT/SFTP, layout, causales y vigencias |
| Código fuente y pruebas del ZIP integrado 01-11-2026 | Implementación a auditar, nunca fuente normativa | Las pruebas existentes son evidencia a evaluar, no sustituyen revisión de la ruta productiva |

**Decisión de trabajo:** usar siete ciclos como **baseline del código actual bajo auditoría**, conservar los cinco históricos para fechas anteriores al 14-10-2026 y mantener abierto un hallazgo documental hasta obtener respaldo de vigencia y del ajuste horario del ciclo 4. No certificar por inferencia.

## 3. Secuencia de la auditoría

| Fase | Trabajo concreto | Entregable | Puerta de aceptación |
|---|---|---|---|
| 0. Congelamiento | Registrar SHA-256 del ZIP/manuales, rama/commit si existe, fecha, versión de esquemas/seeds, configuración y diferencias entre fuentes. | Acta de baseline y contradicciones | Fuente/versión y alcance cerrados o explícitamente bloqueados |
| 1. Inventario normativo | Extraer en IDs las reglas de Manual V35 §§2.4–2.7, §§2.10–2.11, §6, anexos de causales/calidad, más calendario de siete ciclos. Separar reglas por tipo, ciclo y estado. | Catálogo maestro de reglas | Ningún punto prescriptivo sin ID / referencia |
| 2. Dependencias transversales | Trazar registro cámara, política efectiva, calendarios, zona Bogotá, clasificación de transacción, scheduler, trazabilidad, archivo, SFTP/MFT, autorización, retorno y compensación. | Mapa de llamadas end-to-end | Cada regla llega a un punto de decisión productivo |
| 3. Ciclos 1–7 | Auditar por separado admisión, bordes horarios, origen permitido de devoluciones, restricción por producto, cierre, planilla, pago, liberación y cuadre. | Siete fichas, una por ciclo | Cada ciclo con matriz de positivos/negativos/bordes |
| 4. Causales | Revisar cada causal del Anexo 9 ACH, cada devolución por operador del Anexo 3, rechazo total/formal, excepciones y causalidad por clase/dirección. | Matriz causal × tipo × ciclo × flujo | Ninguna causal genérica sin compatibilidad comprobada |
| 5. NACHA-M/MFT | Validación registro/campo, longitud, offsets, adenda, controles, nombre, extensiones `.env/.OUT/.RET`, criptovault, recepción y reintentos. | Matriz campo/capa y contrato de transporte | Round-trip estático trazable y errores fail-closed |
| 6. Estado financiero | Validar signos de posición neta, efectos de devoluciones, exclusión de devoluciones por operador, planillas, pago, confirmación SEBRA y barrera para liberar archivos, cuadre operativo. | Reconciliación por ciclo y modelo de estados | No considerar liquidado solo por enviar archivo |
| 7. Riesgos operativos | Simular mentalmente con código y datos: doble ejecución, duplicados, concurrencia, desfases día hábil, cortes, caída MFT, falta de fondos, reversión y re-procesamiento. | Hallazgos y escenarios de abuso/falla | Sin ruta silenciosa a doble compensación o pérdida |
| 8. Dictamen y backlog | Clasificar y priorizar, vincular cambio propuesto con regla y prueba de regresión, sin modificar código mientras se inventarían hallazgos. | Informe `GO/NO-GO` estático + backlog | Bloqueantes identificados; no confundir estático con homologado |

## 4. Catálogo inicial de reglas transversales (ampliable al extraer anexos completos)

Esta es la **estructura mínima**; la auditoría ampliará un identificador por campo NACHA y **uno por cada causal** oficial. Los IDs sirven de trazabilidad, no son códigos oficiales de ACH Colombia.

| IDs iniciales | Obligatoriedad a rastrear | Fuente primaria | Componentes iniciales a inspeccionar |
|---|---|---|---|
| `ACHCOL-CAL-001..008` | Fecha de vigencia; día hábil, festivo, hora Bogotá, cambio de día, orden de sesiones, huecos y bordes inclusivos/exclusivos | V35 §2.4, tabla siete ciclos | `RegulatoryCycleScheduleCatalog`, `ClearingHouseCycleConfigSeeder`, `RegulatoryCycleSeedRepair`, `OperationalCycleWindowResolver`, scheduler |
| `ACHCOL-CYC-001..007` | Ventanas de envío, cierre, planilla, pago y liberación para ciclo 1..7 | PDF siete ciclos pp. 1–3 | `CycleTransactionPolicy`, `AchCycleScheduler`, ciclo/config actuales, scheduler Quartz |
| `ACHCOL-TYP-001..008` | Crédito, débito, prenota de ambos sentidos, retornos, productos PSE/SSS/DIAN y restricción de débito monetario en ciclo 7 | V35 §§2.4.8, 2.10, 2.11; PDF siete ciclos | `CycleTransactionPolicy`, clasificación `TransactionCode`, entradas/salidas |
| `ACHCOL-RET-001..007` | Ventana de cuatro ciclos origen permitidos por ciclo de devolución 1..7 | PDF siete ciclos pp. 1–3 | `RegulatoryCycleScheduleCatalog.ReturnWindows`, `AchReturnEligibilityPolicyComponent`, `AchReturnsService`, `AchReturnEligibilityService` |
| `ACHCOL-RET-008..014` | Diferenciar devolución crédito/débito/prenota; fuera de plazo crédito vs débito; causal vs tipo; fecha de solicitud; correlación con original; una aplicación | V35 §§2.4.8, 2.10.4, 2.11.5 y Anexo 9 | `AchReturnPolicy`, `RegulatoryCatalogSeeder`, `AchReturnArtifactGenerationComponent`, `AchIncomingReturnIngestionService` |
| `ACHCOL-ROR-001` | Verificar **si existe regla contractual ACH Colombia** para devolución de una devolución antes de habilitarla; no importar CENIT por defecto | **No demostrada con las secciones ACH V35 consultadas** | `AllowsReturnOfReturn`, `ReturnOfReturnOrchestrator`, `AchReturnOfReturn*` |
| `ACHCOL-PRE-001..006` | Prenotificación crédito/débito, aceptación tácita según plazo, autorización, al menos tres días hábiles según flujos, opción de cuatro ciclos del débito, error/rechazo | V35 §§2.10.3 y 2.11.4 | `PrenotificationHandler`, servicios de validación, política y estado cuenta |
| `ACHCOL-REJ-001..006` | Rechazo archivo fatal, devolución por operador, devolución por receptor, error campo, resultado `.OUT`/`.RET`, trazabilidad de causal | V35 §6.1.7–6.1.8, Anexo 3, Anexo 9 | NACHA parser, validadores, return ingest, `ReturnRejectionReportsController` |
| `ACHCOL-NAC-001..0NN` | Todos los tipos 1/5/6/7/8/9; cada campo, longitud, offsets, obligatoriedad, valor permitido, padding, total, hash, secuencia y referencia | V35 capítulo 6 y fichas/anexos aplicables | `NachaFileBuilder`, `NachaFileGenerationCoordinator`, perfiles oficiales, parsers |
| `ACHCOL-FIL-001..010` | Nombre/identificador externo, no colisión, encriptado, firma/sobre, MFT/SFTP, descarga, extensiones, idempotencia e integridad | V35 §§2.4.2–2.4.3, §6.1.10 | `AchColombiaManagedMft*`, `NachaFileNamingRuleService`, export/import |
| `ACHCOL-CMP-001..010` | Planilla por ciclo, signos netos según matriz, SEBRA/CUD, no compensar devoluciones por operador, pago, bloqueo liberación, cuadre por archivo/ciclo | V35 §§2.4.5–2.4.7, §§2.5–2.6 | módulos de compensación, reportes, transportes, conciliación, estados |
| `ACHCOL-OPS-001..010` | Horario de excepción, contingencia, reintentos, auditoría, duplicados, eventos de calidad, segregación de permisos, respuesta del operador | V35 §§2.4.3–2.4.4, 2.6–2.7, cap. 3–4 | Quartz, cron, control de concurrencia, logs, auditoría, API/SPA |

**Descomposición obligatoria:** `ACHCOL-RET-001` debe convertirse en subreglas por `ciclo de envío × ciclo de origen × día operativo × tipo de retorno × causal`. Una tabla global de `MaxCycles = 4` **no demuestra** por sí sola las siete combinaciones.

## 5. Matriz operacional de siete ciclos

`n` = día hábil operativo actual; `n-1` = día hábil operativo anterior, **no** necesariamente el calendario anterior. Horarios en `America/Bogota`. La admisión indicada es la de la tabla de siete ciclos; validar que todo producto excluido sea rechazado y no sea simplemente omitido en la interfaz.

| Ciclo | Envío | Cierre | Planilla | Pago compensación | Liberación | Orígenes habilitados de devolución | Transacciones listadas |
|---|---|---|---|---|---|---|---|
| 1 | 19:01 `n-1`–07:00 `n` | 07:00 | 07:00–07:30 | 07:30–07:45 | 07:45–08:00 | `n-1`: 4,5,6,7 | Crédito, débito, ambas prenotas, PSE/SSS/DIAN |
| 2 | 07:01–09:00 | 09:00 | 09:00–09:30 | 09:30–09:45 | 09:45–10:00 | `n-1`: 5,6,7; `n`: 1 | Crédito, débito, ambas prenotas |
| 3 | 09:01–11:00 | 11:00 | 11:00–11:30 | 11:30–11:45 | 11:45–12:00 | `n-1`: 6,7; `n`: 1,2 | Crédito, débito, ambas prenotas, PSE/SSS/DIAN |
| 4 | 11:01–12:30 | 12:30 | 12:30–13:00 | 13:00–13:15 | 13:15–13:30 | `n-1`: 7; `n`: 1,2,3 | Crédito, débito, ambas prenotas |
| 5 | 12:31–14:30 | 14:30 | 14:30–15:00 | 15:00–15:15 | 15:15–15:30 | `n`: 1,2,3,4 | Crédito, débito, ambas prenotas, PSE/SSS/DIAN |
| 6 | 14:31–16:00 | 16:00 | 16:00–16:30 | 16:30–16:45 | 16:45–17:00 | `n`: 2,3,4,5 | Crédito, débito, ambas prenotas, PSE/SSS/DIAN |
| 7 | 16:01–18:00 | 18:00 | 18:00–18:30 | 18:30–18:45 | 18:45–19:00 | `n`: 3,4,5,6 | Crédito, ambas prenotas, PSE/SSS/DIAN; **sin débito monetario ordinario** |

**Caso especial del ciclo 1:** la ventana de llegada empieza el día calendario previo, pero debe corresponder al ciclo del siguiente **día hábil bancario**. Auditar viernes→lunes, viernes→martes festivo, cierre de mes/año y antes/después de 19:01.

## 6. Ficha obligatoria de revisión para cada ciclo

Para **cada** ciclo `C1` a `C7`, completar todos los controles siguientes y adjuntar referencias exactas:

1. **Regla temporal:** apertura, recepción, cierre; límites antes/igual/después del minuto, zona, fecha de negocio, festivos y horarios excepcionales.
2. **Clasificación:** permitido/prohibido para crédito monetario, débito monetario, prenota crédito, prenota débito, devolución crédito, devolución débito, devolución prenota, PSE, SSS y DIAN; comprobar la ruta de decisión y retorno de error. No habilitar ROR ACH sin fuente.
3. **Origen de devoluciones:** enumerar los cuatro ciclos permitidos de la matriz y generar **al menos un caso de rechazo por cada origen no permitido**, cruzando `n-1` / `n`.
4. **Elegibilidad:** original existe, cámara coincide, participante y dirección coinciden, causal aplica, importes y adenda se conservan, no doble devolución, no salida vencida sin tratamiento correspondiente.
5. **Archivo:** nombre externo, correlación, registro NACHA-M, identificador, checksum/controles, rechazo total/parcial, acuse/resultado y estado.
6. **Compensación:** saldo, signo, planilla contra registros, reglas para retornos/devoluciones por operador, fondos, confirmación de pago y barrera de liberación.
7. **Recepción:** archivo de salida correcto, rechazo, respuesta, reconciliación, actualización de estados, distribución y auditoría por transacción.
8. **Continuidad:** timeouts/reintentos sin duplicados, scheduler concorrente, crash entre compromisos DB/transporte, archivo tardío, operaciones de contingencia y día siguiente.
9. **Pruebas existentes:** inventario, aserción real (sin mock que oculte), rutas no cubiertas, propuesta de pruebas de regresión.
10. **Dictamen:** fuente, método, clase/archivo/línea, evidencia, estado, severidad, dueño y corrección propuesta.

### Secuencia de auditoría por ciclo y riesgos distintivos

| Ciclo | Primera hipótesis a contrastar | Negativos críticos | Salida esperada de auditoría |
|---|---|---|---|
| **1** | Reapertura y corte entre dos días hábiles. Devolución solo desde 4–7 del día operativo anterior. | Original de ciclo 1–3 anterior; original del mismo día inexistente; festivo/fin de semana. | Ficha `ACHCOL-C1` con evidencia de fecha operativa y control de la primera planilla. |
| **2** | Primera ventana de retorno del día para operaciones del ciclo 1; continuidad de 5–7 del anterior. | Retorno de ciclo 4 anterior, origen mismo ciclo 2, fuera de horario. | Ficha `ACHCOL-C2` y primer enlace n↔n-1. |
| **3** | Retornos de 6–7 anteriores y 1–2 actuales; productos PSE/SSS/DIAN listados. | Retorno de ciclo 5 anterior; clasificación incorrecta de productos; doble ingreso. | Ficha `ACHCOL-C3` con clasificación por producto. |
| **4** | Inicio 11:01 a. m. en decisión interna; orígenes 7 anterior y 1–3 actuales. | Parámetro heredado 11:01 p. m.; retorno 6 anterior; producto no listado. | Ficha `ACHCOL-C4` y decisión formal sobre discrepancia horaria. |
| **5** | Paso a devoluciones exclusivamente de `n`; orígenes 1–4. | Retorno de día anterior; retorno del ciclo 5; duplicado previo. | Ficha `ACHCOL-C5` y prueba de expiración interdiaria. |
| **6** | Deslizamiento de orígenes a 2–5; mantener PSE/SSS/DIAN. | Retorno del ciclo 1 actual (quinta oportunidad), retorno del 6. | Ficha `ACHCOL-C6` y rechazo de ventana vencida. |
| **7** | Último ciclo: créditos y prenotas, **sin débito monetario**; devoluciones de 3–6. | Débito monetario aceptado; devolución de ciclo 2; impacto de respuesta al día siguiente. | Ficha `ACHCOL-C7`, cierre operativo y preparación de ciclo 1 siguiente. |

## 7. Protocolo regla a regla: plantilla para fichas de hallazgo

```text
Regla ID:
Cámara / política / vigencia:
Ciclo(s) y clase funcional:
Documento oficial y numeral / página:
Interpretación testable (precondiciones → acción → resultado):
Excepciones/derogaciones/ambigüedades:
Modelo y configuración / base de datos:
Código productivo (archivo:línea / caller/callee):
Ruta de entrada real (API, Quartz, importación, MFT, manual):
Ruta de salida real (archivo, response, compensación, estado):
Validaciones negativas y fail-closed:
Riesgos (concorrencia, fechas, duplicidad, integridad, seguridad):
Pruebas existentes y su límite:
Casos que faltan (positivo / negativo / borde):
Evidencia estática / observaciones:
Resultado: CUMPLE_ESTATICA | CUMPLE_PARCIAL | NO_CUMPLE | NO_DEMOSTRADO | NO_APLICA | BLOQUEADO_NORMA
Severidad: CRÍTICA | ALTA | MEDIA | BAJA
Cambio propuesto / riesgo de regresión:
```

### Ejemplo aplicado: `ACHCOL-RET-001` — devolución en ciclo 1

- **Fuente**: PDF siete ciclos, p. 1; orígenes válidos: 4,5,6,7 del día `n-1`.
- **Configuración localizada**: `RegulatoryCycleScheduleCatalog.AchColombiaSevenCycleReturnWindows` enumera los orígenes correctos para el ciclo 1.
- **Hallazgo preliminar**: en el código fuente revisado `ReturnWindows` aparece en catálogo y pruebas, pero no se encontró consumo desde servicios productivos. El `RegulatoryCyclePolicySnapshot` tiene únicamente `Cycles`, sin esas ventanas. `CycleTransactionPolicy` controla el permiso booleano `AllowsReturn`, no el ciclo/día de la transacción original.
- **Resultado provisional**: `CUMPLE_PARCIAL` como dato normativo; `NO_DEMOSTRADO` como aplicación del filtro al camino real de todas las devoluciones. **Crítico** hasta completar trazabilidad.
- **Prueba negativa mínima**: impedir devolución de `n-1` ciclo 3; permitir `n-1` ciclo 4; validar distinto comportamiento crédito/débito vencidos según V35; impedir doble aplicación.

## 8. Hallazgos iniciales para confirmar/refutar

> No equivalen a una auditoría concluida ni a errores productivos confirmados. Están basados en una primera lectura de los archivos citados.

| Hallazgo | Evidencia localizada | Riesgo | Acción de auditoría |
|---|---|---|---|
| `PRE-ACH-001` — ventanas de devolución no trazadas a productivo | `RegulatoryCycleScheduleCatalog.cs`: `ReturnWindows`; referencias en `RegulatoryCycleScheduleCatalogTests.cs`; el snapshot confirmado solo transporta ciclos. | El sistema podría permitir un retorno en ciclo legalmente incorrecto aun cuando `AllowsReturn=true`. | Encontrar rutas alternativas que efectivamente validen origen/ciclo/día; si no existen, abrir cambio bloqueante. |
| `PRE-ACH-002` — elegibilidad general vs caso R10 | `AchReturnEligibilityPolicyComponent.cs` valida una ventana específica para ACH Colombia R10; no equivale a todas las causales ni a la matriz de siete ciclos. | Cobertura normativa desigual por causal. | Recorrer cada causal Anexo 9, retorno crédito/débito y prenota, y todos los callers. |
| `PRE-ACH-003` — ROR habilitado por defecto | `RegulatoryCycleSchedule` usa `AllowsReturnOfReturn=true` por defecto para ACHCOL; no se ha identificado todavía sustento de ROR ACH en las secciones consultadas del Manual V35. | Aplicar reglas CENIT indebidamente a ACH Colombia. | Solicitar/ubicar fuente contractual específica o bloquear operación hasta definición. |
| `PRE-ACH-004` — discrepancia fuentes/schedule | Manual V35 describe cinco ciclos; PDF adicional siete; documentación interna fija 2026-10-14 y aclara ciclo 4. | Activación retroactiva, horario no autorizado o reglas sin vigencia. | Validar respaldo oficial y cruces fecha/versión/seeder. |
| `PRE-ACH-005` — planilla/pago/liberación como cadena | V35 §§2.4.5–2.4.7 vincula entrega de archivos a pago de compensación. | Liberar/aplicar transacciones sin condición financiera confirmada. | Rastrear recepción real del comprobante y barreras de estado, no solo temporizadores. |
| `PRE-ACH-006` — matriz NACHA antigua | `MATRIZ_REGLAS_ACHCOL.md` inicia basada en V32, mientras la fuente entregada es V35. | Falsos `CUMPLE` tras cambios de formato, transportes o procedimientos. | Revalidar línea a línea contra V35 y conservar historial de decisiones. |

## 9. Criterio de salida

La auditoría estática concluye cuando: (a) el 100 % de las reglas normativas relevantes estén inventariadas; (b) el 100 % de ciclos × subprocesos permitidos/prohibidos tenga decisión y ruta productiva identificada; (c) todas las causales y campos NACHA-M aplicables estén cruzados; (d) no queden hallazgos **críticos** sin mitigación, ni conflictos de vigencia sin resolución; y (e) exista un backlog trazable con pruebas positivas/negativas/de borde.

**Resultado posible:** `APTO PARA INICIAR CERTIFICACIÓN` (auditoría estática cerrada, aún pendiente de pruebas), `APTO CON RESERVAS` (solo si las reservas no afectan operación ni cumplimiento) o `NO APTO` (reglas críticas no demostradas/contradictorias). La aprobación estática **no** equivale a liberación productiva.

**Orden recomendado de ejecución real:** baseline → ventanas/devoluciones y clasificación de ciclos (bloqueante) → NACHA/catálogo de causales → compensación/seguridad e intercambio → dictamen y correcciones → pruebas automatizadas → UAT/homologación ACH Colombia → recién después revisión equivalente CENIT.
