# Cierre interno ACH Colombia — fases 1 a 9

**Estado del paquete:** **IMPLEMENTACIÓN COMPLETADA / VALIDACIÓN EJECUTABLE FINAL PENDIENTE**  
**Alcance:** integración ACH Colombia de programación operativa, MFT/SFTP CFA, administración, seguimiento, recepción automática y harness integral controlado.  
**No significa:** producción lista, homologación con ACH Colombia, UAT aprobada, aceptación de usuario ni salida a producción.

## 1. Qué quedó implementado

- Contrato arquitectónico: ACHInterbank publica y recibe en la **frontera MFT/SFTP de CFA**; GoAnywhere de ACH Colombia interactúa externamente con esa frontera. ACHInterbank no modela un “SFTP propiedad de ACH Colombia”.
- Programación operativa persistente por ciclo, separada de la regulación ACH Colombia.
- Outbound automático gobernado por Quartz desde la programación operativa persistida y reprogramable sin reiniciar la aplicación.
- C1 puede representar `06:30` como hora operacional sin cambiar su regulación.
- C7 puede limitar el inicio automático a `17:50` sin cambiar su cutoff regulatorio `18:00`.
- C2-C6 no reciben horas automáticas inventadas; se configuran únicamente cuando exista decisión operativa.
- Dos modos internos de intercambio: **Carpeta compartida** y **SFTP hacia infraestructura CFA**.
- SFTP con validación de huella del servidor, credencial protegida, timeout y publicación temporal seguida de rename final.
- Recepción automática de archivos finales `OUT` y `RET` dentro de ventanas `OutputReleaseStartTime` / `OutputReleaseEndTime`, con claim exclusivo, idempotencia, recuperación y archivo posterior al resultado persistido.
- Seguimiento de transferencias y eventos con lenguaje operativo. `Transferred` se presenta como **Publicado para envío** y no implica recepción o aceptación por ACH Colombia.
- Pantalla administrativa para tipo de conexión, rutas, credencial, prueba de conexión y horarios por ciclo.
- Harness controlado de FASE 8 preparado con PostgreSQL persistente, dos instancias de ACHInterbank, Quartz cluster, SFTP local CFA y actor GoAnywhere simulado.

## 2. Estado regulatorio que permanece intacto

- `ACHCOL-7C-CANDIDATE`: **INACTIVE / NOT ACTIVATABLE**.
- `EffectiveFrom`: pendiente de autoridad oficial; continúa `null`.
- C4: inicio oficial pendiente; no se resolvió por inferencia.
- ACH Colombia V35 histórica permanece separada.
- CENIT no fue modificado por este frente.

## 3. Cómo operar la administración ACH Colombia

### Configurar la conexión

1. Abrir la administración de ACH Colombia.
2. Elegir **Carpeta compartida** o **SFTP**.
3. Para Carpeta compartida, informar las carpetas autorizadas de envío y recepción.
4. Para SFTP, informar servidor CFA, puerto, usuario, huella del servidor, carpetas y tiempo de espera.
5. Si se debe establecer o cambiar la credencial, ingresarla y guardar. Después de guardarla, la aplicación solo muestra si existe una credencial configurada; no devuelve el secreto al navegador.
6. Usar **Probar conexión** para validar conexión, autenticación y acceso a las carpetas. Esta prueba no envía archivos NACHA-M.

### Configurar horarios automáticos

- La ventana ACH Colombia se muestra como contexto regulatorio de solo lectura.
- Configurar únicamente horas operativas aprobadas.
- C1 soporta `06:30` como hora operacional.
- C7 soporta `17:50` como última hora permitida para iniciar automáticamente.
- No completar C2-C6 por analogía ni copiando `StartTime`/`CutoffTime`; permanecen pendientes hasta confirmación operativa.
- Guardar una modificación operacional solicita la resincronización de Quartz sin reiniciar la aplicación.

### Entender los estados

