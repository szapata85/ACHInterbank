# Especificación UX/UI — Clientes, terceros, cuentas y validaciones

## 1. Principio

La UI debe reflejar la separación de dominio:

- **Tercero/Cuenta**: maestro genérico, independiente de cámara.
- **Validaciones operativas**: contexto ACH Colombia/CENIT y crédito/débito.

La cámara nunca se solicita como parte del alta de una cuenta.

## 2. Navegación

```text
Clientes
  -> Detalle cliente
     -> Terceros
        -> Tercero
           -> Cuentas
              -> Cuenta
                 -> Datos maestros
                 -> Validaciones operativas
                 -> Historial

Operación ACH
  -> Terceros y prenotificaciones (consola global)
```

## 3. Lista de terceros

Mostrar:

- nombre/razón social;
- documento enmascarado;
- número de cuentas;
- estado administrativo;
- resumen de validaciones pendientes/rechazadas como indicador separado;
- última actividad.

Acciones: agregar tercero, agregar cuenta, detalle, desactivar según permisos.

Badge de origen si existe metadata: `Transacción | Manual | Backfill`.

## 4. Detalle de cuenta

### Datos maestros

- entidad receptora;
- tipo de cuenta;
- cuenta enmascarada;
- titular/identificación enmascarada;
- estado administrativo;
- origen/fechas/auditoría.

### Validaciones operativas

Lista/tarjetas por contexto existente:

- cámara;
- naturaleza;
- servicio cuando aplique;
- requisito efectivo (`Obligatoria`, `Opcional`, `No requerida`), calculado desde policy;
- estado de validación;
- intento actual;
- transaction/correlation reference;
- deadline/resolution;
- causal y detalle permitido;
- vigencia/expiración si aplica.

No presentar estos contextos como “cuentas distintas” ni como parte del onboarding.

## 5. Alta manual

Wizard corto:

1. identidad del tercero;
2. cuenta: entidad, tipo, número;
3. resumen;
4. guardar.

**No pedir cámara ni crédito/débito.**

Después de guardar, una acción separada `Solicitar validación` puede pedir contexto operacional cuando el usuario realmente necesite prenotificar.

## 6. Estados UX

### Maestro

`Activo | Inactivo | Borrador`.

### Validación

`No iniciada | Pendiente de envío | En validación | Validada | Rechazada | Expirada | Suspendida`.

`Prenotificación no requerida` es una **decisión de política/elegibilidad para un contexto**, no un estado maestro permanente.

## 7. Acciones permitidas

- editar datos maestros;
- desactivar/reactivar;
- solicitar prenotificación;
- volver a prenotificar tras corrección;
- ver intento/transacción/evidencia;
- consultar por qué una cuenta no es elegible.

No permitidas:

- aprobar/rechazar manualmente resultado interbancario;
- editar causal recibida;
- borrar historial.

## 8. Consola global

Evolucionar `/customer-third-parties` sin perder trazabilidad. Filtros:

- cliente;
- tercero/cuenta;
- entidad receptora;
- cámara/naturaleza/servicio (sólo para validaciones);
- estado validación;
- causal;
- fechas;
- origen del maestro.

## 9. Creación de transacciones

El selector recibe contexto de la operación y consulta elegibilidad:

- cuenta origen/cliente;
- cámara resuelta;
- crédito/débito;
- servicio/fecha efectiva;
- monto u otras dimensiones sólo si la policy las requiere.

La cuenta canónica no cambia por escoger otra cámara. La salida distingue:

- `Allowed`;
- `AllowedWithoutPrenotification`;
- `RequiresPrenotification`;
- `PrenotificationPending`;
- `ValidationExpired`;
- `Rejected/Blocked/Unsupported`.

La entrada manual actual se conserva cuando el flujo vigente la soporta, activando onboarding silencioso genérico.

## 10. Accesibilidad y branding

Angular Material/tokens actuales, WCAG AA, foco visible, estado no dependiente sólo de color, `aria-live` donde aplique, responsive y PII enmascarada por defecto.
