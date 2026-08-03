# Evidencia de permisos y privacidad

## Perfiles

- Sin `OutgoingTransactions.Monitor.Read`: menú oculto, ruta protegida y API 403.
- Con lectura funcional: listado, filtros, paginación, detalle y línea de tiempo disponibles.
- Con detalle técnico: solo identificadores y metadatos sanitizados autorizados.

## Privacidad

Se inspeccionaron JSON, DOM, consola y red. El destino se entrega enmascarado (`******7890`); no se detectaron cuentas completas, XML SOAP, credenciales, JWT, cadenas de conexión, PFX, llaves, payloads financieros ni stack traces.
