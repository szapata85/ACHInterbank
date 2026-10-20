# Paquete de implementación — CFA SOAP UX + Mapping dinámico

**Programa:** `CFA-SOAP-UX-MAP-001`  
**Estado inicial:** `PLANNED`  
**Siguiente fase permitida:** `P0`  
**Fecha base:** 2026-10-12

Este paquete contiene la arquitectura y el plan de implementación para dos objetivos inseparables:

1. Rediseñar el SPA de administración de integraciones CFA con Angular Material, branding dinámico, lenguaje funcional en español, accesibilidad y diseño adaptable real.
2. Evolucionar el mapping de los tres servicios SOAP CFA hacia un modelo dirigido por contrato y versionado, de forma que agregar un nuevo campo escalar solicitado por CFA no requiera cambios de código cuando el dato de origen ya esté disponible y la transformación necesaria esté soportada.

Servicios incluidos:

- `WSCFAACH.Proc_Contrapartidas`
- `WSCFAACH.Proc_Transacciones`
- `WSAXON.RegistrarRespuestaTransaccion`

## Documentos

1. `START_HERE.md` — contrato de ejecución y límites del programa.
2. `ARCHITECTURE_OVERVIEW.md` — arquitectura objetivo de extremo a extremo.
3. `SPA_UX_UI_ARCHITECTURE.md` — arquitectura frontend, Angular Material, responsive, accesibilidad y lenguaje funcional.
4. `SOAP_CONTRACT_MAPPING_ARCHITECTURE.md` — contratos SOAP versionados, mapping dinámico y serialización para los tres servicios.
5. `BACKEND_SOLID_DESIGN.md` — componentes, interfaces, patrones y aplicación de SOLID/Clean Architecture.
6. `DATA_MODEL_AND_MIGRATIONS.md` — modelo EF Core objetivo, migraciones y compatibilidad de datos.
7. `API_CONTRACTS.md` — endpoints y read/command models que debe consumir el SPA.
8. `IMPLEMENTATION_PLAN.md` — fases P0–P8 con entradas, cambios, pruebas y criterios de salida.
9. `TESTING_AND_RELEASE_GATES.md` — estrategia de pruebas, paridad SOAP, UX, accesibilidad, migraciones y cierre.
10. `MEMORY_STATE.md` — estado durable de la iniciativa y siguiente acción permitida.
11. `MEMORY_PATCH.md` — bloque compacto sugerido para el `MEMORY.md` raíz al iniciar formalmente el programa.
12. `MASTER_PROMPT_CHATGPT.md` — bootstrap amplio; no usarlo en cada turno.
13. `CHATGPT_MICRO_START.md` — prompt ultracorto recomendado para cada conversación.
14. `MICRO_PROMPT_PROTOCOL.md` — reglas para mantener contexto pequeño y cambios incrementales.
15. `MICRO_PROMPTS_INDEX.md` — secuencia P0.1–P8.4.
16. `micro-prompts/P0.md` … `P8.md` — bloques ejecutables, uno por turno.

## Resultado técnico buscado

El flujo objetivo es:

```text
Contrato CFA versionado
        ↓
Parámetros técnicos permitidos
        ↓
Metadatos funcionales en español
        ↓
Mapping publicado por versión
        ↓
Resolución de fuentes y transformaciones
        ↓
Solicitud SOAP construida dinámicamente
        ↓
Transporte SOAP existente
```

Y para un cambio de CFA:

```text
CFA agrega un campo escalar
        ↓
Crear/importar nueva versión de contrato
        ↓
El SPA muestra "dato nuevo pendiente"
        ↓
Usuario selecciona el origen del valor
        ↓
Validar + previsualizar
        ↓
Publicar/activar versión
        ↓
Sin cambio de código del cliente SOAP
```

Si el dato solicitado no existe en ACHInterbank, cambia la estructura del SOAP, cambia una respuesta o se necesita lógica de negocio nueva, el sistema debe clasificar el cambio como `REQUIERE_DESARROLLO`; nunca debe inventar información ni permitir XML libre.

## Regla principal

**Dinámico no significa libre.** Todos los parámetros transmitidos deben pertenecer a una versión de contrato CFA aprobada y todos los valores deben resolverse mediante fuentes y transformaciones registradas por la aplicación.


## Ejecución recomendada en ChatGPT

Para evitar conversaciones pesadas, **no pegar el prompt maestro en cada turno**. Abrir una conversación con `CHATGPT_MICRO_START.md`; el modelo leerá `next_micro_prompt` desde `MEMORY_STATE.md` y ejecutará un único incremento. El estado persistente permite cambiar de conversación después de cada micro-prompt sin perder continuidad.
