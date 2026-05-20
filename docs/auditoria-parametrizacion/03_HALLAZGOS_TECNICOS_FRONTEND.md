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
## Revalidacion runtime 2026-05-20

- Ruta Angular `/transactions/clearing-house-rules`: disponible por fallback SPA.
- Menu dinamico `/navigation/menu`: incluye `/transactions/clearing-house-rules` bajo Transacciones para `admin` multirol.
- APIs consumidas por pantalla: `/api/clearing-house-transaction-rules` y `/api/transaction-prerequisite-policy/preview` responden con Bearer.
- Validacion visual automatizada con navegador integrado: no ejecutada en esta sesion; queda cubierta por validacion HTTP/API y pendiente de evidencia visual manual.

## Simulador NACHA-M Entrada - 2026-05-20

- Nueva ruta Angular: `/uat/nacha-inbound-simulator`.
- Nueva opcion de menu seed: `UAT / Simuladores > Simulador NACHA-M Entrada`.
- La pantalla permite seleccionar camara, escenario, modo de respuesta, fecha de negocio, ciclo, referencias y causal cuando aplica.
- La pantalla consume exclusivamente `/api/uat/nacha-inbound-simulator`; no invoca NachaUpload.
- Mensaje visible obligatorio: el archivo generado debe cargarse posteriormente por NachaUpload.
- Maneja errores 401/403/422 y muestra codigos funcionales para UAT.
