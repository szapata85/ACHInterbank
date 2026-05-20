# Hallazgos Tecnicos Frontend

Fecha: 2026-05-19

## Modulo Revisado

La SPA usa `TransactionsModule` con rutas hijas para operaciones ACH. Ya existia una pantalla administrativa comparable: `CycleConfigManagementComponent`.

## Ubicacion Elegida

Ruta Angular:

`/transactions/clearing-house-rules`

Menu:

`Transacciones > Reglas por camara`

## Funciones Implementadas

- Listado con grilla empresarial.
- Filtro por camara.
- Filtro por naturaleza.
- Incluir/excluir inactivas.
- Crear regla.
- Editar regla.
- Activar/inactivar.
- Preview de politica de prerequisitos.
- Manejo de error 422 devuelto por API al guardar reglas invalidas o solapadas.

## Roles y Seguridad

La pantalla consume endpoints protegidos. La lectura requiere permiso de configuracion/lectura ACH y la mutacion requiere permiso de administracion de configuracion. No se deshabilito autenticacion ni autorizacion.

## Observaciones

No se cambio Nginx porque los nuevos endpoints usan prefijo `/api`, ya cubierto por el proxy SPA Docker.
