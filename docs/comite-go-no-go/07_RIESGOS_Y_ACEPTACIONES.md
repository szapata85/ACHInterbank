# Riesgos y Aceptaciones - Comite GO/NO-GO

Fecha: 2026-05-19
Estado general: Riesgos no aceptables para productivo sin cierre de brechas bloqueantes

## Riesgos Consolidados

| Riesgo | Probabilidad | Impacto | Mitigacion | Requiere aceptacion formal | Responsable |
| --- | --- | --- | --- | --- | --- |
| Salida productiva sin UAT funcional formal | Media | Alto | Ejecutar UAT formal con actas y evidencia completa | Si | Negocio / QA |
| Rol ACH.Operator no validado | Media | Alto | Corregir seed/claims o definir usuario operador sintetico formal | Si | Seguridad / Backend |
| NACHA-M sin validacion campo-a-campo | Alta | Critico | Ejecutar matriz de validacion 1/5/6/7/8/9 y homologacion/waiver | Si | Arquitectura ACH |
| CENIT/CUD pendiente | Media | Critico | Definir alcance, pruebas sinteticas y aprobacion de interoperabilidad | Si | Integracion / Negocio |
| Sobre digital/firma/certificados pendiente | Media | Critico | Validar flujo criptografico y custodia segura de certificados | Si | Seguridad |
| Gestion de secretos incompleta | Media | Alto | Completar estrategia OpenBao/secrets segun alcance | Si | Seguridad / DevOps |
| Backup/restore/rollback no probado | Media | Critico | Ejecutar prueba documentada de recuperacion y rollback | Si | Operaciones / SRE |
| Evidencia visual/operativa insuficiente | Media | Medio | Completar capturas, bitacoras y evidencia trazable | Si | QA / Operaciones |
| Integracion externa no homologada | Media | Critico | Homologacion formal o waiver aprobado | Si | Negocio / Integracion |
| Aprobaciones formales pendientes | Alta | Critico | Obtener firmas de negocio, seguridad, operaciones, auditoria y direccion | Si | PMO / Direccion |

## Aclaracion

No se recomienda aceptar riesgo para productivo hasta cerrar las brechas bloqueantes.

## Riesgo de Continuar UAT Controlado

Continuar UAT controlado es aceptable siempre que se mantengan datos sinteticos, no se conecten terceros reales, no se usen certificados productivos y no se modifique la decision NO-GO productivo.
