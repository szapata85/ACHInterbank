# Protocolo operativo para implementar incrementalmente en ChatGPT (modo chat)

## Regla principal: avance sin bloquearse por ambiente

**No interrumpir los cambios de código posibles** porque Docker, Postgres/SQL Server, Node, .NET SDK, navegador o un servicio externo estén ausentes o bloqueados. Adaptar la verificación: inspección estática, tests aislados ejecutables, validaciones de estructura, fixtures sintéticos y pruebas documentadas. Mantener trazabilidad estricta de lo que SÍ y NO se pudo ejecutar. **Nunca afirmar PASS de una prueba no ejecutada** ni promover código financiero a UAT/producción sin evidencia requerida.

## Secuencia por solicitud de incremento (I01–I07)

1. **Base de datos de archivos**: usar el último ZIP completo entregado, verificar que abre y que contiene el proyecto raíz `ACHInterbank_ajustado/` y este directorio documental. Extraerlo en espacio temporal. No sustituirlo por una versión anterior, ni usar un ZIP de parche.
2. **Contexto**: leer `MEMORY_STATE.md`, `ESTADO_INCREMENTAL.md`, `PLAN_IMPLEMENTACION_2026-11-02.md`, `AUDITORIA_BASE_SPA_2026-11-02.md`, el `AGENTS.md` existente y reglas del proyecto.
3. **Ejecutar un incremento**: realizar cambios reales, focalizados y verificables en código/documentación. Mantener arquitectura, servicios existentes, separación ACHCOL/CENIT, seguridad, idempotencia y consistencia. No hacer otra auditoría integral.
4. **Verificación escalonada**: intentar comandos relevantes si los ejecutables están disponibles. Si algo falla por ausencia de ambiente, registrar el comando, error, motivo y prueba pendiente; seguir con verificaciones permitidas. No inventar datos de resultados.
5. **Estado**: actualizar `MEMORY_STATE.md`, `ESTADO_INCREMENTAL.md`, `ESTADO_INCREMENTAL.json`, `CHANGELOG_INCREMENTAL.md`, `MATRIZ_ACEPTACION.md` y el informe de incremento `INFORME_Ixx.md`. Un incremento no se declara terminado si faltan controles funcionales críticos; puede entregarse `EN_PROGRESO` con pendientes.
6. **Empaquetado**: generar `ACHInterbank_COMPLETO_SIMULADOR_PRENOTIFICACIONES_Ixx_YYYY-MM-DD.zip` con TODO el proyecto y documentación, sin excluir módulos preexistentes ni generar solo archivos modificados. Verificar `testzip`, conteo, estructura y SHA-256.
7. **Respuesta final**: incluir enlace al ZIP completo, ID, estado, cambios, pruebas ejecutadas, pruebas pendientes/bloqueadas por ambiente, riesgos, lo que falta para terminar y siguiente incremento.

## Política normativa

- **Devolución de prenotificación:** usar NACHA-M real de la cámara, causal homologada, importe 0, trace de origen y adenda que corresponda, con descarga y NachaUpload manual.
- **No devolución:** solo ruta de prueba de aceptación tácita mediante plazo/job existentes; **no inventar archivo NACHA de aprobación positiva**.
- **CENIT** usa calendario/ciclos y cierre de día distintos de ACH Colombia; validar cada perfil y no mezclar códigos o rutas.
- UAT local, datos sintéticos/controlados, sin SOAP externo, saldos reales ni auto-importación.

## Alcance de la evidencia

- `PASS`: comando/versión y salida reproducibles, alcance explícito.
- `FALLA`: error ejecutado; adjuntar fallo y corrección o pendiente.
- `NO_EJECUTADA_POR_AMBIENTE`: herramienta/servicio faltante, comando esperado, alternativa estática y condiciones para revalidación posterior.
- `NO_APLICA`: justificar por qué un test no es pertinente.

## Prompt compacto para siguiente chat

> Abre el ZIP completo adjunto de ACH Interbank, lee `MEMORY_STATE.md` y `docs/implementacion-simulador-prenotificaciones/ESTADO_INCREMENTAL.md`. Implementa **I01** del plan de respuestas a prenotificaciones con cambios reales y pruebas. No detengas los cambios posibles por ambiente bloqueado: registra las pruebas no ejecutadas y sigue con las verificaciones viables. Entrega ZIP **completo**, informe I01, memory state y estado actualizado para todos los incrementos. No marques pruebas no ejecutadas como PASS, no transmitas al exterior y no inventes formatos NACHA-M.

Sustituir I01 por el siguiente incremento según el tablero real; no saltar las dependencias obligatorias.
