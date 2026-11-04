# F1.2 — Integración de archivo físico ACH Colombia y encabezado NACHA-M

**Programa:** ACHCOL-NACHA-CFA-EXTCYCLE-001  
**Fecha:** 2026-10-24  
**Estado:** `IMPLEMENTED_UNVERIFIED` — cambios C# incorporados, compilación y tests no ejecutados (SDK .NET 10 no disponible).

## Implementaciones efectivas

| Archivo | Modificación |
|---|---|
| `IncomingNachaIngestionAppService.cs` | Correlaciona nombre ACHCOL `.OUT[.env]` con registro tipo 1 **después** del descifrado existente y antes del bloqueo del resolver de ciclos; registra evidencia o rechazo estable. |
| `ExternalFileNames/AchColInboundHeaderCorrelation.cs` | Correlación pura para `ZZZ ↔ FileIdModifier`, fecha `yyyyMMdd` y nombre canónico post-descifrado; `N` no se iguala a `ZZZ` ni a `AchCycleId`. |
| `AchColombiaManagedMftFolderAdapter.cs` | Descubre también `.OUT` en claro (conserva `.OUT.env` y `.RET.env`). |
| `AchColombiaSftpMftAdapter.cs` | Descubre/reclama/recupera `.OUT` en claro (conserva sobres y exclusión de archivos temporales). |
| `AchColombiaManagedMftImplementationSupport.cs` | Lista permitida de nombres inbound finales `.OUT`, `.OUT.env` y `.RET.env`. |
| `AchColombiaManagedMftInboundWorkflow.cs` | Aplica compuerta de extensiones para los tres tipos; sin cambios de contrato de adaptadores. |
| `AchColInboundHeaderCorrelationTests.cs` | Casos H/008/.8, I/009/.9, H/008/.10, 036/9, rechazos de fecha/identificador/canónico y encabezado ausente. |
| `AchColombiaManagedMftInboundNameTests.cs` | Compromisos explícitos de extensiones permitidas y temporales/RET plano excluidos. |
| `AchColombiaManagedMftFolderAdapterTests.cs`, `AchColombiaSftpMftAdapterTests.cs` | Cobertura de descubrimiento `.OUT` case-insensitive, preservación de `.env` y recuperación de claim plano. |

## Orden y seguridad de proceso

```text
Frontera CFA MFT/SFTP (.OUT/.OUT.env/.RET.env)
  → claim exclusivo / hash y duplicidad existentes
  → ingesta (retiene archivo original)
     → si .env: descifrado/verificación con servicio vigente, sin fallback
     → ChunkFixed106 del contenido en claro
     → resolución cámara/fecha (actual, aún dependiente de ciclo)
     → [solo ACHCOL OUT] comprobar nombre físico/canónico + fecha + id de archivo en R1
     → evidencia o rechazo controlado
     → resolución/admisión basada en ciclo existente (pendiente F2)
     → parseo posterior (cuando las demás reglas admitan)
```

**No se ha modificado:** semántica de otros flujos RET o CENIT, `AchCycle` y su persistencia, mapas SOAP, cliente CFA ni feature flags. Esta entrega no produce despacho monetario nuevo de forma automática.

## Invariantes

- Identificador tipo-1 en posición 36: `A` para 001, `H` para 008, `I` para 009, `9` para 036.
- Fecha del nombre debe coincidir con fecha del encabezado; nombre canónico del descifrado debe coincidir exactamente salvo casing.
- `N=8/9/10/...` es **ciclo externo**, no `AchCycleId` ni secuencia `ZZZ`.
- El servicio de sobre digital existente valida criptografía y jamás interpreta ciphertext como NACHA-M.
- Los adaptadores solo descubren nombres con terminaciones finales admitidas; no se reinterpreta `.RET` en claro.
- Diferencias generan códigos `ACHCOL_INBOUND_*` y no crean identidades operativas ficticias.

## Verificación y límites

- **Revisión estática realizada:** paths y namespaces, diff acotado, recuento de llaves y semántica de extractores, rutas claim/archivo existentes; prueba de integridad y conservación ZIP en empaquetado.
- **No ejecutado:** `dotnet build`, `dotnet test`, DB, MFT/SFTP reales, descifrado con certificado real, integración CFA/Live, UAT. **No declaramos aprobación de esas pruebas.**
- **Pendiente conocido:** el resolver/admisión actual aún requiere un `AchCycle` configurado; se aborda en F2.1/F2.2 sin detener el desarrollo. La homologación física con el receptor CFA se realizará antes de habilitar Live.

## Comandos para ejecutar cuando exista SDK .NET 10

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter "FullyQualifiedName~AchColInboundFileNameParserTests|FullyQualifiedName~AchColInboundHeaderCorrelationTests|FullyQualifiedName~AchColombiaManagedMftInboundNameTests|FullyQualifiedName~AchColombiaManagedMftFolderAdapterTests|FullyQualifiedName~AchColombiaSftpMftAdapterTests"
```

**Este comando se documenta; no se ejecutó.**
