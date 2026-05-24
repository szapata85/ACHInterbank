# Hallazgos Normativos

Fecha: 2026-05-19  
Estado: evidencia normativa usada para parametrizacion inicial.

## Documentos Revisados

| Camara | Documento | Estado |
|---|---|---|
| ACH Colombia | `docs/normativa/md/ACH-Colombia-V32.md` y PDF MAN-004 V32 | Encontrado |
| CENIT | `docs/normativa/md/CENIT-DSP-152-Anexo-2.md` | Encontrado |
| NACHA-M | Matrices `docs/go-live-readiness/MATRIZ_NACHA_M_*` | Parcial |

## Reglas Identificadas

| Camara | Naturaleza | Prenotificacion | Validacion identificacion | Fuente |
|---|---|---|---|---|
| ACH Colombia | Debito | Obligatoria | Obligatoria en flujo de prenotificacion debito | MAN-004 V32, secciones 2.11.4, 2.11.4.1, 2.11.4.2, 2.11.6 |
| ACH Colombia | Credito | Opcional/discrecional | Opcional si se envia prenotificacion | MAN-004 V32, secciones 2.10.2, 2.10.3, 2.10.3.1, 2.10.3.2 |
| CENIT | Debito | Obligatoria previa a entrada debito | Validacion por participante receptor segun campos del anexo | DSP-152 Anexo 2, seccion 4.7 |
| CENIT | Credito | No obligatoria | Opcional/no bloqueante para export monetaria | DSP-152 Anexo 2, seccion 4.7 |

## Riesgos Normativos

- La validacion NACHA-M campo-a-campo por registros 1/5/6/7/8/9 sigue pendiente de archivo no vacio generado por sistema.
- La parametrizacion inicial no reemplaza homologacion o waiver formal de ACH Colombia/CENIT.
- Cualquier cambio futuro de norma debe quedar auditado en `NormativeSource`, `NormativeReference`, vigencia y usuario de modificacion.
