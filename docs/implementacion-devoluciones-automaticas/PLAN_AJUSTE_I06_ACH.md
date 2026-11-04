# Continuidad posterior a I06 — ACH Colombia / CENIT independientes

- **I06-ACH (alcance cerrado en fuente):** conectar a la fase de procesamiento la generación interna automática de archivos `.RET`, por lote acotado, con dos habilitaciones simultáneas y puerta técnica desactivada en la entrega. Recuperación sin regeneración desde la vista existente de devoluciones. No transporte.
- **I07-ACH (siguiente incremento, si se solicita):** ejecutar pruebas .NET, Angular, SQL Server/PostgreSQL, validar calendario ACH 5/7, concurrencia y equivalencia de recuperación; revisar estados financieros del generador y preparar evidencia de homologación. Sin activar transporte productivo sin pruebas.
- **I08-ACH (posible cierre):** evaluar conexión controlada a transporte y conciliación solo si I07 aporta evidencia suficiente. No asumir disponibilidad de ambiente ChatGPT; documentar bloqueos externos y preservar NO-GO cuando corresponda.
- **I01–I06 CENIT:** iniciativa distinta, con normas, ventanas, estructuras NACHA-M y certificación propias. No arrastrar la habilitación ACH hacia CENIT.

**Regla de continuidad:** trabajar siempre desde el ZIP acumulativo más reciente; actualizar `docs/implementacion-devoluciones-automaticas/MEMORY_STATE.md` sin borrar memorias históricas, entregar ZIP íntegro, documentación, pruebas y punto de parada al final de cada incremento. No habilitar el flag técnico sin UAT.
