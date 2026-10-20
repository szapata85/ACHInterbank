# CFA-SOAP-UX-MAP-001 — START HERE

**Estado:** `PLANNED`  
**Siguiente fase permitida:** `P0`  
**Baseline commit:** `UNKNOWN` en el paquete suministrado, porque la copia disponible no contiene metadata `.git`.

## 1. Orden de lectura para bootstrap amplio

El orden siguiente sirve para una primera revisión arquitectónica. **Para implementación cotidiana usar `CHATGPT_MICRO_START.md` y `MICRO_PROMPT_PROTOCOL.md`; no releer todo el paquete en cada turno.**

Antes de modificar código en un bootstrap amplio:

1. `AGENTS.md`
2. `MEMORY.md`
3. Este archivo.
4. `MEMORY_STATE.md`
5. `ARCHITECTURE_OVERVIEW.md`
6. `SOAP_CONTRACT_MAPPING_ARCHITECTURE.md`
7. `BACKEND_SOLID_DESIGN.md`
8. `DATA_MODEL_AND_MIGRATIONS.md`
9. `API_CONTRACTS.md`
10. `SPA_UX_UI_ARCHITECTURE.md`
11. `IMPLEMENTATION_PLAN.md`
12. `TESTING_AND_RELEASE_GATES.md`

La ejecución normal es todavía más granular: **un micro-prompt por intervención**. `MEMORY_STATE.md` define `next_micro_prompt`.

## 2. Objetivo exacto

Implementar una administración CFA comprensible para usuarios funcionales y una infraestructura de mapping SOAP extensible para los tres servicios existentes.

El usuario funcional debe trabajar con conceptos como:

- Créditos recibidos en CFA.
- Débitos originados en CFA.
- Respuestas de transacciones.
- Dato requerido por CFA.
- Origen del dato.
- Tratamiento del dato.
- Configuración completa/incompleta.
- Versión de contrato.
- Borrador/publicada/activa.

Los nombres técnicos (`Proc_Transacciones`, `OFCTA`, `idTransaccionAxon`, rutas de entidades, namespaces, tipos XSD) deben quedar disponibles como detalle avanzado y trazabilidad, no como lenguaje principal.

## 3. Invariantes no negociables

1. No cambiar reglas ACH/CENIT, NACHA-M, ciclos, causales, compensación, liquidación, clasificación monetaria ni elegibilidad por este programa.
2. No cambiar semántica de respuestas CFA para “facilitar” el refactor.
3. No introducir XML arbitrario editable por usuarios.
4. No permitir parámetros SOAP que no pertenezcan a una versión de contrato aprobada.
5. No ejecutar una versión de contrato nueva mientras falte mapping requerido.
6. No permitir que Angular sea autoridad sobre tipos, obligatoriedad, parámetros o fuentes de negocio.
7. No codificar catálogos funcionales CFA en el frontend si pueden provenir del servidor.
8. No duplicar el motor de resolución en TypeScript.
9. Mantener Angular Material 21 como biblioteca principal de UI.
10. Mantener branding dinámico; no agregar colores de marca fijos en las features.
11. Mantener published mappings/contract versions inmutables; evolucionar por clonación/nueva versión.
12. Mantener idempotencia, no-retry ciego y controles de outcome incierto existentes.
13. Toda transición de runtime debe tener pruebas de paridad de XML para los contratos actuales.
14. No retirar el camino legacy hasta que el motor nuevo demuestre paridad para los tres servicios.

## 4. Alcance de implementación

### Incluido

- versionado de contrato SOAP por método;
- registro/importación segura de nuevos parámetros;
- humanización de metadatos del contrato;
- vínculo MappingSet ↔ ContractVersion;
- motor genérico de resolución de valores;
- registro de proveedores de fuentes;
- registro de transformaciones;
- serializador SOAP dirigido por contrato;
- adaptación de los tres servicios al motor común;
- clonación y migración de mappings entre versiones;
- validación, previsualización, comparación y activación;
- SPA rediseñado;
- configuración técnica avanzada separada;
- responsive real, WCAG AA y navegación por teclado;
- pruebas backend, Angular y Playwright;
- migraciones PostgreSQL y SQL Server;
- actualización progresiva de `MEMORY_STATE.md`.

### Fuera de alcance

- crear métodos SOAP nuevos no solicitados;
- cambiar autenticación o transporte CFA salvo dependencia estricta de este programa;
- cambiar formatos NACHA-M;
- crear un lenguaje de expresiones arbitrarias;
- ejecución de WSDL remoto desde URL ingresada por usuario;
- autoejecución de cambios de contrato sin revisión humana;
- cambios en parsers de respuesta salvo que una nueva versión CFA cambie la respuesta y exista una fase adicional aprobada.

## 5. Regla de parametrización

Un campo nuevo CFA debe quedar clasificado así:

| Tipo de cambio | Resultado objetivo |
|---|---|
| Nuevo campo escalar y fuente ya disponible | `SIN_DESARROLLO` |
| Nuevo campo escalar y fuente no expuesta pero dato existente | `EXTENSION_FUENTE` |
| Nuevo tipo de transformación simple | `EXTENSION_TRANSFORMACION` |
| Objeto/colección no soportado | `REQUIERE_DESARROLLO` |
| Cambio de respuesta SOAP | `REQUIERE_DESARROLLO` |
| Cambio de autenticación/transporte | `REQUIERE_DESARROLLO` |
| Dato inexistente en ACHInterbank | `REQUIERE_DESARROLLO` |

## 6. Política para WSDL/contratos

La entrada preferida para una nueva versión es un archivo WSDL/XSD entregado por CFA y cargado localmente al sistema por un usuario autorizado.

Requisitos:

- no resolver entidades externas;
- DTD deshabilitado;
- tamaño máximo configurable;
- hash SHA-256 del documento;
- nombre y fecha registrados;
- diff contra versión activa;
- no activar automáticamente;
- tipos no soportados bloquean activación.

También puede existir alta manual avanzada de un parámetro cuando CFA entregue una especificación formal sin WSDL, siempre con referencia documental y auditoría.

## 7. Condiciones de parada

Detener la fase y registrar `BLOCKED` si:

- la implementación exige modificar semántica ACH/CENIT;
- no existe evidencia suficiente para interpretar un campo CFA;
- un cambio requiere datos sensibles no disponibles;
- aparece una estructura SOAP no soportada por el serializador;
- una migración rompe compatibilidad con mappings publicados existentes;
- el XML actual cambia sin una modificación contractual aprobada;
- Angular necesita duplicar una regla de backend para poder continuar;
- se requiere secreto, certificado o infraestructura externa no disponible;
- existen cambios concurrentes de usuario que puedan perderse.

## 8. Definition of Done global

El programa termina técnicamente cuando:

- los tres servicios usan el mismo pipeline de contrato/mapping/resolución/serialización;
- los XML actuales conservan paridad contractual;
- un parámetro escalar nuevo puede agregarse en una nueva versión de contrato sin modificar código del cliente SOAP;
- el SPA permite mapearlo con lenguaje funcional;
- publicación/activación es fail-closed;
- mappings y contratos anteriores permanecen auditables y reversibles;
- el frontend no contiene catálogos técnicos CFA duplicados;
- no existen colores de marca hardcodeados en el módulo;
- la experiencia funciona en móvil, tablet y escritorio;
- teclado y lector de pantalla pueden completar el flujo principal;
- pruebas de backend, migración, Angular y Playwright requeridas por `TESTING_AND_RELEASE_GATES.md` pasan o quedan explícitamente `NOT_RUN_ENVIRONMENT` sin ser presentadas como PASS.
