# Manifest de fuentes normativas

**Verificación:** 2026-09-29  
**Propósito:** identificar las copias normativas incluidas en el repositorio y evitar que una copia histórica sea tomada accidentalmente como autoridad vigente.

## Copias verificadas contra los archivos entregados en Sources

Los siguientes archivos del repositorio fueron comparados byte a byte mediante SHA-256 con los archivos fuente entregados para esta revisión. Los hashes coinciden exactamente.

| Fuente | Ruta en repositorio | SHA-256 | Estado |
|---|---|---|---|
| ACH Colombia V35, abril 2026 | `docs/normativa/md/ACH-Colombia-V35.md` | `dcac71b23bb047bfa280c4351c62415574961ca5c254d478e3b0ad8b86e59083` | Exacta |
| CENIT DSP-152 Anexo 2 | `docs/normativa/md/CENIT-DSP-152-Anexo-2.md` | `abf9862df69b2b62d9e43e8b9b0d1a5c9d864a759733eab763760e383ba15f5a` | Exacta |
| CENIT Anexo A — causales de devolución | `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md` | `437340a160fd84bf40669656140b4cefc867074e17821c7b7f9a7ac944a2ecfb` | Exacta |
| CENIT Anexo B — causales de rechazo | `docs/normativa/md/CENIT-Anexo-B-Causales-Rechazo.md` | `e150b86378d6ed0a2a31a08336985f1b3490f551e80982f709c3256b486adbfd` | Exacta |
| CENIT Manual de Usuario | `docs/normativa/md/Archivos de CENIT/Manual de Usuario CENIT.md` | `6c975042120d731588c7b92b2cb2ddcaa53f3bb97d6dcc10ad676d1ca808dc6c` | Exacta |
| CENIT Manual de Especificaciones NACHA-M, 7 mayo 2026 | `docs/normativa/md/Archivos de CENIT/Manual de Especificaciones Formato NACHA-M CENIT.md` | `617e7f089a7544a1838a2cf7cf6f07df5e3517fedfbc2679c9e8c79b56cd735b` | Exacta |
| CEOS DSP-152 27 mayo 2022 | `docs/normativa/md/ceos_dsp_152_MAY_27_2022.md` | `96d10a84d8946fedbed371205cadb9f2b0771d4f42699133accf9ffcfe869d07` | Exacta / histórica según alcance |

## Autoridad de implementación aceptada

- **ACH Colombia:** V35, abril de 2026.
- **CENIT NACHA-M ordinario:** manual de 7 de mayo de 2026 y documentos CENIT vigentes/aplicables.
- `ACH-Colombia-V32.md` se conserva para trazabilidad histórica y pruebas de no-regresión; no es la autoridad vigente para nuevas decisiones ordinarias.
- Una eventual V36 requiere evaluación y decisión explícita antes de reemplazar V35 como baseline.

## Regla de uso

Antes de modificar ciclos, NACHA-M, causales, cardinalidad, nombres de archivo, MFT, devoluciones o reglas de una cámara:

1. identificar cámara y versión aplicable;
2. citar la sección normativa exacta;
3. contrastar contra `docs/CURRENT_PROJECT_STATUS.md` y `MEMORY.md`;
4. no inferir reglas de ACH Colombia a partir de CENIT ni viceversa;
5. conservar snapshots/perfiles publicados históricos; una nueva regla debe entrar por versionamiento, no por mutación silenciosa.
