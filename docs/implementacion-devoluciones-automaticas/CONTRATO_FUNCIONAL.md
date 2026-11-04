# Contrato funcional mínimo de devoluciones automáticas

## Motivo y alcance

Un fallo de aplicación en una **transacción recibida por el participante** puede originar una devolución regulada; el sistema debe registrar el resultado real de aplicación y, si corresponde, iniciar una devolución referenciada. No se produce desde el alta de una transacción original ni desde un rechazo de archivo por sintaxis.

## Modo por cámara

| Modo persistido | Detección | Crear borrador/pendiente | Generar NACHA-M | Enviar a cámara |
|---|---|---|---|---|
| `DESACTIVADO` (predeterminado) | Sí, por operación existente; no nueva automatización | No, salvo manual existente | Solo flujo manual autorizado | Solo flujo manual existente |
| `CON_APROBACION` | Sí | Sí, sin afectar fondos ni original todavía | Tras aprobar y revalidar | Por el proceso de envío vigente y autorizado |
| `AUTOMATICO` | Sí | Según elegibilidad | Sí, tras revalidación backend | Solo si un adaptador/canal HOMOLOGADO y habilitado lo permite; de otro modo queda pendiente de salida y NO-GO |

No añadir un segundo booleano `enabled` que contradiga `mode`: se habilita/deshabilita seleccionando el modo. No se parametrizan causales individualmente hasta existir necesidad verificada.

## Reglas de ejecución

1. Consultar el modo desde persistencia por cámara, con valor seguro DESACTIVADO ante ausencia/configuración inválida.
2. Obtener original, tipo, traza, importe, dirección, fecha valor, ciclo, entidad originadora/receptora y resultado de aplicación verdadero.
3. Excluir errores de formato, rechazos por operador, reversiones, ROR, reclamaciones especiales y solicitudes posteriores del beneficiario que requieren instrucción o autorización humana.
4. Seleccionar causal exclusivamente según política vigente por cámara y tipo, nunca por mapeos genéricos sin fundamento.
5. Verificar plazo, ciclo, calendario, monto, saldo/autorización cuando corresponda, duplicado y estado actual inmediatamente antes de generar.
6. Reutilizar `IAchReturnsService` / `AchReturnsService` y NACHA-M existente; resultado de generación ≠ confirmación de envío ≠ liquidación ≠ abono/reintegro.
7. Idempotencia con clave estable o salvaguarda de BD según evidencia del modelo existente; reintento no crea otra devolución.
8. Registrar motivo, actor (`SISTEMA` o usuario), parámetros efectivamente usados y resultado; no almacenar datos sensibles adicionales.
9. Cambio de modo no reescribe ni envía pendientes antiguos; definir la ejecución sobre nuevos eventos, salvo reproceso explícito autorizado.
10. Concurrencia, cancelaciones, fallos de transmisión o reinicios quedan en estado conocido para reintento seguro o atención manual.

## Exclusiones (no crear nuevas funcionalidades por inferencia)

- No implementar débito/abono en banco, compensación ni envío de dinero mediante ChatGPT.
- No crear un nuevo módulo de archivo NACHA-M, un bus de eventos ni motor de reglas si los actuales sirven.
- No crear devoluciones automáticas R23 por reclamo posterior sin solicitud del beneficiario.
- No habilitar CENIT ReturnOut saltando bloqueos de formato/certificación.
- No alterar `NachaUpload` ni el simulador de prenotificaciones salvo una dependencia demostrada y aprobada.

## Autorización y auditoría

`Leer` exige permiso vigente de consulta; `Cambiar modo`, privilegio de administración equivalente a la seguridad actual. `Aprobar` requiere permiso operativo diferenciado de consulta. Usar políticas ya existentes cuando aplique; no inventar política con nombre no registrado. Registrar cambio anterior, nuevo, usuario y momento. Verificación final siempre en API, nunca solo en interfaz.
