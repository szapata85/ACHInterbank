# Fase 6A - Evaluacion SPA

## Rutas legacy

| Ruta | Estado | Riesgo Opcion C | Accion recomendada |
|---|---|---|---|
| `/ach-cycles/nacha/layouts` | Visible/operativa | Parece configuracion NACHA-M oficial aunque no tiene camara/vigencia/version | Deprecar, redirigir o dejar solo lectura |
| `/ach-cycles/nacha/definitions` | Visible/operativa | Permite editar secuencia legacy fuera de perfil oficial | Deprecar, redirigir o dejar solo lectura |

## Ruta candidata oficial

| Ruta | Capacidades | Brechas |
|---|---|---|
| `/nacha-config-admin/perfiles` | Lista perfiles, estado, camara, flujo, direccion, version | No se evidencio como menu oficial unico |
| `/nacha-config-admin/perfiles/:id` | Detalle, records, variantes, campos, reglas, validar, publicar, clonar, preview | `propertyPath` parece editable; debe venir de catalogo controlado |

Recomendacion: el modulo oficial debe ser `/nacha-config-admin/perfiles`. Las rutas legacy no deben competir como punto de parametrizacion oficial.

