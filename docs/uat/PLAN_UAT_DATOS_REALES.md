# Plan UAT con Datos Reales o Anonimizados - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.1 preliminar  
Rama analizada: `ACH-Interbank-Postgresql`  
Solucion analizada: `ACHInterbank.sln`  
Estado: requiere validacion humana por Tecnologia, Operaciones, Negocio, Seguridad y Auditoria.

## 1. Objetivo

Definir el plan de ejecucion UAT para ACH Interbank usando datos reales anonimizados o datos representativos equivalentes, con trazabilidad entre SPA Angular, backend .NET, normativa/documentacion del repositorio, evidencias y actas firmables.

El objetivo del UAT es validar que los flujos ACH/CENIT/NACHA-M criticos funcionan en un ambiente controlado antes de cualquier decision de go productivo.

## 2. Alcance

Incluye:

- Autenticacion, autorizacion, roles y permisos operativos.
- Parametrizacion de camaras, ciclos, causales, estados y catalogos.
- Transacciones ACH de salida, bulk ingestion y carga/generacion NACHA-M.
- Registros NACHA-M tipo 1, 5, 6, 7, 8 y 9.
- Devoluciones de salida, devoluciones de entrada, casos huerfanos, rechazos, rechazo total, devolucion parcial y ROR.
- CENIT: ciclos, neteo, liquidez y frontera CUD.
- Conciliacion operativa y frontera no-contable.
- Sobre digital, firma, certificados y OpenBao si aplica al ambiente UAT.
- Auditoria, trazabilidad, idempotencia, reportes, reproceso y cierre operativo.
- Validacion SPA Angular contra endpoints backend reales.

## 3. Fuera De Alcance

- Declarar GO productivo sin acta, evidencia y aprobaciones formales.
- Guardar datos sensibles reales en Git.
- Guardar certificados privados, PFX, llaves privadas, tokens o passwords en Git.
- Crear reglas normativas no soportadas por documentos existentes.
- Ejecutar migraciones, borrar volumenes o cambiar secretos como parte de este plan documental.
- Declarar integracion CUD bancaria si no existe evidencia operacional o API real.
- Declarar contabilidad definitiva si el sistema solo entrega conciliacion/revision operativa.

## 4. Ambientes Requeridos

| Ambiente | Proposito | Evidencia requerida | Estado |
|---|---|---|---|
| Local tecnico | Build, smoke y preparacion de evidencias tecnicas | comandos y salidas no sensibles | PENDIENTE VALIDAR |
| UAT backend | Ejecucion API contra PostgreSQL UAT | URL, version, commit, health checks | PENDIENTE VALIDAR |
| UAT SPA | Ejecucion de pantallas Angular | URL, version, capturas | PENDIENTE VALIDAR |
| PostgreSQL UAT | Persistencia de datos UAT anonimizados | version, migracion aplicada, backups | PENDIENTE VALIDAR |
| OpenBao UAT | Gestion de secretos/certificados si aplica | estado, politica, referencias enmascaradas | PENDIENTE VALIDAR |
| Repositorio seguro externo | Custodia de evidencias sensibles | referencia, hash SHA256, responsable | PENDIENTE VALIDAR |

## 5. Prerrequisitos Tecnicos

| Prerrequisito | Ruta/evidencia | Estado |
|---|---|---|
| Solucion .NET principal disponible | `ACHInterbank.sln` | OK |
| API principal | `src/Cfa.ACHInterbank.Api` | OK |
| Persistencia PostgreSQL/EF | `src/Cfa.ACHInterbank.Persistence` | OK |
| SPA Angular | `web/ach-interbank-ui` | OK |
| Tests backend | `tests/Cfa.ACHInterbank.Tests` | OK |
| Docker Compose principal | `docker-compose.yml` | PARCIAL |
| Docker Compose test | `docker-compose.test.yml` | OK |
| OpenAPI/Scalar | `src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs` | OK |
| Health live/ready | `/health/live`, `/health/ready` | PARCIAL |
| OpenBao en compose principal | `docker-compose.yml` | NO ENCONTRADO |

## 6. Prerrequisitos Funcionales

- Catalogos de camaras, ciclos, causales y estados cargados o parametrizados.
- Usuarios UAT definidos con roles y permisos por perfil.
- Datos de clientes, cuentas, bancos, transacciones y archivos anonimizados.
- Casos UAT priorizados por criticidad.
- Criterios de aceptacion y rechazo firmados por Negocio/Operaciones.
- Proceso de escalamiento de defectos acordado.
- Repositorio seguro externo definido para evidencias sensibles.

## 7. Datos Reales o Anonimizados Representativos

UAT puede avanzar con datos anonimizados representativos siempre que:

- Mantengan estructura, longitudes, formatos, estados, montos y combinaciones funcionales equivalentes a la operacion real.
- Permitan validar reglas de camara, ciclo, causal, NACHA-M, trazabilidad e idempotencia.
- No expongan PII, cuentas reales, saldos reales, certificados privados, llaves privadas, tokens ni passwords.
- Usen hashes o referencias seguras para evidencias sensibles.

## 8. Politica De No Versionar Datos Sensibles En Git

No deben versionarse:

- Datos personales reales.
- Numeros de cuenta reales.
- Saldos reales.
- Certificados privados, PFX o llaves privadas.
- Tokens, passwords, client secrets, API keys o secretos OpenBao.
- Archivos NACHA-M reales sin anonimizar.
- Capturas o logs con datos sensibles sin enmascarar.

Permitido en Git:

