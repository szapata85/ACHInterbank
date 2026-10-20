# Evidencia — Proc_Contrapartidas LIVE

La ejecución partió del endpoint normal de creación de transacciones y del orquestador dirigido UAT; el adaptador SOAP no fue invocado directamente.

Cadena demostrada:

1. raíz persistida en `AchTransactions`;
2. clasificación de salida y ciclo asignado;
3. ítem elegible de despacho;
4. selección por el orquestador;
5. intento persistido;
6. tráfico HTTP real hacia el WCF local;
7. ejecución de `Proc_Contrapartidas`;
8. respuesta SOAP real R96 para los intentos exitosos;
9. interpretación y persistencia estructurada;
10. consulta por API y visualización por SPA Docker.

El error controlado se provocó deteniendo únicamente el IIS Express local. El cliente efectuó un intento real de transporte, que quedó persistido como error técnico; el WCF no pudo recibirlo precisamente por estar indisponible. No se creó rechazo financiero ni respuesta funcional ficticia. Tras restablecer el servicio, el reintento usó la misma raíz y obtuvo respuesta real.

Los intentos conservan servicio, modo, inicio, fin, duración, resultado técnico, código/descripción sanitizados y correlación. No se persiste ni se expone XML completo en el monitor.
