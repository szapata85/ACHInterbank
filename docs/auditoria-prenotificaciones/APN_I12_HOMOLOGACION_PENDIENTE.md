# APN-I12 — Plan ejecutable de homologación y criterios de evidencia

**Estado:** PLAN/PRUEBAS ESCRITAS; no se atribuye homologación a la creación de este documento.

## A. Preparación

- Entorno .NET SDK 10.0.403 según `global.json`, dependencias NuGet restaurables, SQL Server y PostgreSQL de prueba, scheduler Quartz con hora controlada y bancos simulados autorizados.
- Cargar el proyecto completo APN-I12 (no el delta). Crear base de datos desechable mediante migraciones del propio proyecto, sin cambios manuales de schema.
- Evitar exponer credenciales, trazas bancarias, cuentas o datos personales en evidencias públicas.

## B. Ejecuciones técnicas obligatorias

```bash
cd ACHInterbank_ajustado
bash scripts/audit/prenotificaciones/run_apn_i12_dotnet.sh
```

1. Compilar y ejecutar xUnit I08–I12 en .NET 10. Capturar TRX, versión SDK y hash del código fuente.
2. En SQL Server y luego en PostgreSQL: sembrar intentos con deadline nulo, vencido y futuro; comprobar la traducción EF de `UNION`, filtro `DateTimeOffset`, orden/limites y creación y actualización del parámetro de cursor.
3. Inyectar 2.001 candidatos bloqueados y candidatos posteriores; ejecutar el job con `BatchSize=10`, `MaxCandidatesPerRun=100`, verificar que el cursor avance por bloques a través de ejecuciones sucesivas y que el estado permanezca pendiente salvo evidencia positiva y barreras completas.
4. Simular caída antes de persistir cursor; repetir job, comprobar idempotencia y ningún doble evento de aceptación tácita.
5. Introducir una devolución concurrente al último instante del plazo, con aislación transaccional y múltiples instancias de scheduler. Confirmar estados e historial consistentes.
6. Medir duración, consultas por ejecución, bloqueo SQL, cardinalidad de candidatas y latencia de procesamiento en grandes colas. Definir alerta para `examined == scanLimit` y para errores al persistir cursor.

## C. Matriz funcional por cámara

| Cámara | Caso | Esperado |
|---|---|---|
| ACH Colombia (7 ciclos) | ciclo 1, cuatro sucesores válidos y liberación ciclo 5 15:30 | Jamás antes del plazo; no aceptar si hay devolución pendiente |
| ACH Colombia | ciclo sucesor cancelado | No cuenta como oportunidad; se exige cuarto válido posterior |
| ACH Colombia | ciclo final cruza al próximo día hábil | Usa calendario vigente, festivos y fecha operacional local |
| CENIT (5 ciclos) | devolución durante sesión 5, liberación hasta 19:15 | No aceptar antes de salida íntegra y su procesamiento |
| Ambas | vencimiento almacenado nulo y calendario reparado | Revaluar, sin aprobar antes del recálculo |
| Ambas | rechazo de operador, archivo MFT o NACHA ambiguo | Retener aceptación y registrar motivo |
| Ambas | archivo externo todavía no recibido | **No demostrable** sin comprobante externo; APN-R07 abierto |
| Ambas | devolución después de aceptación terminal | Revisión manual; no sobrescribir silenciosamente |
| Ambas | reintentos y carrera concurrente | Idempotencia, sin inconsistencia entre intento/validación/cuenta |

## D. Criterios de aceptación APN-I12

- Evidencia TRX verde en ambas cámaras y resultados reproducibles sobre SQL Server/PostgreSQL, sin errores de traducción LINQ.
- Rotación de cursor y reevaluación de plazo nulo verificadas; no se habilita ninguna aceptación si faltan barreras.
- Confirmada integridad de estados y trazabilidad, incluyendo las respuestas tardías y rechazos.
- Logs observables y umbrales operativos aprobados por los responsables del job.
- Mecanismo real de cierre de recepción de la cámara **se mantiene como requisito independiente**; ni estas pruebas ni `AchCycle.Closed` lo suplen.

**Resultado actual:** ninguno de estos criterios de homologación ambiental se certifica en este turno por ausencia de .NET, bases de datos y canales operativos. No se detiene la preparación del proyecto ni sus pruebas.
