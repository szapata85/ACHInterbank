# Matriz resultados UAT SOAP end-to-end

Fecha: 2026-05-23  
Productivo: **NO-GO**

| Escenario | Resultado | Evidencia |
|---|---|---|
| Proc_Contrapartidas debito CFA | Cerrado tecnico previo | `docs/uat/EVIDENCIAS_SOAP_PROC_CONTRAPARTIDAS.md` |
| Proc_Transacciones credito externo | OK tecnico UAT | `docs/uat/EVIDENCIAS_SOAP_PROC_TRANSACCIONES.md` |
| Proc_Transacciones envelope DryRun | OK tecnico UAT | `docs/uat/evidencias/soap-integrations/mapping-trace/proc_transacciones/proc_transacciones_envelope_sanitizado.xml` |
| RegistrarRespuestaTransaccion no monetario | OK tecnico UAT | `docs/uat/EVIDENCIAS_SOAP_REGISTRAR_RESPUESTA_TRANSACCION.md` |
| Prenotificacion CFA aprobada por respuesta diferencial | OK tecnico UAT | `docs/uat/evidencias/soap-integrations/prenotification-responses/approved/` |
| Prenotificacion CFA rechazada por respuesta diferencial | OK tecnico UAT | `docs/uat/evidencias/soap-integrations/prenotification-responses/rejected/` |
| Missing mapping respuesta diferencial | OK negativo | Tests `DifferentialPrenotificationResponseProcessorTests` |
| Duplicado respuesta diferencial | OK negativo | Tests `DifferentialPrenotificationResponseProcessorTests` |
| No transmision externa UAT/local | OK | Reportes `no_external_transmission_report.md` |
