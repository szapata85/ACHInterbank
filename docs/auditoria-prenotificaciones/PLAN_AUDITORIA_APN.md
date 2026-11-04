# APN — Plan de auditoría de prenotificaciones ACH Interbank

**Versión del plan:** 1.0 | **Corte:** 2026-11-03 | **Proyecto:** ACH Interbank
**Cámaras:** ACH Colombia (siete ciclos operativos) y CENIT (cinco ciclos).
**Base obligatoria:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I06_CENIT_2026-11-03.zip`.
**Modalidad:** auditoría focalizada y incremental de código, reglas, integración y pruebas; **sin modificación de código productivo durante la fase de auditoría**. Correcciones por hallazgos se presupuestarán después y requerirán evidencia específica.

## 1. Decisiones obligatorias de alcance

- **ACH Colombia opera siete ciclos.** La tabla adicional `Nuevos Ciclos de Proceso y Horarios.pdf` prevalece para operación sobre la tabla histórica de cinco ciclos de la versión V35; la versión V35 sigue siendo fuente de las reglas que no fueron sustituidas. El repositorio registra `ACHCOL-7C-20261014` desde 2026-10-14. La fuente complementaria contiene la errata `cinco (7)` y en C4 `11:01pm`; la decisión interna registrada fija el comienzo de C4 en `11:01 a. m.`. No convertir esa errata en una ventana nocturna.
- **CENIT:** cinco ciclos. Sesión 1 07:30–10:30; sesión 2 11:00–13:00; sesión 3 13:30–15:00; sesión 4 15:30–17:15; sesión 5 17:45–18:45, con archivos de salida hasta las 19:15. No mezclar sus políticas con ACH Colombia.
- Separar **admisión técnica del archivo**, **procesamiento de prenotificación**, **recepción de devolución**, **plazo operativo**, **descarga e ingestión del último archivo** y **habilitación de monetarias**.
- Una prenotificación sin devolución durante la ventana no genera una supuesta respuesta NACHA positiva; la aceptación es **tácita**, por la regla aplicable.
- No asumir que los fallos de entorno autorizan declarar `PASS`. Marcar `NO_EJECUTADO`, conservar la evidencia estática y continuar los incrementos.

## 2. Objetivo y preguntas de auditoría

Determinar si las rutas ACH Colombia/CENIT generan, transmiten, enlazan, resuelven y vencen correctamente las prenotificaciones débito/crédito sin habilitar transacciones monetarias antes de la finalización correcta de la validación. Comprobar consistencia de estados y de la evidencia de cada intento bajo mensajes tardíos, duplicados y ejecuciones concurrentes.

Preguntas obligatorias: ¿cuál es el ciclo real de procesamiento?, ¿cuál es el último ciclo admitido para la devolución?, ¿cuándo queda disponible el último archivo?, ¿ha sido recibido/procesado por el sistema?, ¿puede el job ejecutar antes?, ¿qué ocurre si llega después una devolución?, ¿se mantiene correcto el estado del tercero?, ¿la validación es específica a cámara y naturaleza de operación?

## 3. Fuentes y prioridad

| Fuente local incorporada | Referencia aplicada | Uso |
|---|---|---|
| `fuentes/Nuevos_Ciclos_ACH_Colombia_7.pdf` | Tabla páginas 1–3 | Siete ciclos, cortes, entrega de archivos y ventanas de devoluciones |
| `fuentes/ACH-Colombia-V35.md` | 2.4.8, 2.10.3.1, 2.11 y 6 | Plazo de cuatro ciclos, validaciones e intercambio NACHA-M; cinco ciclos solo histórico |
| `fuentes/CENIT-DSP-152-Anexo-2.md` | 3.1–3.3 y 4.7–4.8 | Cinco sesiones y vencimiento en mismo día operacional |
| `fuentes/CENIT-Anexo-A-Causales-Devolucion.md` | Tabla de causales | Devoluciones aplicables a prenotificaciones |
| `fuentes/Manual_Especificaciones_NACHA-M_CENIT.md` | 2, 4 y 7 | Estructura de registros, respuestas y adendas |
| `src/Cfa.ACHInterbank.Application/ACH/Models/RegulatoryCycleScheduleCatalog.cs` | `AchColombiaSevenCycleConfirmedPolicy` | Política programada; no certifica activación DB |
| `docs/operations/ACHCOL_CICLOS_7_VIGENCIA_2026-10-14.md` | Decisión interna | Vigencia y corrección horaria de C4 |

Toda conclusión basada en código debe registrar ruta y símbolo verificable. Las afirmaciones de vigencia del ambiente, UAT, banco, red, base de datos o job programado necesitan evidencia externa o prueba real.

## 4. Incrementos y entregables

| Incremento | Actividad / alcance principal | Entregable | Criterio de cierre documental |
|---|---|---|---|
| **APN-I01** | Inventario de estados, intentos, contratos y eventos | `incrementos/APN-I01_ESTADOS.md` | Matriz de transiciones y rutas identificadas |
| **APN-I02** | ACH Colombia, siete ciclos, 4 ciclos siguientes, horarios | `incrementos/APN-I02_ACHCOL_7_CICLOS.md` | Matriz C1–C7 y límites de liberación |
| **APN-I03** | CENIT, cinco sesiones y último retorno del día | `incrementos/APN-I03_CENIT_5_CICLOS.md` | Matriz sesiones y ventana de devolución |
| **APN-I04** | Ingesta NACHA, clasificación, causal y correlación | `incrementos/APN-I04_RESPUESTAS.md` | Ruta de ingesta y excepciones trazadas |
| **APN-I05** | Plazos, job y aceptación tácita | `incrementos/APN-I05_ACEPTACION_TACITA.md` | Riesgos de carreras y faltantes de calendario catalogados |
| **APN-I06** | Activación del tercero y elegibilidad monetaria | `incrementos/APN-I06_CONSISTENCIA.md` | Matriz de barreras, concurrencia y reintentos |
| **APN-I07** | Consolidación, verificadores de lectura y status | `incrementos/APN-I07_CIERRE.md` | Hallazgos únicos, evidencia y pendientes explícitos |

**Formato de entregas:** cada `APN-Ixx` tiene su ZIP **completo y acumulativo**: raíz `ACHInterbank_ajustado/` con el código original, los documentos y verificadores añadidos hasta esa fase. El ZIP final y APN-I07 contienen lo mismo. No se envían solamente parches ni se pierde documentación histórica.

## 5. Pruebas y escenarios mínimos

| ID | Caso | Aplica |
|---|---|---|
| P01 | Prenotificación enviada y procesada en C1 | Ambas |
| P02 | Primera devolución permitida | Ambas |
| P03 | Última devolución permitida | Ambas |
| P04 | Sin devolución hasta vencimiento | Ambas |
| P05 | Devolución presente en archivo todavía no descargado/aplicado | Ambas |
| P06 | Devolución posterior a aceptación tácita | Ambas |
| P07 | Ingreso fuera de hora, pasa a ciclo siguiente | Ambas |
| P08 | Prenotificación creada, pero nunca transmitida | Ambas |
| P09 | Rechazo técnico por operador | Ambas |
| P10 | Archivo duplicado/entrega repetida | Ambas |
| P11 | Trace/identidad ambiguos o no encontrados | Ambas |
| P12 | Nuevo intento tras corrección | Ambas |
| P13 | Fin de semana/festivo/cambio de fecha valor | Ambas |
| P14 | Cuatro ciclos posteriores cruzan al siguiente día hábil | ACH Colombia |
| P15 | Origen en C5, C6 y C7 | ACH Colombia |
| P16 | Devolución en sesión quinta | CENIT |
| P17 | Job y devolución simultáneos | Ambas |
| P18 | Reinicio, reintento y idempotencia | Ambas |
| P19 | Débito obligatorio / crédito opcional y sus requisitos | Ambas |
| P20 | Aislamiento por cámara/cuenta/naturaleza | Ambas |
| P21 | No existe calendario suficiente para los cuatro ciclos | ACH Colombia |
| P22 | Último archivo se recibe después del cutoff | Ambas |

En pruebas temporales usar T−1 segundo, T y T+1 segundo; distinguir `ResponseDeadlineAtUtc`, `CutoffTime`, `OutputReleaseTime`, recepción y procesamiento real. Ejecutar pruebas de .NET, SQL, UI y UAT **si el entorno está disponible**, de lo contrario reportar `NO_EJECUTADO` y avanzar mediante inspección y verificadores offline.

## 6. Estado, severidad y evidencia

**Estados de evaluación:** `TRAZADO_ESTATICO`, `RIESGO_ESTATICO`, `NO_DEMOSTRADO`, `NO_EJECUTADO`, `PASS_RUNTIME`, `FAIL_RUNTIME`, `NO_APLICA`.

**Severidad:** P0 pérdida de control sobre elegibilidad/estado; P1 ventana o coherencia errónea con impacto operativo; P2 trazabilidad y pruebas; P3 documentación. Nunca usar `PASS_RUNTIME` a partir de lectura de código o comandos que solo hacen búsqueda textual.

Cada registro deberá contener ID, cámara, regla, escenario, archivos y símbolos, condición de entrada, salida esperada, salida observada, prueba/comando, evidencia, impacto, estado y eventual corrección mínima.

## 7. Protocolo de bloqueo de ambiente

1. Confirmar herramienta disponible (`dotnet`, npm, base de datos, servicios). No inventar resultados.
2. Si falta `dotnet` o un servicio, completar evidencia estática verificable, casos de prueba reproducibles y matriz de riesgos; marcar las pruebas runtime `NO_EJECUTADO`.
3. Nunca alterar el reloj global, conectarse a bancos reales ni sembrar producción para salvar un bloqueo.
4. Mantener el siguiente incremento operativo y el proyecto íntegro; registrar comandos exactos para homologación posterior.

## 8. Cierre y aceptación

La **auditoría estática** concluye con hallazgos consolidados y estado APN-I01–I07. La **certificación de funcionamiento** requiere ejecutar todos los escenarios relevantes con pruebas sobre un ambiente representativo y evidencia de base de datos, archivos y job; ambos hitos son independientes. Las correcciones de código solo comienzan mediante una fase separada, priorizando hallazgos confirmados y preservando las funcionalidades existentes.

## 9. Continuidad y estructura

Leer en este orden: `MEMORY_STATE_APN.md` (raíz) → `docs/auditoria-prenotificaciones/MEMORY_STATE.md` → `STATUS_IMPLEMENTACIONES.md` → `HALLAZGOS.md` → `incrementos/APN-Ixx_*.md`. No reemplazar `MEMORY_STATE.md` preexistente: corresponde a otro frente de trabajo.
