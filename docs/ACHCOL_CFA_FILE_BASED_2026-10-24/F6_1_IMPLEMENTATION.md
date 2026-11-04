# F6.1 — Auditoría técnica y regresión offline (2026-10-25)

**Estado:** `IMPLEMENTED_UNVERIFIED` — parte estática ejecutada y corrección focal incorporada; **compilación, xUnit, migraciones e integración CFA todavía NO ejecutadas**. No se declara F6.1 certificada. Base: `ACHInterbank_ACHCOL_CFA_F5_3_UNIFICADO.zip`. Entrega: `ACHInterbank_ACHCOL_CFA_F6_1_UNIFICADO.zip`.

## Evidencia efectivamente ejecutada

| Comando / control | Resultado comprobado | Alcance real |
| --- | --- | --- |
| `ZipFile.testzip()` del proyecto F5.3 | PASS; 8.124 entradas | CRC/lectura del archivo fuente ZIP, no semántica C# |
| `python3 scripts/validate-solution-projects.py` | PASS: 7 proyectos; 7 referencias de proyectos resueltas | Grafo de solución, no compilación |
| `python3 scripts/validate-achcol-f61-offline.py` | **PASS 12/12** | Auditoría determinista de presencia de fuentes, esquema, flags, bloqueos y tests; **no ejecuta C#** |
| `python3 scripts/validate-r5d-inbound-postprocessing.py` | PASS | Ratchet estático del pipeline histórico R5D |
| `python3 scripts/validate-nav3-4j-digital-envelope.py` | PASS: 145 aserciones estáticas | Revisión estática de código sobre digital; no descifra archivos reales |
| `python3 scripts/validate-r5c-inbound-persistence.py` | **FAIL:** `R5B inbound validation components changed during R5C` | Control histórico por SHA fija; incompatible con las modificaciones actuales. No equivale a defecto de compilación ni autoriza ignorarlo en CI. Requiere actualizar el baseline con revisión independiente. |
| `scripts/validate-r7-returns-separation.py` | **NO CONCLUYÓ** por límite de tiempo del entorno | No atribuir éxito o fallo funcional. |
| `dotnet --info`, `dotnet build`, `dotnet test` | **NO EJECUTABLES** | `dotnet` ausente; `global.json` fija SDK `10.0.402`; red del contenedor sin resolución de `dot.net`. |
| PostgreSQL/SQL Server y CFA | **NO EJECUTADOS** | Sin contenedores Docker, servidores o credenciales; no hubo envío SOAP. |

## Corrección focal de seguridad F6.1

- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColProcTransaccionesDryRunContractCheck.cs`: se rechazan atributos no autorizados en el cuerpo SOAP, atributos inesperados en `Envelope`, texto libre, CDATA o instrucciones ajenas a los campos aceptados en `Body` y `Proc_Transacciones`. Sigue prohibiendo DTD/entidades externas y mantiene el XML de ejemplo como **no transmisible**.
- `tests/Cfa.ACHInterbank.Tests/AchColProcTransaccionesContractReadinessF53Tests.cs`: test de regresión para atributo inyectado en `Body` y texto fuera de los parámetros. **Escrito, aún no compilado/ejecutado**.
- `scripts/validate-achcol-f61-offline.py`: una auditoría estática reproducible de 12 chequeos sin nuevas dependencias, configuración, base de datos, colas, endpoints o servicios. Se ejecutó exitosamente.

## Cierre de alcance, deuda y gates pendientes

No se alteraron modelos, migraciones, parser, planner, clasificador, worker, control de idempotencia o cliente SOAP en F6.1. Los cambios ejecutables quedan confinados al **inspector de vista previa DryRun** y sus pruebas. El feature flag de ACHCOL no se habilitó y `CanTransmit=false` persiste.

**Puerta pendiente obligatoria** al contar con .NET 10.0.402 y NuGet:

```bash
python3 scripts/validate-solution-projects.py
python3 scripts/validate-achcol-f61-offline.py
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore --maxcpucount:1
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~AchCol|FullyQualifiedName~IncomingNacha|FullyQualifiedName~NachaInbound' --logger 'trx;LogFileName=achcol-f61.trx'
```

Las migraciones F2.3/F3.1/F3.2/F3.3 y la concurrencia requieren ejecución controlada contra **ambos** motores, con esquema aislado y datos sintéticos. El filtrado focal indicado no sustituye la suite total ni el comando CI existente en `.github/workflows/dotnet-ci.yml`. Repetir los ratchets existentes, investigar el fallo histórico R5C y registrar el resultado de R7. La homologación XSD `xsd0/xsd1`, identificadores CFA y pruebas E2E/UAT siguen pendientes y **no permiten activar Live**.

## Decisiones F6.1

- **DEC-076:** Mantener la fase F6 en el mismo flujo incremental y preservar un ZIP completo después de cada entrega, con evidencia real y restricciones explícitas.
- **DEC-077:** La falta de SDK, motores y XSD no transforma comprobaciones estáticas en pruebas ejecutadas. Estado `IMPLEMENTED_UNVERIFIED` y deuda prioritaria visible.
- **DEC-078:** Endurecer exclusivamente el inspector SOAP DryRun ante contenido inesperado; no introducir otro sistema de pruebas/worker ni habilitar transmisiones.
- **DEC-079:** No retocar a ciegas el baseline histórico de SHA R5C para forzar verde; la divergencia debe revisarse y documentarse al ejecutar CI.
- **DEC-080:** Continuar F6.2 con operación/observabilidad/plan de despliegue, **sin considerar cerrada la certificación F6.1**. Reejecutar las pruebas cuando exista un entorno válido.

## Archivos modificados/adicionados

1. Modificado: `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColProcTransaccionesDryRunContractCheck.cs`.
2. Modificado: `tests/Cfa.ACHInterbank.Tests/AchColProcTransaccionesContractReadinessF53Tests.cs`.
3. Nuevo: `scripts/validate-achcol-f61-offline.py`.
4. Documentación: `F6_1_IMPLEMENTATION.md`, `PLAN.md`, `MEMORY_STATE.md`, `CHATGPT_MICRO_START.md`, `README_PAQUETE_UNIFICADO.md`.

**No hubo commit Git:** el ZIP conserva el snapshot `.git` proporcionado; ver diferencias de la línea base en `F0_1_BASELINE.md`. Las pruebas xUnit del código nuevo **no** se declararon aprobadas.
