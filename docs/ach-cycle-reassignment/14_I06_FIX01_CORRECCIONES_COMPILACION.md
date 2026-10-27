# ACH-CR-001 — I06-FIX01: corrección focal de tres errores C#

**Fecha:** 2026-10-26  
**Entrada:** `ACHInterbank_ACH_CR_I06_COMPLETO.zip`  
**Salida:** `ACHInterbank_ACH_CR_I06_FIX01_COMPLETO.zip`  
**Naturaleza:** parche posterior a I06 para errores de compilación reportados por el usuario; **no es un nuevo incremento funcional**.

## 1. Errores reportados y resolución

| Diagnóstico | Ruta relativa a `ACHInterbank_ajustado/` | Causa verificada en fuente | Intervención mínima |
|---|---|---|---|
| `CS1929` | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchJobService.cs` | `transactionIds` es `int[]` (IDs de `AchTransaction`), mientras `IncomingNachaDispatchQueue.AchTransactionId` es `int?`. | La condición LINQ exige `q.AchTransactionId.HasValue` antes de `transactionIds.Contains(q.AchTransactionId.Value)`. |
| `CS0103` | `src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchPendingCycleReassignmentHandler.cs` | `OperationalCycleWindowStatus` está declarado en `Cfa.ACHInterbank.Application.ACH.Models`, no importado en este archivo. | Agregar `using Cfa.ACHInterbank.Application.ACH.Models;`. |
| `CS0165` | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaAdmissionValidator.cs` | La expresión con `||` puede omitir `TryValidate` y su argumento `out var correlationError`, quedando sin asignar en el mensaje de rechazo. | Declarar `string? correlationError = null;` antes del `if` y usar `out correlationError`. |

## 2. Justificación de seguridad y alcance

- `AchTransactionId` es nullable porque la cola también soporta la ruta ACH Colombia basada en archivo. La condición `HasValue` respeta esta distinción y evita admitir registros sin transacción ACH asociada.
- El `using` únicamente hace visible un tipo existente; no afecta políticas de sesión ni la doble barrera de activación de Quartz.
- La inicialización de `correlationError` no elimina la validación ni sus rechazos: cuando falta `parsedAchColName`, el mensaje sigue usando la explicación genérica de nombre inválido; cuando `TryValidate` retorna false, conserva la causal concreta que devuelve el validador.
- **Sin** cambios a `Application`, `Domain`, contratos, migraciones, consultas externas, NACHA golden files, motores CENIT, configuración de activación o política de fecha valor.
- Se respetaron los saltos de línea originales de los archivos `.cs` para mantener un diff pequeño y revisable.

## 3. Validación y evidencia

- Verificación textual/tipológica: `scripts/validate-cycle-reassignment-i06-fix01-static.py` comprueba los tres cambios y los tipos fuente (`int[]`/`int?`, namespace del enum y `out string?`).
- El validador histórico `validate-cycle-reassignment-i06-static.py` admite ahora el nombre de ZIP **I06-FIX01 solamente si** `MEMORY_STATE` contiene marcador y evidencia del parche; evita un falso fallo del nombre sin aceptar entregas arbitrarias.
- Validaciones heredadas I01–I06 y grafo de proyectos (`scripts/validate-solution-projects.py`) se ejecutaron en el árbol del paquete corregido.
- Validación de ZIP: comparar con I06 por ruta, tamaño y CRC; únicamente pueden diferir los tres `.cs`, la documentación de estado y los nuevos artefactos de este parche.
- **No disponible en este entorno:** SDK `dotnet`, compilación del ensamblado Persistence y ejecución de tests .NET. Los tres diagnósticos quedaron corregidos en texto fuente, **sin afirmar aún compilación exitosa de la solución**.

## 4. Verificación recomendada en Windows (CMD)

En la raíz `ACHInterbank_ajustado` del ZIP corregido:

```cmd
python scripts\validate-solution-projects.py
python scripts\validate-cycle-reassignment-i06-fix01-static.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests\Cfa.ACHInterbank.Tests\Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Si el build revela otros diagnósticos, conservar código y salida de Visual Studio como evidencia para un nuevo parche puntual. **No** habilitar Quartz ni clasificar la iniciativa como `GO` por el solo hecho de compilar.

## 5. Continuidad

- Archivo canónico: `docs/ach-cycle-reassignment/MEMORY_STATE.md`.
- Incrementos I00–I06: **7 de 7**, **0 pendientes**.
- Estado de homologación: **pendiente**; `PRODUCTION_STATE: NOT_READY`.
- Próximo objetivo externo: build real, tests y plan `13_PLAN_HOMOLOGACION_GO_NO_GO.md`.
