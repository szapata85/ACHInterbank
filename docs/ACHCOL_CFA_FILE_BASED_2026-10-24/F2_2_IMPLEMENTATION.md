# F2.2 — Admisión ACH Colombia sin `AchCycleId` interno

**Programa:** `ACHCOL-NACHA-CFA-EXTCYCLE-001`  
**Fecha:** 2026-10-24  
**Fuente exacta:** `ACHInterbank_ACHCOL_CFA_F2_1_UNIFICADO.zip`  
**Estado:** `IMPLEMENTED_UNVERIFIED` (código y pruebas definidos; .NET 10 no disponible para ejecutarlos).  
**Siguiente:** `F2.3` — persistencia de contexto y trazabilidad de parsing sin ciclo interno.

## Objetivo

Permitir la **admisión** de archivos ACH Colombia válidos, previamente reconocidos y descifrados si procede, cuando `IncomingNachaCycleResolver` devuelve `ResueltoSinCiclo=7`, `ResolutionMode=AchColFileBased` y `AchCycleId=null`. La presencia de `H/008/.8`, `I/009/.9` o `H/008/.10` no debe exigir una fila `AchCycle`; el consecutivo `ZZZ` no es el ciclo externo `N`.

La admisión es distinta de la autorización de **despacho monetario**: el parser, la persistencia, la elegibilidad, la cola, los mapeos y la invocación SOAP CFA continúan protegidos por sus propios controles. No habilitar Live ni activar el flag en producción durante F2.2.

## Cambios de código

| Archivo | Cambio |
|---|---|
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaAdmissionValidator.cs` | Introduce vía de admisión específica ACHCOL `ResueltoSinCiclo` con configuración opt-in. La decisión verifica `IsResolved`, no ambiguo, estado exacto, modo exacto y `AchCycleId` ausente. La cámara debe existir, ser `ACHCOL` y estar activa. Revalida nombre físico `.OUT` canónico y correlación `ZZZ` ↔ `FileIdModifier`/fecha del tipo 1. Conserva controles de festivos, fecha operativa y hora de creación. Usa formato de fecha efectiva ACHCOL de 8 dígitos (`yyyyMMdd`), comprueba inconsistencia o invalidez del lote presente. Devuelve `ExternalCycleNumber` como metadata `CycleNumber` del preview de esta ruta, sin generar `AchCycleId`. No consulta `AchCycles` ni ventana asociada al ciclo **solo en esa rama**. Conserva la ruta preexistente CycleBound. |
| `tests/Cfa.ACHInterbank.Tests/IncomingNachaAdmissionAchColFileBasedTests.cs` | Pruebas de unidad sobre SQLite in-memory para `.8`, `.9`, `.10` sin ciclos; flag OFF, estado equivocado/ambiguo, extensión no canónica, identificación y fecha del header, hora inválida, fecha efectiva de lote inconsistente o inválida, festivo, cámara inactiva/CENIT y petición malformada. No incluyen endpoint real CFA. |

## Decisiones F2.2

- **DEC-022 — Bypass de dependencia, no de controles.** Solo ACHCOL en contexto `ResueltoSinCiclo + AchColFileBased + AchCycleId=null`, con bandera explícita `AchCol:AllowCycleIndependentInboundDispatch=true`, puede prescindir de `AchCycle` en **admisión**. El resto de cámaras y estados retienen las reglas existentes.
- **DEC-023 — Defensa en profundidad en admisión.** El validador verifica nuevamente cámara activa y correlación física `.OUT` ↔ encabezado; no confía ciegamente en campos de un DTO o en el resultado de F1.2. `.OUT.env` todavía cifrado **no** es admitido directamente: debe convertirse primero en `.OUT` canónico mediante el descifrado de F1.2.
- **DEC-024 — Fechas ACHCOL.** Para la ruta basada en archivo, el campo `EFFECTIVEENTRYDATE` de lote se interpreta como ocho dígitos `yyyyMMdd` conforme al layout ACHCOL; una fecha efectiva presente pero inválida o discordante se rechaza. La lectura legacy `yyMMdd` conserva su comportamiento anterior.
- **DEC-025 — Trazabilidad e independencia.** `CycleNumber` del resultado de admisión ACHCOL refleja `N` externo (8, 9, 10...), **nunca** `ZZZ` ni FK a `AchCycle`. La persistencia tipada de todos estos valores y el linking transaccional corresponden a F2.3.

## Controles y compatibilidad

1. Restricciones de día hábil (fin de semana, `BankHolidays`, `ClearingHouseSpecialDates`) permanecen **antes** de admitir cualquiera de las rutas.
2. Se rechazan estados incompletos, falsos, ambiguos, flag apagado y cámara distinta/inactiva.
3. La ruta con `AchCycle` conserva comprobaciones existentes de presencia, estado `Closed/Cancelled`, tolerancia de recepción, fecha y reproceso explícito.
4. No se modifican CENIT, RET, modelos EF/SQL, esquema, descifrado, MFT/SFTP, parser de registros, cola, mapper o cliente SOAP.
5. No se crean ciclos 8/9 ficticios; no se asigna una referencia a un `AchCycle` arbitrario.
6. El propio proceso de parsing y cualquier bloqueo posterior siguen pendientes: F2.3–F5 los atenderán; F2.2 por sí solo **no** garantiza despacho CFA.

## Pruebas y evidencia

- **Realizado:** inspección estática de diferencias C#, recorrido lógico de condiciones y guardas, control de archivos previos y prueba de integridad del ZIP.
- **Definido y añadido pero NO ejecutado:** suite xUnit `IncomingNachaAdmissionAchColFileBasedTests` y regresión existente `IncomingNachaTraceabilityCoreTests`.
- **NO realizado:** `dotnet build`, `dotnet test`, ejecución EF contra proveedores productivos, E2E MFT/SFTP, llamadas SOAP CFA, UAT o homologación. **SDK .NET 10 no disponible** en el entorno actual. Las pruebas finales se realizarán en F6 después de F1–F5; no se suspende el siguiente incremento.

## Riesgos, rollback y continuidad

- **Activación:** conservar `AchCol:AllowCycleIndependentInboundDispatch` deshabilitado por defecto; habilitarlo para certificación/Live únicamente cuando estén completas persistencia, seguridad, idempotencia, mapeos, autorización y controles de CFA.
- **Rollback:** apagar el flag revierte la admisión sin ciclo; las ingestas previamente creadas se mantienen para auditoría, y los resultados inciertos no se recirculan automáticamente.
- **Siguiente F2.3:** preservar de extremo a extremo modo `AchColFileBased`, nombre, `ZZZ`, fecha, `N` y `AchCycleId=null` en persistencia; no reutilizar `DetectedCycleNumber` como referencia al ciclo interno; identificar los consumidores que aún requieran una FK a `AchCycle`.
