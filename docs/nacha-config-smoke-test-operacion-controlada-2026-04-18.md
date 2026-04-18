# Smoke test integral NACHA Config (operación administrativa controlada)

Fecha base del guion: 2026-04-18.

## Precondiciones

1. Usuario con permisos `CanReadAch` y `CanManageAch`.
2. API y SPA desplegadas con endpoint `nacha-config` disponible.
3. Existencia de al menos un perfil en estado `BORRADOR` y uno publicado para validar filtros y auditoría.

## Flujo integral y resultado esperado

1. **Abrir listado de perfiles**
   - Acción: navegar a `/nacha-config-admin/perfiles`.
   - Esperado: aparece grilla `ui-grilla-empresarial` con perfiles, sin error de carga.

2. **Filtrar por catálogos estructurados**
   - Acción: usar filtros de Estado/Cámara/Flujo/Dirección con `ui-selector-buscable`.
   - Esperado: las opciones provienen de `GET /nacha-config/catalogos-filtro`; no dependen solo de filas visibles.

3. **Crear borrador**
   - Acción: diligenciar formulario reactivo y pulsar `Crear borrador` una vez.
   - Esperado: botón entra en loading, se deshabilita anti doble clic, y redirige a workspace del nuevo perfil.

4. **Entrar al workspace**
   - Acción: abrir detalle del perfil desde la grilla.
   - Esperado: se visualiza resumen con estado, versión y token de concurrencia.

5. **Editar perfil**
   - Acción: actualizar metadatos en pestaña `Detalle y edición` y guardar.
   - Esperado: mensaje de éxito, rowVersion actualizado.

6. **Editar secuencia**
   - Acción: ajustar secuencia en grilla de registros y guardar.
   - Esperado: persiste nueva secuencia y se refresca grilla.

7. **Editar variante (guiado)**
   - Acción: seleccionar fila de variantes en grilla y guardar cambios en formulario.
   - Esperado: no se requiere escribir ID manual; sin selección el botón queda deshabilitado.

8. **Editar campo (guiado)**
   - Acción: seleccionar fila de campos y editar.
   - Esperado: formulario se precarga desde la selección; botón deshabilitado sin selección.

9. **Editar regla**
   - Acción: seleccionar regla en grilla y guardar cambios.
   - Esperado: flujo seleccionar → editar → guardar consistente con variantes/campos.

10. **Validar**
   - Acción: ejecutar validación prepublicación.
   - Esperado: resumen con errores bloqueantes/advertencias y lista de issues.

11. **Publicación bloqueada**
   - Acción: intentar publicar perfil con validaciones pendientes.
   - Esperado: alerta `PUBLISH_BLOCKED` y detalle de bloqueos.

12. **Preview del resolvedor**
   - Acción: ejecutar vista previa con contexto cámara/flujo/dirección/servicio y registros.
   - Esperado: resultado de perfil resuelto, layouts por record y trazas.

13. **Historial/snapshots**
   - Acción: abrir pestaña historial.
   - Esperado: timeline + grillas de auditoría e instantáneas.

14. **Concurrencia**
   - Acción: simular actualización paralela y guardar con token viejo.
   - Esperado: alerta de conflicto, opciones de recargar o descartar cambios locales.

## Criterio de aprobación

El smoke se considera aprobado cuando los 14 pasos completan su resultado esperado sin errores críticos (HTTP 5xx o bloqueo sin recuperación) y con trazabilidad de auditoría visible.
