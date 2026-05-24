# Auditoria Opcion C NACHA Config

Fecha: 2026-05-24

Esta auditoria corresponde a Fase 6A. No implementa cambios de codigo, reglas, migraciones ni generacion NACHA-M.

## Decision tecnica

Opcion C es la direccion recomendada, pero el sistema actual no esta listo para adoptarla como fuente oficial sin Fase 6B.

Estado actual: **legacy-first / table-driven parcial**.

## Evidencia principal

- Modo default: `LEGACY`.
- Builder carga definiciones y layouts legacy.
- `nacha-config` soporta perfil/camara/vigencia/version/status/campos/reglas.
- `NachaConfigResolver` existe, pero missing profile/layout no es fail-fast oficial.
- No hay perfil CENIT completo evidenciado.
- No hay trace normalizado FieldDefinition -> valor generado.
- Rutas legacy siguen visibles y operativas.

## Entregables de auditoria

Ver `docs/uat/evidencias/nacha-config-option-c-audit/`.

## Conclusion

Fase 6B debe convertir perfiles publicados/vigentes por camara en fuente obligatoria, eliminar fallback legacy en modo oficial, agregar errores controlados y normalizar la trazabilidad.

Productivo: **NO-GO**.

