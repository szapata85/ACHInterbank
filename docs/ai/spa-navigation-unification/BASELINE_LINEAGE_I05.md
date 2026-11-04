# Bloqueo de línea base — NAV-UX-MFT | I05

No se debe confundir un **ZIP completo** con un **incremento nuevo**: I04A conservó todo el código de I04 pero solo añadió protocolo documental. El código nuevo de este corte corresponde a I05.

## Cadena comprobada de artefactos disponibles

| Paso | Artefacto | SHA-256 comprobado | Delta |
| --- | --- | --- | --- |
| I04 | `ACHInterbank_NAV_UX_MFT_2026-10-27_PROYECTO_COMPLETO.zip` | `607e90b90ab5f71de61f9e66656679d9fe99b849dea1df844f38d3cf023c4cb0` | Cambios originales menú/MFT |
| I04A | `ACHInterbank_NAV_UX_MFT_2026-10-27_I04A_PROYECTO_COMPLETO.zip` | `94e6d0c03576b26a9ff34cd32a070679b6e49261f7facb2efe007aca8d2aabd0` | Dos documentos añadidos y cuatro actualizados; sin cambio de aplicación |
| I05 | `ACHInterbank_NAV_UX_MFT_2026-10-27_I05_PROYECTO_COMPLETO.zip` | Archivo lateral `*_I05_SHA256.txt` | Corrección de accesibilidad compacto + prueba + guarda de linaje |

**Límite de esta afirmación:** verifica únicamente los ZIP disponibles en esta conversación. No demuestra que no existan implementaciones posteriores alojadas en otro chat, rama, repositorio o dispositivo.

## Puerta de entrada del siguiente incremento

1. Solicitar o usar el ZIP **I05** y validar su SHA lateral; no sustituir por la base `SEC-I06A`, por `I04A` ni por `I04` para seguir el programa del menú.
2. Extraer el proyecto completo. Antes de modificar, leer `AGENTS.md`, `MEMORY.md`, `docs/ai/navigation-menu/MEMORY_STATE.md` y `docs/ai/spa-navigation-unification/MEMORY_STATE.md`.
3. Registrar `parent_artifact` y `parent_sha256` en la nueva memoria. Usar `scripts/verify-incremental-source-lineage.py` antes de empaquetar para detectar regresiones de fuentes; **esta herramienta no puede descubrir versiones no entregadas**.
4. Comparar archivos y declarar explícitamente si se trata de un incremento técnico, un incremento documental o solo pruebas. No computar dos veces los avances anteriores.
5. Si no se dispone del ZIP padre que afirma el estado, informar que la línea base no se pudo confirmar y evitar declarar ese archivo como la última implementación.

## Comando de control usado en I05

```bash
python scripts/verify-incremental-source-lineage.py \
  --parent-zip /ruta/ACHInterbank_NAV_UX_MFT_2026-10-27_I04A_PROYECTO_COMPLETO.zip \
  --expected-parent-sha256 94e6d0c03576b26a9ff34cd32a070679b6e49261f7facb2efe007aca8d2aabd0 \
  --evidence docs/ai/spa-navigation-unification/LINEAGE_VERIFICATION_I05.json
```

Para el siguiente incremento, cambiar el ZIP padre y hash por los de I05; no reutilizar el SHA I04A.
