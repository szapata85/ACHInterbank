# Hallazgos Tecnicos Backend

Fecha: 2026-05-19

## Componentes Revisados

| Componente | Hallazgo |
|---|---|
| `NachaFileBuilder` | La validacion de prenotificacion estaba ligada al tipo de transaccion y exigia prenotificacion a toda transaccion monetaria no prenote. |
| `AchPrenotificationPolicy` | Existia politica por `TransactionType`, sin camara ni fuente normativa por vigencia. |
| `RegulatoryCatalogSeeder` | Sembraba reglas globales debit/credit, no reglas por ACH Colombia/CENIT. |
| `NachaExportController` | Ya devolvia 422 controlado para prerequisitos desde DEF-UAT-021. |
| `Proc_Contrapartidas` | DryRun UAT/local protegido desde DEF-UAT-022; no se modifica en esta fase. |

## Brecha Tecnica

La decision de prenotificacion para export NACHA-M necesitaba considerar:

- Camara de compensacion.
- Naturaleza debit/credit.
- Tipo de transaccion.
- Vigencia.
- Fuente normativa.
- Aplicabilidad a export NACHA-M y transacciones monetarias.

## Implementacion Aplicada

Se agregaron:

- Entidad `ClearingHouseTransactionRule`.
- Enums `TransactionNature`, `PrenotificationRequirementMode`, `ValidationRequirementMode`.
- Servicio `ITransactionPrerequisitePolicyService`.
- Servicio CRUD `IClearingHouseTransactionRuleService`.
- Migracion EF `AddClearingHouseTransactionRules`.
- Seeds iniciales para ACH Colombia y CENIT con fuente normativa.

## Riesgo Residual

Las reglas parametrizadas no autorizan generacion productiva. DEF-UAT-020 permanece abierto hasta ejecutar UAT NACHA-M con prenotificaciones validas y archivo no vacio.
