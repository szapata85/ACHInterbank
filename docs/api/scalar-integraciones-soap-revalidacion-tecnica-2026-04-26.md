# Revalidación técnica SOAP-1B de integraciones `Proc_Transacciones` y `Proc_Contrapartidas`

**Fecha de revalidación:** 2026-04-26  
**Fase:** Corrección técnica SOAP-1B  
**Ámbito:** evidencia técnica de código, build y pruebas para los flujos SOAP internos relacionados con superficie REST publicada en OpenAPI/Scalar.

---

## 1) Resultado de inspección técnica

Se ejecutó inspección de estado del repositorio, historial reciente y búsqueda de artefactos de integración SOAP en `src`, `tests` y `docs`.

### 1.1 Comandos ejecutados

```bash
git status --short
git log --oneline -20
test -f docs/api/scalar-integraciones-soap-proc-transacciones-proc-contrapartidas-2026-04-26.md && echo "Documento SOAP base existe"
rg -n "Proc_Transacciones|Proc_Contrapartidas|ProcTransacciones|ProcContrapartidas|SOAP|Soap|Fault|faultstring|WscfaachSoapClient|IWscfaachSoapClient|ProcTransaccionesResponseParser|ProcContrapartidasResponseParser|ProcTransaccionesRequestMapper|ProcContrapartidasRequestMapper|ContrapartidaDispatchJobService|AchContrapartidasByCycleHandler|IncomingNachaPostProcessingOrchestrator|SoapIntegrationSettingsService" src tests docs -S
```

### 1.2 Hallazgos

- El documento SOAP base existe y está versionado en `docs/api`.
- La implementación activa confirma dos ramas técnicas separadas:
  - `Proc_Transacciones`: mapper + parser + orquestador inbound + cliente SOAP.
  - `Proc_Contrapartidas`: mapper + parser + job handler por ciclo + servicio de dispatch.
- Existe evidencia de manejo de `Fault/faultstring`, códigos retryables y mapeo de errores técnicos.
- Se observan pruebas unitarias robustas para `Proc_Transacciones` (parser y orquestación inbound).
- Se observa brecha de pruebas explícitas para parser/job de `Proc_Contrapartidas` dentro del proyecto de tests actual.

---

## 2) Relación con el documento SOAP previo

Documento base relacionado:

- `docs/api/scalar-integraciones-soap-proc-transacciones-proc-contrapartidas-2026-04-26.md`

Relación entre ambos documentos:

- El documento previo describe arquitectura funcional, mapa REST → SOAP y consideraciones operativas.
- Este documento agrega **revalidación técnica verificable** del estado real del código y de la cobertura de pruebas observada en repositorio.
- Este documento no reemplaza el anterior; lo complementa con foco de QA técnico y cierre de brechas.

---

## 3) Compilación real o bloqueo técnico

### 3.1 Comando ejecutado

```bash
dotnet build ACHInterbank.sln -c Release
```

### 3.2 Resultado

- **Bloqueo técnico de entorno:** el contenedor no tiene `dotnet` instalado o disponible en `PATH`.
- Salida observada:

```text
/bin/bash: line 1: dotnet: command not found
```

### 3.3 Implicación de control

- No es válido declarar cierre técnico de SOAP-1B basado en compilación en este entorno.
- Para cierre formal se requiere rerun de build en entorno con SDK .NET habilitado.

---

## 4) Pruebas relacionadas encontradas

Se inspeccionó `tests/Cfa.ACHInterbank.Tests` para ubicar cobertura sobre parser, mapper, orquestador y job/handler.

### 4.1 Evidencia localizada

- `ProcTransaccionesResponseParserTests`.
- `IncomingNachaPostProcessingOrchestratorTests`.
- `ProcTransaccionesRequestMapperTests`.
- `IntegrationMappingEndToEndTests` (incluye evidencia de `WSCFAACH.Proc_Contrapartidas` a nivel de mapping).
- `ContrapartidaDispatchPersistenceServiceTests`.

### 4.2 Cobertura no encontrada como suite dedicada

- No se encontró archivo de pruebas dedicado con nombre de clase para:
  - `ProcContrapartidasResponseParserTests`.
  - `ContrapartidaDispatchJobServiceTests`.
  - `AchContrapartidasByCycleHandlerTests`.

---

## 5) Pruebas ejecutadas o no ejecutadas

### 5.1 Ejecutadas

- Inspección estática con `rg` sobre código fuente y tests.

### 5.2 No ejecutadas por bloqueo técnico

- `dotnet build ACHInterbank.sln -c Release` (falló por ausencia de SDK).
- `dotnet test ...` (no ejecutable en este entorno por la misma causa).

### 5.3 Decisión de calidad

- Estado de pruebas en esta revalidación: **parcial y documental** (sin ejecución de test runner .NET).
- Se requiere corrida posterior en entorno con SDK para completar evidencia de cierre.

---

## 6) Cobertura actual de `Proc_Transacciones`

### 6.1 Cobertura observable (revisión de repositorio)

| Componente | Cobertura observada | Estado |
|---|---|---|
| `ProcTransaccionesRequestMapper` | Pruebas identificadas | Cubierto en nivel unitario/mapping |
| `ProcTransaccionesResponseParser` | Pruebas identificadas, incluye casos de éxito, rechazo y `Fault` | Cubierto en nivel parser |
| `IncomingNachaPostProcessingOrchestrator` | Pruebas identificadas con escenarios de respuesta SOAP y transición de estado | Cobertura significativa |
| `IWscfaachSoapClient` real | No aplica como integración real en unit tests | Requiere pruebas de contrato externas |

