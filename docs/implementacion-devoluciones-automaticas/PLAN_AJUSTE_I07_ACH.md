# Continuidad después de I07-ACH

1. **I07 estático terminado**: reforzada recuperación íntegra usando huella original ya auditada; sin migración ni modificaciones a CENIT / Angular / transporte.
2. **I08-ACH, siguiente incremento**: ejecutar test C#, BD y NACHA-M con archivos de prueba; resolver diferencias binarias reveladas por I07, sin reemitir secuencias, y separar con evidencia estado generado de enviado y liquidado. Preparar prueba de concurrencia. No añadir arquitectura nueva salvo que la prueba demuestre necesidad.
3. **Habilitación productiva**: prohibida hasta verificar equivalencia de artefactos, política de ciclos vigente, integración SQL, conciliación real, acuses y autorización de la cámara. Mantener `AchCol:EnableAutomaticReturnFileGeneration=false` en el ZIP.
4. **CENIT**: realizar incrementos propios a partir de una línea base posteriormente congelada, sin heredar causal/ciclo/flag de ACH Colombia.

**Regla:** no declarar aprobadas pruebas que no se ejecutaron. Entregar cada incremento en ZIP completo con `MEMORY_STATE.md` de iniciativa, informe y evidencia. No sobrescribir `MEMORY.md` ni `MEMORY_STATE.md` de la raíz, que registran trabajo distinto del simulador.
