# Rediseno UX/UI - Matriz de Campos SOAP

Fecha: 2026-06-19
Ruta SPA: `/integraciones/mappings`
Productivo: NO-GO

## Objetivo funcional

La pantalla deja de comportarse como administrador tecnico de `MappingSets` y pasa a ser una matriz funcional para entender que parametro SOAP se alimenta desde que tabla y campo interno.

Servicios obligatorios visibles:

- `Proc_Transacciones`: creditos monetarios recibidos desde otra entidad financiera hacia CFA.
- `Proc_Contrapartidas`: debitos monetarios originados por CFA hacia otra entidad financiera.
- `RegistrarRespuestaTransaccion`: respuestas, rechazos o notificaciones diferenciales; no realiza movimiento monetario.

## Ajuste aplicado

- Vista principal renombrada a `Matriz de campos SOAP`.
- Selector de servicio SOAP con descripcion funcional por operacion.
- Tabla principal con columnas: Servicio SOAP, Parametro SOAP, Tabla origen, Campo origen, Regla de conversion, Obligatorio, Estado, Ultima actualizacion y Acciones.
- Estados funcionales limitados a `Mapeado`, `Sin mapear` e `Inactivo`.
- Tablas origen visibles limitadas a `NachaHeaders`, `BatchHeaders`, `EntryDetails`, `AddendaRecords`, `BatchControls` y `FileControls`.
- Usuario solo consulta ve la matriz y detalle/auditoria secundaria, sin acciones de edicion.
- Usuario administrador puede crear borrador, editar relacion y abrir editor avanzado como accion secundaria.
- Historial, ruta tecnica del campo y detalle tecnico quedan fuera de la vista principal.
- `/integraciones/soap-settings` conserva responsabilidad exclusiva sobre configuracion tecnica SOAP.

## Restricciones conservadas

- No se modificaron contratos SOAP.
- No se cambio logica monetaria.
- No se alteraron reglas NACHA-M.
- No se modificaron golden files.
- No se agregaron dependencias.
- No se introdujo OpenBao.
- No se exponen secretos, credenciales ni certificados privados.
- Productivo permanece **NO-GO**.

## Validacion esperada

- Build Angular exitoso.
- Tests Angular exitosos.
- Build .NET exitoso.
- Tests backend exitosos.
- Verificacion manual de `/integraciones/mappings` y `/integraciones/soap-settings` en desktop y responsive.
