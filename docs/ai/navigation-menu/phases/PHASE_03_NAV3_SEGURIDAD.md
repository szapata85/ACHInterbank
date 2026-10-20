# NAV3 — Seguridad y herramientas de prueba

## Objetivo
Mostrar herramientas técnicas por autorización, incluso en Production.

## Implementar
- permiso `TestTools.Access` o nombre equivalente aprobado;
- rol(es) técnicos aditivos;
- asignación rol→permiso;
- menú `Herramientas de prueba` siempre sembrado;
- retirar filtro `IsProduction()` de navegación UAT;
- proteger simulador y pruebas de servicios con permiso técnico.

## No hacer
No otorgar el permiso a `ACH.Operator`. No depender de checks de rol en Angular.

## Gate
Usuario sin permiso no ve/no entra. Usuario técnico sí ve/entra en cualquier ambiente.
