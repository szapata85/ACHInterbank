# Deprecacion propuesta de layouts/definitions NACHA legacy

## Pantallas legacy

- `/ach-cycles/nacha/layouts`
- `/ach-cycles/nacha/definitions`

## Motivo

Estas pantallas administran `NachaRecordLayout`, `NachaRecordField` y `NachaRecordDefinition`, que no tienen separacion por camara, vigencia, version normativa ni estado publicado. Por tanto no cumplen el modelo oficial Opcion C.

## Recomendacion Fase 6B

1. Mantener temporalmente como solo lectura o historico.
2. Mostrar aviso "Legacy / no oficial para nueva parametrizacion".
3. Redirigir la administracion oficial a `/nacha-config-admin/perfiles`.
4. Evitar que cambios legacy afecten generacion oficial.
5. Conservar shadow compare solo mientras dure la migracion.

Productivo: **NO-GO**.

## Estado tras Fase 6B.2

El builder oficial `TABLE_DRIVEN` ya no lee `NachaRecordLayout` ni `NachaRecordDefinition` para exportacion oficial UAT/local. Las rutas y entidades legacy permanecen fisicamente para consulta historica, pruebas y transicion, pero no son fallback automatico.

Pendiente Fase 6B.4:

- declarar visualmente las pantallas legacy como no oficiales;
- redirigir administracion operativa a `/nacha-config-admin/perfiles`;
- evitar ambiguedad para usuarios de parametrizacion.

Productivo: **NO-GO**.
