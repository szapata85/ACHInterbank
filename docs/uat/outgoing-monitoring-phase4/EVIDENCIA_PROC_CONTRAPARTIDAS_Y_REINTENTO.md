# Evidencia de Proc_Contrapartidas y reintento

## Modo seguro

La prueba usó intentos persistidos sintéticos con `ExecutionMode = DryRun` y endpoint nominal `simulador-local-controlado`. No se invocó WCF/SOAP Live.

## Falla y reintento

- Falla: `TIMEOUT`, transporte agotado, error técnico sanitizado, reintento permitido.
- Resultado funcional: no determinado; nunca se convirtió en rechazo.
- Reintento: misma transacción raíz, intento 1 técnico y intento 2 exitoso.
- Conteos: una transacción, dos intentos, cero XML persistido en el fixture, cero movimientos y cero respuestas duplicadas.
- Idempotencia: la segunda ejecución del seed mantuvo los mismos conteos.
