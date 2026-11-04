# PLAN DE DEVOLUCIONES AUTOMÁTICAS CENIT — corte I04 (2026-11-03)

| Incremento | Trabajo independiente | Estado de código | Ejecución/homologación |
|---|---|---|---|
| I01-CENIT | Modo independiente `DESACTIVADO` por defecto, permisos, auditoría | INCORPORADO | Pendiente |
| I02-CENIT | Detección pasiva, evidencias I02 y excepciones distinguibles | INCORPORADO | Pendiente |
| I03-CENIT | Preparación manual y única en ventana ordinaria, sin envío | INCORPORADO | Pendiente |
| **I04-CENIT** | Aprobación/descarte por segunda persona, metadatos y consulta de excepciones, sin envío | **INCORPORADO** | **Pendiente** |
| I05-CENIT | Generación controlada NACHA-M CENIT sin transmisión no autorizada | PENDIENTE | Pendiente |
| I06-CENIT | Integración, acuse, conciliación y homologación | PENDIENTE | Pendiente |

**CENIT: 4/6 incrementos incorporados al código; FALTAN 2 (I05 e I06).** ACH Colombia: I01–I08 incorporados, I09-B validación compilada obligatoria pendiente y I09-A preparación condicional. En ambas cámaras quedan **3 fases obligatorias** del plan (2 CENIT + 1 ACH) y **0–1 fase ACH opcional**, sujetas a correcciones derivadas de pruebas. Producción: **NO-GO**.

### Antes de I05

1. Compilar y ejecutar xUnit I01–I04 CENIT, aplicar migraciones I03–I04 a SQL Server/PostgreSQL en entornos desechables, probar dos revisores simultáneos, carrera con cambios de modo y evidencia y confirmar auditoría y permisos reales.
2. Probar identidad JWT y que ninguna preparación sin identidad de preparador sea aprobable; gestionar roles/autorización y posible uso de identidades alias de una misma persona.
3. Confirmar que la referencia de soporte corresponda a evidencia real, que la revisión humana no sustituya condiciones especiales R06/R23, calendario, devoluciones de devoluciones ni prenotificaciones.
4. Diseñar y validar NACHA-M de CENIT y su esquema de prueba **sin llamar al generador ACH Colombia**. No añadir transporte en I05.
5. Conservar el estado de ACH Colombia, incluidos 32/35 de I04-ACH y pendientes I09. No confundir aprobación de una reserva con autorización productiva.

**Punto de parada:** I04-CENIT. Próximo I05-CENIT solo mediante nueva instrucción. **NO-GO**.
