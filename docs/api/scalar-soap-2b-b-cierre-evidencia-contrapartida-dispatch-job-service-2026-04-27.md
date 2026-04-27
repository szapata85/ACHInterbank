# SOAP-2B-B — Cierre de evidencia técnica de `ContrapartidaDispatchJobService`

**Fecha:** 2026-04-27  
**Ámbito:** evidencia de compilación y pruebas automatizadas para el flujo de despacho de contrapartidas (`Proc_Contrapartidas`) ejecutado por `ContrapartidaDispatchJobService`.

---

## 1) Objetivo de cierre

Consolidar evidencia verificable de que la cobertura de pruebas para `ContrapartidaDispatchJobService` fue ampliada y ejecutada, dejando trazabilidad de:

- compilación del repositorio;
- pruebas específicas del job service;
- pruebas complementarias del parser de `Proc_Contrapartidas`;
- riesgos residuales y próximos pasos de QA.

---

## 2) Comandos ejecutados (obligatorios + validación)

```bash
bash scripts/codex/setup-codex-env.sh
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH

dotnet --info
dotnet build ACHInterbank.sln -c Release

dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter ContrapartidaDispatchJobServiceTests
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter ProcContrapartidasResponseParserTests
```

---

## 3) Resultado de compilación

- **Resultado:** exitoso.
- **Errores:** `0`.
- **Warnings:** `9` (nulabilidad preexistente, fuera del alcance de SOAP-2B-B).
- **SDK/Runtimes observados:** .NET SDK `10.0.203`, Runtime `10.0.7`.

Conclusión de esta sección: no hay bloqueo de compilación para ejecutar cobertura de pruebas SOAP en este entorno.

---

## 4) Resultado de pruebas ejecutadas

### 4.1 `ContrapartidaDispatchJobServiceTests`

- **Estado:** Passed.
- **Totales:** `Passed: 4, Failed: 0, Skipped: 0`.

Escenarios cubiertos en esta suite:

1. ciclo sin ítems elegibles;
2. procesamiento exitoso y transición a `ReportedToContrapartida`;
3. respuesta retryable con transición a `RetryPending`;
4. resultado mixto por ítems con clasificación parcial y `CompletedWithErrors`.

### 4.2 `ProcContrapartidasResponseParserTests`

- **Estado:** Passed.
- **Totales:** `Passed: 6, Failed: 0, Skipped: 0`.

Escenarios cubiertos en esta suite:

1. respuesta vacía retryable;
2. éxito contractual con `ANSST/ANCLC`;
3. rechazo funcional;
4. `SOAP Fault` retryable;
5. `SOAP Fault` no retryable;
6. parseo por ítems (`TransactionResult`) con resultado mixto.

---

## 5) Evidencia de cierre funcional SOAP-2B-B

Con las corridas registradas, la evidencia de cierre para `ContrapartidaDispatchJobService` en esta fase queda sustentada en:

- compilación satisfactoria del repositorio;
- ejecución exitosa de la suite específica del job service;
- ejecución exitosa de la suite del parser que soporta la misma rama funcional;
- validación explícita de transiciones de estado críticas (`ReportedToContrapartida`, `RetryPending`, `ContrapartidaReportFailed`) y estado de batch (`Completed`, `Failed`, `CompletedWithErrors`).

---

## 6) Riesgos residuales y siguiente paso recomendado

### Riesgos residuales

1. Las pruebas ejecutadas son de nivel unitario/integración interna con `Sqlite` en memoria y mocks; no sustituyen prueba de contrato con proveedor SOAP externo.
2. Los warnings de nulabilidad no bloquean esta fase, pero deben ser gestionados en backlog técnico para reducir deuda.

### Siguiente paso recomendado

1. Ejecutar en CI/CD la suite completa:
   - `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release`
2. Programar prueba de contrato SOAP controlada (sin credenciales ni endpoints sensibles reales) para cierre de hardening operacional.

---

## 7) Veredicto de esta corrida

Para el alcance SOAP-2B-B solicitado en este bloque, la evidencia técnica queda **completada** en este entorno para:

- compilación;
- cobertura y ejecución de pruebas objetivo del job service de contrapartidas.
