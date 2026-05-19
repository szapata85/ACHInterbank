# Riesgos y Aceptaciones - Comite GO/NO-GO

Fecha: 2026-05-19
Estado general: Riesgos no aceptables para productivo sin cierre de brechas bloqueantes

| Riesgo | Probabilidad | Impacto | Mitigacion | Requiere aceptacion formal | Responsable |
| --- | --- | --- | --- | --- | --- |
| Salida productiva sin UAT funcional formal | Media | Alto | Ejecutar UAT formal con actas y evidencia completa | Si | Negocio / QA |
| Rol ACH.Operator no validado | Media | Alto | Corregir seed/claims o definir usuario operador sintetico | Si | Seguridad / Backend |
| NACHA-M sin validacion campo-a-campo | Alta | Critico | Ejecutar matriz 1/5/6/7/8/9 y homologacion/waiver | Si | Arquitectura ACH |
| CENIT/CUD pendiente | Media | Critico | Definir alcance y aprobacion de interoperabilidad | Si | Integracion / Negocio |
| Sobre digital/firma/certificados pendiente | Media | Critico | Validar flujo criptografico y custodia segura | Si | Seguridad |
| Gestion de secretos incompleta | Media | Alto | Completar estrategia OpenBao/secrets segun alcance | Si | Seguridad / DevOps |
| Backup/restore/rollback no probado | Media | Critico | Ejecutar prueba documentada | Si | Operaciones / SRE |
| Aprobaciones formales pendientes | Alta | Critico | Obtener firmas de negocio, seguridad, operaciones y direccion | Si | PMO / Direccion |

No se recomienda aceptar riesgo para productivo hasta cerrar las brechas bloqueantes.
