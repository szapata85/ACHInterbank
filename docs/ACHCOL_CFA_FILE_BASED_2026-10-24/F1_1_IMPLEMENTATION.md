# F1.1 — Parser de nombre físico entrante ACH Colombia

- **Programa:** ACHCOL-NACHA-CFA-EXTCYCLE-001
- **Fecha:** 2026-10-24
- **Estado:** `IMPLEMENTED_UNVERIFIED` (código real incluido, SDK .NET 10 no disponible, pruebas escritas no ejecutadas).
- **Fuente:** ZIP unificado F0.3.A; se conservan los demás archivos del proyecto y los estados Git originales.

## Cambios aplicados

1. `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ExternalFileNames/AchColInboundFileNameParser.cs` — nuevo parser interno sin dependencias de base de datos, ciclos ni servicios CFA.
2. `tests/Cfa.ACHInterbank.Tests/AchColInboundFileNameParserTests.cs` — 29 filas xUnit `[InlineData]` más dos casos `[Fact]`.
3. Se actualizaron `PLAN.md`, `MEMORY_STATE.md`, `CHATGPT_MICRO_START.md`, `README_PAQUETE_UNIFICADO.md`.

## Contrato de nombre y reglas

Gramática física soportada por el nuevo parser:

```
RRRRTTT.ZZZ.yyyyMMdd.N.OUT
RRRRTTT.ZZZ.yyyyMMdd.N.OUT.env
```

- Código origen/tránsito: exactamente siete caracteres numéricos ASCII.
- `ZZZ`: tres dígitos ASCII y secuencia 001–036 conforme al mapeo ACH Colombia V35.
- Fecha: ocho dígitos y fecha real (`yyyyMMdd`, cultura invariante).
- `N`: entero decimal positivo sin ceros iniciales, sin límite 7; validación de overflow `Int32`. El número **NO** identifica `AchCycleId` ni se fuerza a igualar `ZZZ`.
- `.OUT`: `ProtectionKind=PlainNacha`; `.OUT.env`: `DigitalEnvelope`; extensiones comparadas case-insensitively. El nombre canónico remueve **solo** el `.env` final, conservando el resto y su casing.
- La entrada debe ser un **nombre base** único, no ruta; no recorta directorios, rechaza barras/`:`/controles, tokens extra, extensiones no admitidas y longitud mayor a 128 caracteres. Errores de validación con códigos estables, sin echo de datos sensibles.
- El parser **no** descifra ni valida firmas: la ruta ya existente de sobre digital se preserva y se enlazará en F1.2.

## Ejemplos de aceptación

| Nombre | ZZZ | N | Forma |
|---|---:|---:|---|
| `0001283.008.20260731.8.OUT` | 8 | 8 | Texto plano |
| `0001283.009.20260731.9.OUT.env` | 9 | 9 | Cifrado |
| `0001283.008.20260731.10.OUT.env` | 8 | 10 | Cifrado, ZZZ distinto de N |
| `0001283.036.20260228.123.OUT` | 36 | 123 | Texto plano |

## Riesgos y límites deliberados

- **Sin activación del camino de negocio:** este incremento crea el parser y pruebas, pero aún no se lo inyecta en ingesta/validador ni se modifica el envío SOAP. Esto evita rechazos a archivos existentes de otros perfiles o habilitar prematuramente una ruta transaccional sin idempotencia y trazabilidad completa. **F1.2** es responsable del cableado inicial y descubrimiento `.OUT` en Managed MFT/SFTP. **F2–F5** desacoplan de `AchCycle` y completan CFA.
- No se modificó el parser CENIT ni flujo RET, ni `ExternalFileNameSupport` (outbound).
- Si otros perfiles ACHCOL poseen formas físicas no reflejadas en los ejemplos aportados, deben añadirse mediante perfil explícito, no relajando globalmente esta gramática.
- El contrato manual ACH Colombia V35 habla de otros nombres en contexto outbound; los sufijos `.8/.9` de esta ruta proceden del requerimiento funcional del usuario.

## Verificación disponible y deuda

- **Efectuado:** comparación de archivos con ZIP base, revisión de nombres de namespace y `InternalsVisibleTo(Cfa.ACHInterbank.Tests)`; revisión estática de la sintaxis y de los casos de prueba.
- **No realizado:** `dotnet build`, `dotnet test` y pruebas de integración/ambiente. .NET SDK 10 no está instalado en este entorno. Los 31 tests están **definidos**, no aprobados.
- **Pendiente F1.2:** integración física `.OUT`/`.OUT.env`, seguridad de sobre, correlación encabezado/ZZZ y regresión MFT/SFTP.
- **Pendiente F6:** compilación/CI completa si sigue sin SDK disponible, pruebas CFA, SQL Server/Postgres, tolerancia a errores, seguridad y UAT final.

## Comandos para verificar en un entorno .NET 10

```sh
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter FullyQualifiedName~AchColInboundFileNameParserTests
```

No se ejecutó este comando en la presente sesión.

## Huellas de los nuevos fuentes

- `AchColInboundFileNameParser.cs` — SHA-256 `eeb0fd627f734b97b81f0f756bdf8c7706a0f32b7bc344c248ab88be7b08b8d0`.
- `AchColInboundFileNameParserTests.cs` — SHA-256 `086132db844b9d8097fc5bbaae191af8902b78ac16dade1dd46745012aafaa63`.

Las huellas se obtuvieron directamente de los archivos entregados; no constituyen evidencia de compilación.
