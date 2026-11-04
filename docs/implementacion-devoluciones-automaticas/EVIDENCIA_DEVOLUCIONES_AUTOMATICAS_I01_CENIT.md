# Evidencia de I01-CENIT — ejecución vs. pendientes

**Fecha:** 2026-11-02. **Origen:** `ACHInterbank_COMPLETO_DEVOLUCIONES_AUTOMATICAS_I08_ACHCOLOMBIA_2026-11-02.zip`. SHA-256 `008a19c8e9e62e5dfaf274983433bdf50a2c4e24789078eddcb0b5247bc15317`.

## Comprobaciones realmente ejecutadas

| Comprobación | Resultado | Interpretación |
|---|---|---|
| `unzip -t` sobre ZIP base | **PASS**, sin errores | Integridad de datos comprimidos, no ejecución funcional |
| `python3 scripts/test/automatic_returns_i01_cenit_static_check.py` | **PASS 29/29** | Análisis estático de fuente/contratos, no compilación |
| Regresión ACH I02 | **18/18** | Chequeo estático heredado |
| Regresión ACH I03 | **25/25** | Chequeo estático heredado |
| Regresión ACH I04 | **32/35, NO aprobado** | Chequeo histórico incompatible con ampliaciones posteriores, situación preservada desde I08 |
| Regresión ACH I05 | **33/33** | Chequeo estático heredado |
| Regresión ACH I06 | **28/28** | Chequeo estático heredado |
| Regresión ACH I07 | **24/24** | Chequeo estático heredado |
| Regresión ACH I08 | **25/25** | Chequeo estático heredado |
| Comparación SHA-256 por ruta con el ZIP base | **4860/4860 presentes: 4858 idénticos, 2 cambios de infraestructura, 0 eliminados** | Garantía de conservación en disco y ZIP, no runtime |
| Integridad y SHA-256 del ZIP I01 | Archivo `.sha256` externo | Entrega íntegra verificable |

## Pruebas añadidas al proyecto, NO ejecutadas

Archivo `tests/Cfa.ACHInterbank.Tests/CenitAutomaticReturnSettingsI01Tests.cs`, con casos: desactivado inicial; GET con `ProcessingConnected=false`; transición a `CON_APROBACION`, `AUTOMATICO` y regreso; persistencia entre contextos; auditoría EF antes/después; rechazo de valores de modo y anterior inválidos; conflicto 409 por actualización obsoleta; token de concurrencia de EF con dos contextos; configuración propia ausente/compartida; no afectación ACH Colombia; no escritura al no cambiar modo; políticas HTTP y 400/409 del controlador.

### Procedimiento de ejecución posterior

```bash
# Compilar desde la raíz ACHInterbank_ajustado (requiere SDK .NET 10)
dotnet build ACHInterbank.sln --no-restore
# Previamente restaurar paquetes si el entorno no dispone del caché correspondiente.
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  --filter FullyQualifiedName~CenitAutomaticReturnSettingsI01Tests
# Regresión ACH I01 de configuración, independiente:
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  --filter FullyQualifiedName~AchColombiaAutomaticReturnSettingsI01Tests
```

Para la prueba relacional, aplicar migraciones en BD de ensayo aislada mediante el mecanismo real de despliegue del repositorio, comprobar que cada cámara conserva su fila/valor, ejecutar GET/PUT autenticados con/ sin `P1.ConfigManage`, concurrencia desde dos clientes, comprobar `AuditLogs.ChangedBy`/fechas, restauración tras conflicto, regresión del flujo **manual** de CENIT y de ACH Colombia. No ejecutar en producción hasta aprobación de controles de cambios y respaldo. Repetir con SQL Server y PostgreSQL, en especial CHECK y condiciones de concurrencia. Ejecutar la compilación SPA y pruebas operativas únicamente si una fase posterior modifica interfaz.

## No ejecutado / no certificado

**NO EJECUTADO:** `dotnet build`, xUnit, `dotnet test`, migraciones contra motores reales, HTTP autenticado, concurrencia relacional, auditoría con usuario real, regresión manual end-to-end, Angular/Karma, NACHA-M golden files, integración bancaria, homologación CENIT. **NO-GO productivo.**

La ausencia del SDK y de bancos de prueba no transformó los controles estáticos en éxitos funcionales. El código existente fuera del alcance se contrasta con hashes exactos en el manifiesto.
