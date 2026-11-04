# Línea base y continuidad I06

- **Padre exclusivo:** `ACHInterbank_NAV_UX_MFT_2026-10-27_I05_PROYECTO_COMPLETO.zip`
- **SHA-256 padre confirmado:** `ce27bd3b3e062211fb8d11964fde30246a7627a77931a7d556ebaf4cc20da512`
- **Nuevo ZIP esperado:** `ACHInterbank_NAV_UX_MFT_2026-10-27_I06_PROYECTO_COMPLETO.zip`
- **Tipo:** corrección CSS funcional, NO reapertura I01/I02 ni rollback a I04A.
- **Evidencia:** `LINEAGE_VERIFICATION_I06.json`: comparar contenido de cada ruta padre y detectar faltantes (debe ser `deleted=[]`).
- **Límite:** la herramienta no verifica cambios de otro chat o repositorio que no se hayan entregado en esta sesión.
- Para reanudar, extraer el ZIP I06, comprobar su hash externo y revisar `MEMORY_STATE.md` + `PROGRESS_REPORT_I06_2026-10-27.md`. No usar I05 para nuevas implementaciones salvo si se decide explícitamente revertir I06.
