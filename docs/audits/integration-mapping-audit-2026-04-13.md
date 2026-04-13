# Auditoría integral — Integraciones SOAP/mapping (2026-04-13)

## Alcance auditado
- WscfaachSoapClient
- Proc_Contrapartidas (catálogo, mapping set/rules, resolver funcional, request mapper, parser de respuesta)
- Preparación para Proc_Transacciones
- Seed/catálogos/contratos internos
- Angular `/integraciones`, `/integraciones/soap-settings`, `/integraciones/mappings`, compare/preview/validate/publish/clone

## Resumen ejecutivo
El proyecto **sí tiene cimientos aprovechables** (motor de versionado, validación, preview, historial y compare), pero está **desalineado en el contrato funcional real de Proc_Contrapartidas** y en la UX principal para usuario no técnico. La implementación actual modela un payload anidado de ciclo/transacción/addenda que no coincide con el contrato real plano del servicio (OF*/ANS*), por lo que la autonomía de negocio está comprometida.

## Diagnóstico técnico actual
### Lo que sí sirve
1. Servicio de MappingSet con ciclo de vida (draft/publish/archive), clonación, historial y comparación por parámetro.
2. Validación estructural/funcional con cobertura por parámetro y hints.
3. Preview con contexto controlado y real.
4. Cliente SOAP parametrizable por endpoint/action.

### Lo crítico mal enfocado
1. **Contrato interno de Proc_Contrapartidas incorrecto**: usa `ClearingHouseId`, `Transactions[]`, `Addendas[]` en vez de los campos reales `OFNIT..ANSIDREVER`.
2. **Catálogo de parámetros incorrecto**: seed solo crea método `WSCFAACH.Proc_Contrapartidas` pero con parámetros del modelo anidado, no del contrato real.
3. **Mapper SOAP de Proc_Contrapartidas incorrecto**: construye XML anidado no alineado al input real del método.
4. **Parser de respuesta desacoplado del output real**: parsea códigos genéricos y nodos transaccionales, no `ANSIDLOTE/ANSST/ANCLC/ANSIDTX/ANSIDREVER`.
5. **Fallback silencioso con catch vacío** en el mapper, ocultando fallos de mapping configurable.
6. **Sin arquitectura equivalente para Proc_Transacciones** en catálogo/motor (solo existe invocación SOAP/config técnica).

## Diagnóstico funcional actual
- La interfaz mezcla “configuración técnica SOAP” con “mapping funcional”, y el editor expone términos técnicos (`SourceKind`, `SourceFieldPath`, `ConditionExpression`) como experiencia principal.
- Aunque hay textos en español, la UX sigue orientada a operador técnico; no guía por objetivo de negocio (“qué dato de mi sistema llena OFNIT”).
- No existe experiencia orientada al contrato plano real de contrapartidas ni un “asistente por campos OF*/ANS*”.

## Hallazgos por severidad
### Crítico
1. Modelo/contrato interno de Proc_Contrapartidas no coincide con contrato real del servicio.
2. Catálogo/seed de parámetros no coincide con el contrato real.
3. Generación de XML request de Proc_Contrapartidas no coincide con el contrato real.
4. Parser de respuesta no parsea output contractual real.

### Alto
1. UX del editor centrada en primitivas técnicas, no en usuario no técnico.
2. Fallback silencioso (catch vacío) degrada trazabilidad y auditabilidad.
3. Seguridad/permisos amarrados a `CanManageUsers`, no a permisos específicos de integración ACH.
4. Ausencia de diseño dual de método (Contrapartidas + Transacciones) en catálogo funcional reusable.

### Medio
1. Nomenclatura mixta ES/EN y camel-case inconsistente para paths.
2. `PreviewResult` calcula `limitedItems` pero retorna colección completa.
3. `build default settings` usa mapeos de parámetros de entrada genéricos (`transaccion`, `lote`) sin reflejar contrato.

