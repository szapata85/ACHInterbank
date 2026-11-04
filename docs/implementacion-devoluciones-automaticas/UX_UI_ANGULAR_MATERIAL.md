# Interfaz mínima (incremento I04) — Español / Angular Material

## Decisión

No crear pantalla nueva ni incorporar biblioteca visual. Reusar `web/ach-interbank-ui/src/app/features/transactions/components/ach-returns-management/`, que ya usa `mat-card`, `mat-select`, `mat-form-field`, `mat-chip`, botones y estados de la aplicación.

## Cambios limitados

1. En administración **existente** de parámetros operativos: dos bloques, `ACH Colombia` y `CENIT`, cada uno con selector `Desactivada`, `Requiere aprobación`, `Automática`. Mostrar estado efectivo y si el backend limita capacidad de generación/salida.
2. En la pantalla **Devoluciones**: filtro `Pendientes de aprobación` y acciones `Revisar`, `Aprobar devolución`, `Descartar`, solo para registros elegibles y permisos vigentes. Mantener consulta/manual actual sin interferencia.
3. Mensajes claros: «No se generó la devolución porque la transacción está fuera del plazo»; «La devolución se preparó, pero todavía no se ha enviado»; «La configuración cambió; actualiza los datos».
4. Usar confirmación simple antes de habilitar automático. Mostrar cámara, transacción original, causal, importe y ciclo; no diálogos encadenados ni asistentes de múltiples pantallas.
5. No prometer éxito cuando solo se generó archivo; nunca mostrar «Liquidada» por una respuesta HTTP 200 de generación.

## Calidad mínima

Accesibilidad de teclado, etiquetas/lectores de pantalla, estados carga/error/vacío de componentes compartidos, texto corto, una acción principal por contexto, retroalimentación sin jerga técnica ni anglicismos. Ninguna nueva dependencia front-end sin justificación.

**I00:** propuesta de interfaz únicamente; ningún componente modificado.
