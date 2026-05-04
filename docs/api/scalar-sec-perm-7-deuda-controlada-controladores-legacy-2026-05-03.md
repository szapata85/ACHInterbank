# Scalar-SEC-PERM-7 — Deuda controlada de controladores legacy restantes

TOTAL_CONTROLLERS_LEGACY=22
TOTAL_OPERACIONES_OPENAPI=213
OPERACIONES_CON_SECURITY=207
OPERACIONES_SIN_SECURITY=6
OPERACIONES_SIN_SECURITY_NO_ESPERADAS=0
ESCRITURAS_SIN_SECURITY_NO_ESPERADAS=0
Suite PERM-6D previa: 440/440 OK.

Se documenta deuda controlada del remanente legacy; IntegrationMappingSetsController fue identificado como top candidato en PERM-7C.

## Qué NO se implementó en Scalar-SEC-PERM-7
- No se migraron controladores legacy restantes.
- No se migró IntegrationMappingSetsController en esta fase.
- No se migró AchCyclesController en esta fase.
- No se migró ReportsController en esta fase.
- No se eliminaron CanReadAch ni CanManageAch.
- No se exigieron permisos finos de forma exclusiva.
- No se cambiaron rutas.
- No se cambiaron contratos.
- No se cambiaron DTOs.
- No se cambiaron roles reales.
- No se crearon migraciones.
- No se ejecutó pentest.
- No se declara producción lista.

## Veredicto
- Scalar-SEC-PERM-7: CERRADO como diagnóstico, limpieza y deuda controlada.
- Deuda legacy restante: aceptada y documentada.
- Seguridad OpenAPI: sin huecos inesperados según evidencia previa.
- Producción lista: NO.
