# Cómo incorporar APN-I12 en Sources

**Sube solamente el ZIP completo** `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I12_2026-11-03.zip` como nueva línea base. No es necesario mezclar I11 ni aplicar el delta en Sources.

El ZIP conserva `ACHInterbank_ajustado/`, código de ambas cámaras, incrementos anteriores, `MEMORY.md`, `MEMORY_STATE.md`, `MEMORY_STATE_APN_I01...I11` donde apliquen, y suma `MEMORY_STATE_APN_I12.md` con los resultados presentes.

El archivo delta es para revisión de cambios entre I11 e I12. Las pruebas Python no sustituyen compilación `.NET` ni UAT. Iniciar cualquier homologación usando el script `scripts/audit/prenotificaciones/run_apn_i12_dotnet.sh` en un ambiente dotnet apto.

Para continuidad consultar: `MEMORY_STATE_APN_I12.md`, `docs/auditoria-prenotificaciones/incrementos/APN-I12_SELECCION_JUSTA_Y_REEVALUACION.md`, `docs/auditoria-prenotificaciones/STATUS_IMPLEMENTACIONES_I12.md`, `docs/auditoria-prenotificaciones/HALLAZGOS_I12.md`.