### Bajo
1. Textos UI todavía técnicos en títulos/labels.
2. Estructura de navegación puede simplificarse a un flujo asistido único para negocio.

## Reutilización recomendada (backend)
### Reutilizar (con refactor)
- `IntegrationMappingSetService` (lifecycle, publish, clone, history, compare).
- `IntegrationMappingValidationService` (núcleo de validación + hints).
- `IntegrationMappingPreviewService` (mecánica de preview, no su modelo de parámetros actual).
- `WscfaachSoapClient` (mecanismo de envío, resolución endpoint/action).
- Estructura de entidades `Integration*` (sets/rules/history) y configuración EF.

### Eliminar o sustituir
- Contratos `ProcContrapartidasRequest*` actuales.
- `BuildProcContrapartidasParameterCatalog` y source catalog actuales.
- Lógica del `ProcContrapartidasRequestMapper` y `ProcContrapartidasFunctionalMappingResolver` basada en Transactions/Addendas.
- Parser de respuesta actual por parser estricto del output real.

## Reutilización recomendada (frontend)
### Reutilizar (con refactor fuerte)
- Páginas de listado de MappingSets y compare (base de versionado/auditoría).
- Servicios Angular `IntegrationMappingAdminService`.
- Bloques de validación/preview, pero cambiando semántica y copy.

### Borrar / reescribir / fusionar
1. **Borrar como experiencia principal** la pantalla actual de configuración técnica SOAP dentro del flujo de negocio (dejarla en área avanzada/admin).
2. **Reescribir** `mapping-editor-page` en modo asistido por etapas para usuarios no técnicos.
3. **Fusionar** workspace + mappings en un “Asistente de configuración de envío a Contrapartidas”.
4. Mantener compare/history como vistas avanzadas de auditoría (no pantalla inicial).

## Propuesta final de reorientación
1. **Modelo canónico por método SOAP**
   - Método 1: `WSCFAACH.Proc_Contrapartidas` con parámetros EXACTOS del contrato real (OF*/ANS* input + ANS* output).
   - Método 2: `WSCFAACH.Proc_Transacciones` con parámetros EXACTOS (TREG..RTALOC).
2. **Motor único reusable**
   - Mismo `IntegrationMappingSet/Rule/History` para ambos métodos.
   - Resolver por parámetro escalar (sin imponer estructura anidada).
3. **Separación clara técnica vs funcional**
   - Técnica SOAP (endpoint/action/auth) en módulo admin avanzado.
   - Funcional mapping en asistente de negocio.
4. **UX no técnica**
   - Pregunta guía: “¿De dónde sale OFNIT?”
   - Catálogo de orígenes en español con ejemplos.
   - Validación con mensajes accionables de negocio.
5. **Trazabilidad bancaria**
   - Auditoría fuerte en publicación, diferencias y evidencias de preview.
   - Sin fallback silencioso.

## Fases de implementación sugeridas
1. **Fase 0 (bloqueo de desvío):** congelar nuevas features en editor actual; definir contrato real como fuente única.
2. **Fase 1 (backend contractual):** rehacer catálogo/seed/DTOs/mapper/parser de Contrapartidas al contrato real; eliminar fallback silencioso.
3. **Fase 2 (frontend asistido):** rediseñar editor en wizard de negocio (campos OF*/ANS*).
4. **Fase 3 (auditoría y gobierno):** permisos dedicados, evidencia preview/publish, hardening validaciones.
5. **Fase 4 (escalado a Transacciones):** replicar por configuración de método, sin duplicar arquitectura.

## Riesgos residuales si no se corrige
- Envíos SOAP funcionalmente incorrectos con rechazo en ambiente real.
- Operación dependiente de perfiles técnicos (objetivo de autonomía incumplido).
- Aumento de deuda técnica por duplicar “parches” sobre modelo incorrecto.
- Riesgo de auditoría/compliance por trazabilidad incompleta ante incidencias.
