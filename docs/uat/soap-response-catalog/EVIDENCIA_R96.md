# Evidencia R96

Fecha: 2026-07-16. Datos usados: exclusivamente sintéticos y enmascarados.

## Implementación

- El bootstrap inserta o actualiza R96 para ambos métodos sin cambiar IDs existentes.
- `IntegrationResponseCatalogResolver` normaliza espacios y mayúsculas y consulta `Source + Method + Code`.
- Los parsers SOAP extraen el código, pero ya no contienen el significado funcional de R96.
- Los flujos de débito y crédito persisten `CatalogId`, código, descripción, estado de transporte, estado funcional, reintento y revisión manual.
- La consulta posterior no vuelve a leer XML.

## Validación automatizada

| Validación | Resultado |
| --- | --- |
| Seed/resolución dirigida | 37/37 y suites de flujo 28/28 |
| SQL Server y PostgreSQL reales | 2/2 |
| Backend completo | 1821 aprobadas, 1 omitida diagnóstica, 0 fallidas |
| Angular completo | 395/395 |
| Build Release | 0 warnings, 0 errores |

## Ejecución local controlada

Se persistió un único intento `Live` de `Proc_Contrapartidas` con resultado derivado R96:

- método correcto;
- transporte `Succeeded`;
- negocio `Success`;
- `ResponseCatalogId` presente;
- descripción de débito correcta;
- reintento deshabilitado;
- revisión manual deshabilitada;
- respuesta consultable después de reiniciar la API;
- cero referencias a `Proc_Transacciones`, `RegistrarRespuestaTransaccion` o `<METODO>` en el request derivado validado;
- un solo intento persistido.

No se conserva aquí request, response, token, cuenta, identificación ni correlador transaccional.

## Limitación de evidencia LIVE

Playwright terminó fallido después del dispatch porque la consulta de evidencia filtraba por un nombre de auditoría diferente al principal autenticado. La consulta fue corregida para correlacionar por la transacción sintética única, pero no se repitió el débito. Además, el archivo plano WCF no registró una entrada nueva demostrable. Por estas dos condiciones la certificación Playwright LIVE permanece NO-GO, aunque la persistencia técnica R96 fue comprobada.

