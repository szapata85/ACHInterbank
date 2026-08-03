# Matriz transacción–intento–respuesta LIVE

Los identificadores internos, cuentas y correlaciones se omiten deliberadamente. Los conteos corresponden al estado persistido final, después de reanudar el scheduler.

| Transacción | Motor | Ciclo real | Elegible | Intentos | WCF recibió éxitos | Respuesta SOAP exitosa | Persistida | API | SPA | Raíces duplicadas |
| --- | --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| TX-A | SQL Server | Sí | Sí | 1 | Sí | 1 | Sí | Sí | Sí | 0 |
| TX-B | SQL Server | Sí | Sí | 2 | Sí, tras recuperación | 1 | Sí | Sí | Sí | 0 |
| TX-C | SQL Server | Sí | Sí | 2 | Sí, en reintento | 1 | Sí | Sí | Sí | 0 |
| TX-A | PostgreSQL | Sí | Sí | 1 | Sí | 1 | Sí | Sí | Sí | 0 |
| TX-B | PostgreSQL | Sí | Sí | 2 | Sí, tras recuperación | 1 | Sí | Sí | Sí | 0 |
| TX-C | PostgreSQL | Sí | Sí | 2 | Sí, en reintento | 1 | Sí | Sí | Sí | 0 |

Para TX-B y TX-C, el primer intento se ejecutó durante la indisponibilidad controlada y quedó como error técnico; por definición no fue recibido por el WCF detenido. TX-C se reintentó de forma dirigida. TX-B fue recuperada por el scheduler después de reanudarlo. En ambos casos se conservó una sola raíz.
