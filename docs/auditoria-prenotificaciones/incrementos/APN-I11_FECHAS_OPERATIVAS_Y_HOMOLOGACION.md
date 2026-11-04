# APN-I11 — Fechas operacionales locales, ciclos cancelados y homologación reproducible

**Fecha:** 2026-11-03 · **Base:** `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I10_2026-11-03.zip` · **Proyecto:** ACH Interbank

**Estado:** `IMPLEMENTADO_EN_FUENTES / PRUEBAS_PYTHON_APROBADAS / DOTNET_SQL_UAT_NO_EJECUTADOS`.

## Propósito

Revisar las correcciones APN-I08–I10 en los puntos de vencimiento y recepción, y corregir dos defectos verificables por inspección de código: (1) el conteo de un ciclo operativo cancelado como si existiera oportunidad de devolución, y (2) el uso de `recalculatedDeadline.UtcDateTime.Date.AddDays(1)` como límite para archivos y respuestas, aunque la fecha operacional de Colombia usa `America/Bogota`. Se amplía además la barrera para respuestas negativas CENIT sin correlación resuelta que llegan en **otra sesión de devolución**.

## Cambios productivos

1. `ClearingHouseValidationPolicyService.cs`:
   - La sesión de origen cancelada no genera plazo de aceptación tácita.
   - Los sucesores con `AchCycleOperationalStatus.Cancelled` no cuentan dentro de los cuatro ciclos posteriores de ACH Colombia.
   - Se conserva el requisito anterior de calendario completo, hora real de liberación y cinco sesiones activas CENIT.
2. `PrenotificationReceiptWindow.cs` (**nuevo, clase pura**):
   - Expresa explícitamente fechas operativas locales `[inicio, fin)` y ventanas de llegada UTC `[inicio, fin)`.
   - Convierte la **medianoche local posterior al día de vencimiento** a UTC, evitando comparar fechas locales con un corte UTC arbitrario.
   - Utiliza la zona configurada y falla con excepción si falta o si el plazo precede a la fecha de origen; el job retiene al candidato ante error.
3. `AchTacitAcceptanceJobHandler.cs`:
   - Filtra fechas operativas de ingestiones NACHA y transferencias MFT con límites locales.
   - Filtra `UploadedAtUtc` y `CenitChamberResponse.ReceivedAtUtc` con límites UTC convertidos a partir de la fecha operacional.
   - No excluye un NACK/rechazo CENIT ambiguo por el hecho de que su `AchCycleId` sea diferente del ciclo **de origen**: la devolución puede corresponder a una sesión posterior.
   - Actualiza la referencia de evidencia del evento a `APN-I11`.

**No se crean estados, tablas, migraciones ni señal falsa de fin de archivos recibidos.**

## Pruebas creadas y repetibles

- `PrenotificationReceiptWindowI11Tests.cs`: **5 pruebas C# propuestas** para ACH ciclo 1/5, salida final CENIT, corte local a medianoche, cruce de días y fail-closed de la zona.
- `ThirdPartyAccountValidationLifecycleTests.cs`: **3 pruebas C# nuevas**: sucesor ACH cancelado, origen cancelado y CENIT con una sesión cancelada.
- `test_apn_i11_windows.py`: **12 pruebas Python** (9 contratos estáticos de código y 3 ejemplos independientes de aritmética de fechas). **No ejecutan C#.**
- `run_apn_i11_dotnet.sh`: ejecutor con SDK .NET para la suite APN y regresiones; devuelve error 127 si falta el SDK.
- Regresiones Python APN-I08 (6), I09 (8), I10 (10) y calendarios de referencia (6).

## Contrato pendiente de cierre de recepción

Se incorpora `APN_I11_CONTRATO_CIERRE_RECEPCION.md` como especificación **propuesta**, con campos de origen, sesión, participante, manifiesto, integridad, emisor, fecha y verificación. No hay productor externo oficial validado ni se declara implementado un cierre positivo. Este asunto continúa en `APN-R07`.

## Qué está y no está verificado

- **Sí:** comparación de código contra I10, verificaciones estáticas y ejemplos de fechas, integridad de los paquetes ZIP.
- **No:** compilación .NET 10, ejecución xUnit, base de datos, transacciones concurrentes, Quartz, entrega real de MFT CENIT ni homologación UAT.
- La comprobación del código no prueba semántica real del contrato externo. No se promueve a producción por el mero hecho de generar el ZIP.

## Reproducción en ambiente apto

```bash
cd ACHInterbank_ajustado
python scripts/audit/prenotificaciones/test_apn_i11_windows.py
bash scripts/audit/prenotificaciones/run_apn_i11_dotnet.sh
```

`global.json` exige .NET SDK `10.0.403` con rollforward de patch. Es necesario restaurar dependencias. Repetir sobre SQL Server/PostgreSQL y archivos reales/sintéticos autorizados antes de marcar homologado.

## Riesgos residuales

- `APN-R07`: la fuente de prueba positiva de recepción final no existe en las interfaces verificadas.
- `APN-R05`: pruebas .NET/BD/UAT no ejecutadas.
- `APN-R06`: latencia del job Quartz de 30 minutos no certificada.
- `APN-R08`: las barreras conservadoras pueden requerir resolución manual de ambigüedades.
- **APN-R09/R10:** cambios de I11 implementados en fuentes y pendientes de homologación.