- Hash SHA256 de evidencia.
- Referencias a ubicacion segura externa.
- Identificadores anonimizados.
- Capturas redactadas.
- Datos sinteticos o anonimizados sin reversibilidad razonable.

## 9. Reglas De Anonimizacion

| Tipo de dato | Regla minima | Observacion |
|---|---|---|
| Cliente ordenante/beneficiario | Sustituir nombre e identificacion por valores ficticios consistentes | Mantener tipo persona si es relevante |
| Cuenta | Enmascarar y reemplazar con numero no real de misma longitud | No conservar ultimos digitos reales si no hay autorizacion |
| Banco/institucion | Usar catalogo UAT o codigos reales publicos si no son sensibles | Validar con Operaciones |
| Monto | Usar rangos representativos, no saldos reales | Mantener casos limite |
| Archivo NACHA-M | Generar archivo controlado sin PII real | Registrar hash |
| CUD | Usar evidencia operacional controlada o referencia externa segura | No declarar API CUD si no existe |
| Certificados | Publicos permitidos si estan aprobados; privados fuera de Git | Referenciar `secretRef` enmascarado |

## 10. Roles Participantes

| Rol | Responsabilidad | Firma requerida |
|---|---|---|
| Tecnologia | Ambiente, build, API, SPA, base de datos, pruebas tecnicas | Si |
| Operaciones | Ejecucion de flujos ACH/CENIT y cierre operativo | Si |
| Negocio | Aceptacion funcional y priorizacion de defectos | Si |
| Seguridad | Certificados, firma, sobre digital, secretos y acceso | Si |
| Auditoria | Evidencia, trazabilidad, control documental | Si |
| Proveedor/camara | Validacion externa si aplica | PENDIENTE VALIDAR |

## 11. Criterios De Inicio

- Ambiente UAT disponible y accesible.
- Version del build, commit, rama e imagen identificados.
- Migraciones aplicadas en ambiente UAT o evidencia de esquema vigente.
- Usuarios y roles UAT creados.
- Datos anonimizados preparados.
- Health checks basicos OK.
- Plan de evidencias y defectos publicado.
- Riesgos conocidos comunicados al equipo UAT.

## 12. Criterios De Aceptacion

- Escenarios criticos ejecutados sin defectos bloqueantes.
- Evidencia de cada escenario registrada en `docs/uat/INDICE_EVIDENCIAS_UAT.md` o repositorio seguro externo.
- Defectos clasificados y cerrados, diferidos o aceptados formalmente.
- Acta UAT firmada por los roles requeridos.
- Cualquier brecha normativa queda marcada como PENDIENTE VALIDAR y no se usa para aprobar productivo.

## 13. Criterios De Rechazo

- Defecto bloqueante sin workaround aprobado.
- Inconsistencia de totales, hash, block count, registros NACHA-M o idempotencia.
- Exposicion de datos sensibles o secretos.
- Evidencia insuficiente o no trazable.
- Falta de aprobacion de Negocio, Operaciones, Seguridad o Auditoria para flujos criticos.
- CENIT/CUD, sobre digital o naming externo sin evidencia cuando son parte del alcance probado.

## 14. Criterios De Suspension

- Caida del ambiente UAT.
- Datos UAT incorrectos o sensibles sin autorizacion.
- Riesgo de afectacion a sistemas externos.
- Defecto bloqueante que invalida multiples escenarios.
- Hallazgo de seguridad critico.

## 15. Criterios De Cierre

- Todos los escenarios ejecutados o formalmente excluidos.
- Matriz de defectos actualizada.
- Indice de evidencias completo.
- Acta UAT emitida.
- Riesgos residuales aceptados por los responsables.
- Scorecard GO/NO-GO actualizado.

## 16. Riesgos

| Riesgo | Impacto | Mitigacion |
|---|---|---|
| UAT sin datos representativos | Validacion incompleta | Matriz de datos UAT obligatoria |
| Evidencia sensible en Git | Riesgo legal/seguridad | Repositorio seguro externo + hash |
| CENIT/CUD sin evidencia operacional | NO-GO productivo | Ejecutar E2E homologado o aceptar brecha |
| Sobre digital sin validacion externa | Rechazo externo | Validacion con camara/proveedor |
| SPA/backend desalineados | Flujo roto en UAT | Matriz SPA-backend-norma-UAT |

## 17. Supuestos Y Dependencias

- La rama analizada es `ACH-Interbank-Postgresql`.
- La solucion principal es `ACHInterbank.sln`.
- UAT no equivale a go productivo.
- La evidencia externa puede vivir fuera de Git, siempre con hash y referencia segura.
- Las reglas normativas deben provenir de `docs/normativa`, `docs/audits` o validacion humana/proveedor.

## 18. Evidencias Obligatorias

- Capturas SPA.
- Request/response redactados.
- Logs backend redactados.
- Hashes de archivos NACHA-M y archivos de respuesta.
- Eventos de auditoria/trazabilidad.
- Evidencia de idempotencia/reproceso.
- Evidencia de conciliacion.
- Evidencia CUD si aplica.
- Evidencia de certificado/firma/sobre digital si aplica.
- Resultados de build/test no destructivos.
- Acta UAT firmada.

## 19. Firmas Y Aprobaciones Requeridas

Productivo NO debe aprobarse sin:

- Acta UAT firmada.
- Evidencias completas.
- Responsables identificados.
- Riesgos residuales aceptados.
- Defectos bloqueantes cerrados o formalmente aceptados.
- Aprobacion de Negocio, Operaciones, Tecnologia, Seguridad y Auditoria.
- Aprobacion de proveedor/camara cuando aplique.

