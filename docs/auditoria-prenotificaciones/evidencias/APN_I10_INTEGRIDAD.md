# Integridad APN-I10 — evidencia dentro del ZIP

- Base: `ACHInterbank_COMPLETO_CORRECCION_PRENOTIFICACIONES_APN_I09_2026-11-03.zip`.
- Las **4.998 entradas** del proyecto base se conservaron; ninguna fue eliminada.
- Se modificaron **6 archivos existentes** y se añadieron nuevos archivos de documentación y pruebas.
- `ZipFile.testzip()` y la comparación SHA-256 por entrada se verifican al construir el ZIP completo y el delta.
- El hash SHA-256 **del ZIP** se publica fuera del mismo, en `APN_I10_INTEGRIDAD.md`, para evitar la autorreferencia.
- Integridad del archivo no implica compilación ni certificación funcional.
