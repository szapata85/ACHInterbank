# ACH-CR-001 — Plan de homologación, seguridad y GO/NO-GO

**Estado:** plan de ejecución FUTURO; no representa pruebas realizadas. Base de prueba: ZIP completo I06. **Restricción:** sin movimientos monetarios ni conexión a cámara real fuera de homologación formalmente autorizada. Trabajar inicialmente con datos ficticios.

## 1. Preparación y prohibiciones

- Preparar ambiente aislado con SDK .NET compatible, SQL Server/PostgreSQL (según el proyecto), configuración efectiva de cámara, datos sintéticos, calendario de feriados de Bogotá y un endpoint MFT de prueba sin acceso productivo.
- Iniciar con tarea Quartz `AchPendingCycleReassignment` **Disabled** y `AchCycleReassignment:EnableExecution=false` (o ausente). Confirmar que tareas ya persistidas en BD no heredan accidentalmente `Enabled` de una instalación antigua; no bastan los defaults del seeder.
- Revisar logs sin cuentas, credenciales ni PII. No habilitar actividad monetaria real, ni reintento automático de transferencias con ACK desconocido.
- Registrar versión, SHA-256 del paquete instalado, commit si se dispone, motor/versión de BD, fecha, responsable, datos sintéticos y resultados por escenario.

## 2. Comandos de compilación y pruebas que deben ejecutarse fuera de este entorno

```cmd
python scripts\validate-solution-projects.py
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore
dotnet test tests\Cfa.ACHInterbank.Tests\Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Si una compilación falla, **NO-GO** y guardar log; no alterar expectativas de tests para esconder el defecto. Las herramientas Python no verifican semántica C#/EF. Ejecutar las pruebas de I01–I05 y luego batería general.

## 3. Matriz de pruebas adversariales (no ejecutadas)

| ID | Prueba | Evidencia esperada/puerta |
|---|---|---|
| H01 | Crear transacción sábado, lunes hábil C1; festivo lunes→martes | Asignación futura válida, una sola transacción, fecha valor coherente |
| H02 | C1 vencido y C2 vigente; repetir comando dos veces | Único cambio ciclo/lote, único evento auditable; segunda invocación sin mutación |
| H03 | Próximo ciclo restringido por tipo y último ciclo del día | Selección del próximo admitido o rechazo conservador; nunca alterar fecha valor |
| H04 | Archivo NACHA A leyó antes; I02 gana primero; luego audita A | Admisión de A rechazada sin archivo entregado ni segunda presentación |
| H05 | NACHA audita primero; I02 se ejecuta después | Movimiento rechazado, referencia original inalterada |
| H06 | Repetir H04/H05 con **dos procesos** y SQL Server/PostgreSQL bajo carga | Ausencia de doble membresía, cambios parciales o resultados ambiguos tratados como éxito; captura errores de serialización/deadlock |
| H07 | Interrumpir tras primer `SaveChanges` de I02 antes de commit | Rollback de ciclo/lote/evento/totales; reintento idempotente |
| H08 | Fallo al publicar segunda partición NACHA | Evidencias parciales detectables; no publicación/reenvío ciego; conciliación manual |
| H09 | CENIT cola/ejecución/optimización con ciclo vencido | Nuevo handler se abstiene; motores originales no alterados |
| H10 | 250 pendientes inelegibles delante de 1 elegible; correr scheduler repetidamente | Cuantificar starvation y crear decisión basada en volumen y SLA |
| H11 | Scheduler Disabled / flag false / Paused / tarea faltante | Cero escrituras por I03 y observabilidad del motivo de skip |
| H12 | Activación controlada opt-in en entorno aislado, batch y escaneo limitados | Solo transacciones no presentadas, resultados agregados correctos; rollback seguro |
| H13 | Crash/restart de Quartz/BD durante traslado, nuevo intento | Idempotencia, sin doble archivo ni fecha valor cambiada |
| H14 | Respuesta externa ausente o estado de envío ambiguo | Bloqueo de traslado y reenvío hasta conciliación formal |

Los `H04–H09`, `H12–H14` requieren ambiente relacional + observabilidad y no pueden considerarse PASS por revisión de código.

## 4. Secuencia de habilitación controlada

1. **Fase A — NO-GO inicial:** conservar tarea Disabled y flag false. Validar startup, estructura de BD, migraciones existentes, permisos, reloj/zonas y esquema efectivo; ejecutar H01–H11.
2. **Fase B — UAT aislada:** usar datos sintéticos y cámara/MFT simulado, confirmación de no afectación financiera; habilitar temporalmente solo para H12 en ambiente no productivo y con supervisión, límites bajos, sin autopublicación financiera.
3. **Fase C — Evaluación operativa:** revisar fallos, latencia, casos retenidos, backlog por `MaxScanned`, jornada sin destino, conciliación y controles de día valor. Si falta una regla de negocio o external ACK, no forzar un resultado.
4. **Fase D — Solicitud de autorización:** equipo técnico + operaciones + seguridad + responsable de cámara aprueban evidencias; solo entonces una decisión independiente de go-live puede considerarse. I06 **no** otorga esa aprobación.

## 5. Checklist GO/NO-GO

- [ ] Build Release de `ACHInterbank.sln` exitoso sobre hash/version exactos.
- [ ] xUnit focales I01–I05 y suite regresiva PASS en ambiente apropiado.
- [ ] H01–H14 ejecutados y evidenciados; sin dobles envíos ni corrupción de lotes.
- [ ] Conciliación de archivo/particiones y errores `Serializable` revisada.
- [ ] Política de fecha valor y último ciclo aprobada expresamente, o aceptado mantener pendiente sin rollover.
- [ ] Riesgos por rechazo conservador de ciclo completo y starvation medidos, con runbook.
- [ ] Calendario/regulación ACH Colombia V35 y CENIT vigente confirmados; 7C no activado inadvertidamente.
- [ ] Verificado flag/tarea real en base de datos de despliegue (no solo seed).
- [ ] Monitoreo, permisos, logs y alertas acordados; manual de conciliación disponible.
- [ ] Aprobación de negocio, operaciones y seguridad registrada con responsable y fecha.

**Regla:** cualquier casilla imprescindible sin evidencia implica **NO-GO**. No convertir el cierre I06 en certificación externa.

## 6. Contención y recuperación de un ensayo fallido

- Poner task `Disabled` **y** flag `AchCycleReassignment:EnableExecution=false`; verificar que no haya ejecución en curso.
- No cancelar/reenviar automáticamente archivos con resultado externo desconocido; detener despacho desde el entorno aislado y conciliar archivos, membresías, estados y trazas contra registros auditados.
- Revisar eventos `CYCLE_REASSIGNED`, `AchFileExportTransaction`, lotes afectados, `CenitCycleQueue` y resultados Quartz. Conservar evidencias; nunca borrar trazabilidad para que pase un test.
- Para retroceso de software, usar paquete/version anterior **solo con evaluación de compatibilidad de estado**; revertir binarios no deshace operaciones ya registradas. No aplicar `Down()` ni manipular tablas en caliente.
- Reportar defecto como nueva intervención focal, anotar reproducción segura y actualizar `MEMORY_STATE.md` y el ZIP siguiente únicamente después de cambios probados.

**Resultado de este documento al cerrar I06:** `PLAN_READY`, `TESTS_NOT_EXECUTED`, `GO_LIVE_NOT_AUTHORIZED`.