- **Publicado para envío:** el archivo quedó publicado en la frontera CFA. No confirma pickup por GoAnywhere, recepción por ACH Colombia, S3/Integra ni aceptación externa.
- **Pendiente de confirmación:** el resultado de publicación es incierto y necesita conciliación/reintento controlado.
- **Procesado:** el archivo inbound fue procesado por ACHInterbank.
- **Duplicado:** el contenido ya había sido tratado y no debe producir un segundo efecto funcional.
- **Reintento pendiente:** existe una operación recuperable pendiente.
- **No se pudo procesar:** el procesamiento falló; revisar detalle e historial antes de reintentar.
- **Recibido fuera del horario esperado:** se observó un archivo fuera de la ventana prevista; no se inventa un rechazo regulatorio por esa condición.

### Revisar archivos recibidos

En el seguimiento consultar nombre, fecha operativa, ciclo, tamaño, hash, origen, intentos, fechas, resultado, referencia sanitizada e historial. Ante error o duplicado, usar esa trazabilidad antes de reintentar o escalar. Nunca interpretar la publicación local como aceptación de ACH Colombia.

## 4. Pendiente antes de homologación/producción

- [ ] Ejecutar `dotnet build ACHInterbank.sln -c Release` sobre el paquete exacto.
- [ ] Ejecutar las pruebas backend focales y la regresión aplicable sobre el paquete exacto.
- [ ] Ejecutar build y pruebas frontend completas con dependencias disponibles.
- [ ] Ejecutar FASE 8 con Docker/Compose, dos instancias y PostgreSQL persistente.
- [ ] Verificar en runtime el clúster Quartz real y la no duplicidad con dos nodos.
- [ ] Validar el SFTP CFA en un ambiente autorizado, nunca contra producción durante la certificación interna.
- [ ] Confirmar rutas físicas INPUT/OUTPUT y archivo.
- [ ] Confirmar usuario/credencial de homologación y su procedimiento de rotación.
- [ ] Confirmar huella oficial del servidor CFA.
- [ ] Confirmar firewall/lista blanca e IP de origen de ACH Colombia según el contrato de infraestructura.
- [ ] Confirmar horarios operativos definitivos C2-C6.
- [ ] Confirmar `EffectiveFrom` oficial de la política de siete ciclos.
- [ ] Resolver oficialmente la hora inicial de C4.
- [ ] Ejecutar homologación con ACH Colombia/GoAnywhere en ambiente autorizado.
- [ ] Ejecutar UAT de usuario y registrar aceptación formal.

## 5. Dependencias externas aún abiertas

- Topología productiva final: Carpeta compartida o SFTP CFA.
- Rutas, ACL, usuario, credencial, host key, firewall/lista blanca y ownership operativo.
- Contrato de observabilidad para saber si GoAnywhere recogió un archivo o si ACH Colombia lo aceptó. Sin ese contrato, la recepción externa continúa **no confirmada**.
- Ambiente, fixtures y criterios de homologación proporcionados/aceptados por ACH Colombia.
- `EffectiveFrom`, C4 y horas operativas C2-C6 cuando corresponda.

## 6. Control final ejecutable

En un host con .NET 10 y Docker/Compose disponibles:

```bash
python scripts/validate-solution-projects.py
dotnet --info
dotnet build ACHInterbank.sln -c Release
scripts/ci/achcol-phase8/run-controlled-e2e.sh
```

Para el frontend, con dependencias instalables:

```bash
cd web/ach-interbank-ui
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

El harness de FASE 8 debe generar evidencia sanitizada y la matriz `docs/certification/ach-colombia-phase8-scenario-matrix.csv` debe pasar de `NOT_EXECUTED_HOST_GATE` a resultados respaldados por ejecución real. Solo después puede evaluarse un cambio de estado; no declarar homologación, UAT, release o producción por inferencia.

## 7. Dónde continuar

- Estado consolidado: `docs/CURRENT_PROJECT_STATUS.md`.
- Decisión de frontera MFT/SFTP: `docs/adr/ADR-ACHCOLOMBIA-MFT-SFTP-BOUNDARY.md`.
- Operación Quartz: `docs/scheduler/QUARTZ_PRODUCTION.md`.
- Guía UAT/preproductivo: `docs/operations/RUNBOOK_UAT_Y_PREPRODUCTIVO.md`.
- Harness integral: `docs/certification/ACH_COLOMBIA_PHASE8_CONTROLLED_E2E.md`.
- Auditoría histórica MFT: `docs/ai/MFT_EXTERNAL_READINESS_AUDIT.md` (usar su aviso de vigencia; no como descripción actual del código).