### 6.2 Riesgo residual

- Falta evidencia de prueba de contrato extremo a extremo contra proveedor SOAP en ambiente de integración controlado.

---

## 7) Cobertura actual de `Proc_Contrapartidas`

### 7.1 Cobertura observable (revisión de repositorio)

| Componente | Cobertura observada | Estado |
|---|---|---|
| `ProcContrapartidasRequestMapper` | Evidencia indirecta por pruebas de mapping E2E | Parcial |
| `ProcContrapartidasResponseParser` | Implementación encontrada; sin suite dedicada explícita detectada | Brecha |
| `ContrapartidaDispatchJobService` | Referenciado en arquitectura/flujo; sin suite dedicada explícita detectada | Brecha |
| `AchContrapartidasByCycleHandler` | Implementación encontrada; sin suite dedicada explícita detectada | Brecha |

### 7.2 Riesgo residual

- La rama `Proc_Contrapartidas` presenta mayor riesgo de regresión funcional/técnica por menor evidencia automatizada directa.

---

## 8) Brechas de pruebas

1. Ausencia de suite dedicada para `ProcContrapartidasResponseParser` con matriz de respuestas globales e itemizadas.
2. Ausencia de suite dedicada para `ContrapartidaDispatchJobService` en escenarios de lote mixto (éxito/parcial/falla).
3. Ausencia de suite dedicada para `AchContrapartidasByCycleHandler` orientada a ventanas activas/inactivas y consolidación de métricas.
4. Sin prueba de contrato SOAP externa para validar compatibilidad de envelope y semántica de `faultcode/faultstring`.
5. Sin evidencia de build/test en este entorno por bloqueo de SDK .NET.

---

## 9) Matriz de pruebas faltantes (accionable)

| ID | Flujo | Tipo de prueba | Escenario mínimo | Resultado esperado | Prioridad |
|---|---|---|---|---|---|
| SOAP1B-T01 | `Proc_Contrapartidas` parser | Unit | Respuesta con `ANSST=00` y múltiples items | `IsSuccess=true`, items exitosos consistentes | Alta |
| SOAP1B-T02 | `Proc_Contrapartidas` parser | Unit | Rechazo funcional (`ANSST`/`ANCLC` no exitoso) | `IsSuccess=false`, `IsRetryable=false`, código funcional correcto | Alta |
| SOAP1B-T03 | `Proc_Contrapartidas` parser | Unit | `Fault` técnico retryable (`timeout`, `temporarily unavailable`) | `IsSoapFault=true`, `IsRetryable=true` | Alta |
| SOAP1B-T04 | `Proc_Contrapartidas` parser | Unit | `Fault` no retryable por mensaje/código | `IsSoapFault=true`, `IsRetryable=false` | Media |
| SOAP1B-T05 | `ContrapartidaDispatchJobService` | Unit | Batch mixto: 1 éxito, 1 rechazo funcional, 1 fault técnico | Persistencia de estados por item + conteos correctos | Alta |
| SOAP1B-T06 | `ContrapartidaDispatchJobService` | Unit | Reintento tras fault técnico | Incremento de intentos + reprogramación esperada | Alta |
| SOAP1B-T07 | `AchContrapartidasByCycleHandler` | Unit | Sin ciclos activos | No procesa lotes, resumen estable | Media |
| SOAP1B-T08 | `AchContrapartidasByCycleHandler` | Unit | Varios ciclos activos con resultados heterogéneos | Resumen agregado correcto (`Processed/Success/Failed/Partial`) | Alta |
| SOAP1B-T09 | REST→Job→SOAP contrapartidas | Integration (interna) | Trigger de tarea y consumo de cola con datos controlados | Invocación esperada de mapper/cliente y transición de estados | Alta |
| SOAP1B-T10 | Contrato SOAP externo | Contract | Request/response sin datos sensibles en ambiente de integración | Compatibilidad envelope + parseo de códigos/fault | Alta |

---

## 10) Plan recomendado de cierre

### 10.1 Fase inmediata (bloqueante)

1. Habilitar entorno con SDK .NET (misma versión objetivo del repositorio).
2. Ejecutar y evidenciar:
   - `dotnet build ACHInterbank.sln -c Release`
   - `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release`

### 10.2 Fase de cobertura técnica

3. Implementar pruebas faltantes `SOAP1B-T01` a `SOAP1B-T08`.
4. Ejecutar pruebas de integración internas `SOAP1B-T09`.
5. Coordinar prueba de contrato externa `SOAP1B-T10` con datos sanitizados y sin credenciales reales.

### 10.3 Criterios mínimos para declarar cierre SOAP-1B

- Build Release exitoso.
- Suite de tests backend ejecutada sin fallas críticas.
- Matriz `SOAP1B-T01`…`SOAP1B-T10` cerrada o con excepciones formalmente aceptadas.
- Evidencia documental actualizada en `docs/api` sin exposición de secretos/URLs sensibles.

---

## 11) Conclusión de revalidación

- La implementación de integración SOAP para `Proc_Transacciones` muestra madurez de cobertura superior respecto a `Proc_Contrapartidas`.
- Para `Proc_Contrapartidas`, la cobertura técnica actual es parcial y requiere completar suites dedicadas de parser/job/handler y contrato.
- En esta corrida **no procede declarar cierre técnico final** por bloqueo de compilación y ausencia de ejecución de pruebas .NET en el entorno disponible.
