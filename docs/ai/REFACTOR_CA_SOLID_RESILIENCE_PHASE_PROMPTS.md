# Prompts de ejecución por fase — ACHInterbank Refactor

Usar un chat separado por fase/subfase cuando el cambio sea de riesgo medio/alto.

## R0
> Ejecuta únicamente R0 del programa `REFACTOR-CA-SOLID-RES-001`. Captura baseline, crea architecture tests tipo ratchet y fortalece characterization/golden guards. No cambies comportamiento. Ejecuta los gates disponibles, actualiza state memory y termina sin iniciar R1.

## R1
> Ejecuta únicamente R1. Limpia la frontera Application: HTTP/middleware/filtros a Api, WCF/generated proxies a External, X509 detrás de un port y NLog fuera de Application. Retira packages solo cuando ya no haya consumidores. Reduce el ratchet, valida y no avances a R2.

## R2
> Ejecuta únicamente R2. Elimina service construction oculta y migra globals de configuración hacia options/ports cohesivos, con fail-fast startup. No hagas abstracciones ceremoniales para helpers puros. Valida y no avances a R3.

## R3
> Ejecuta únicamente R3. Normaliza resiliencia y logging seguro de integraciones externas. Usa HttpClientFactory/typed clients donde corresponda, clasifica retries por idempotencia y agrega fault-injection tests. No cambies semántica financiera ni avances a R4.

## R4A
> Ejecuta únicamente R4A. Extrae componentes deterministas de bajo riesgo de `NachaFileBuilder`, conservando facade, callers y output byte-for-byte. Golden/regression primero. No avances a R4B.

## R4B
> Ejecuta únicamente R4B. Extrae/renderiza responsabilidades por tipo de registro solo donde no duplique el motor table-driven. Mantén PUBLISH authority y output byte-for-byte. No avances a R4C.

## R4C
> Ejecuta únicamente R4C. Separa el assembly pipeline de generación (selection/batches/render/totals/validation/artifact) manteniendo compatibilidad. No avances a R4D.

## R4D
> Ejecuta únicamente R4D. Encapsula decisiones específicas ACH Colombia/CENIT sin switch rígido por IDs, usando códigos/configuración/perfiles existentes. No introduzcas PaymentRail. No avances a R4E.

## R4E
> Ejecuta únicamente R4E. Adelgaza la facade y elimina fallbacks concretos restantes del builder. Ejecuta regresión outbound completa y no avances a R5A.

## R5A
> Ejecuta únicamente R5A. Extrae parser físico determinista y testeable sin DB. No cambies selección PUBLISH ni persistencia todavía. No avances a R5B.

## R5B
> Ejecuta únicamente R5B. Separa validación estructural y semántica/regulatoria manteniendo exacta autoridad del snapshot seleccionado. No avances a R5C.

## R5C
> Ejecuta únicamente R5C. Aísla persistencia del resultado parseado/validado, preservando transacciones, lineage y exact-file membership. No avances a R5D.

## R5D
> Ejecuta únicamente R5D. Convierte post-processing en pipeline de etapas explícitas, preservando classification, manual review, duplicate, idempotency y dispatch semantics. No avances a R6A.

## R6A
> Ejecuta únicamente R6A. Segrega contratos MFT por capacidades con facade temporal si reduce riesgo. No cambies estados persistidos ni Quartz. No avances a R6B.

## R6B
> Ejecuta únicamente R6B. Divide implementación MFT en workflows/query/admin/credentials/retry/archive, preservando lifecycle, claim/recovery y semantics de publicación. No avances a R6C.

## R6C
> Ejecuta únicamente R6C. Separa Incoming Command Center en query/read model y operations/commands sin cambiar API ni permisos. No avances a R7.

## R7
> Ejecuta únicamente R7. Descompón Returns/ROR por eligibility/correlation/assembly/generation/state/audit. No reabras R10/DEV14 ni mezcles reglas CENIT/ACH Colombia. No avances a R8A.

## R8A
> Ejecuta únicamente R8A. Modula ReportsController preservando rutas, DTOs, policies y OpenAPI. No avances a R8B.

## R8B
> Ejecuta únicamente el cleanup secundario explícitamente seleccionado. No uses tamaño de archivo como única justificación. Valida y no avances a R9.

## R9
> Ejecuta únicamente R9. Introduce tiempo determinista en rutas operacionales críticas y elimina sync-over-async de las rutas tocadas. Preserva timezone operacional. No avances a R10.

## R10
> Ejecuta únicamente R10. No agregues nuevas mejoras. Ejecuta recertificación interna, compara outputs, revisa architecture ratchet, build/tests/provider/E2E disponibles y actualiza documentación/memory. No declares release/UAT/homologación externa.
