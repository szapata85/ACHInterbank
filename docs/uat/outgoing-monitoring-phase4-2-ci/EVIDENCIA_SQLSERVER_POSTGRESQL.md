# Evidencia SQL Server y PostgreSQL

## Pruebas multimotor oficiales

Se ejecuto la categoria preexistente `OutgoingMonitorMultiDb` con sus variables oficiales contra ambos motores. Resultado: 2 aprobadas, 0 fallidas.

En cada proveedor la prueba creo una base aislada, ejecuto bootstrap tres veces y verifico:

- un mapping publicado activo de `Proc_Contrapartidas`;
- 17 reglas obligatorias, sin duplicados;
- estabilidad de `MappingSetId` e IDs de reglas;
- referencia, monto y direccion reales en el resolver;
- readiness construible, sin fallback y solo con warning tecnico de IP;
- settings SOAP unicos e idempotentes.

## Bases nuevas LIVE

| Motor | Base aislada | Mapping | Readiness | Resolver | Duplicados |
| --- | --- | --- | --- | --- | --- |
| SQL Server | `ACHInterbankPhase42Sql` | version 1, 17/17 | construible, sin fallback | fuentes reales | 0 |
| PostgreSQL | `ACHInterbankPhase42Pg` | version 1, 17/17 | construible, sin fallback | fuentes reales | 0 |

Las definiciones funcionales fueron iguales en ambos motores. La diferencia de serializacion de fecha de PostgreSQL (`Z`) no cambio el mapping ni las conversiones.

La base principal SQL Server contenia un mapping publicado no identificado como `seed`; el bootstrap lo preservo y readiness lo rechazo por placeholders. Esto valida simultaneamente la no sobrescritura de mappings de usuario y el fail-safe de readiness.
