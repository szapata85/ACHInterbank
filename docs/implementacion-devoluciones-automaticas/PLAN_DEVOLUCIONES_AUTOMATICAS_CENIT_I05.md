# PLAN DE DEVOLUCIONES AUTOMÁTICAS CENIT — I05 (2026-11-03)

| Incremento | Función independiente | Estado código | Compilación / homologación |
|---|---|---|---|
| I01-CENIT | Configuración independiente desactivada por defecto | INCORPORADO | Pendiente |
| I02-CENIT | Detección pasiva y evidencias | INCORPORADO | Pendiente |
| I03-CENIT | Preparación idempotente sin envío | INCORPORADO | Pendiente |
| I04-CENIT | Revisión, aprobación/descarte, excepciones informativas | INCORPORADO | Pendiente |
| **I05-CENIT** | **Generación local PPD/CCD, perfil NACHA-M CENIT 2026, cifrado y hash, sin envío** | **INCORPORADO** | **Pendiente** |
| I06-CENIT | Transporte autorizado, naming, acuses, conciliación, pruebas e integración | PENDIENTE | Pendiente |

**Avance de código CENIT: 5/6 (83,3 %). Falta I06, un incremento.** Cifras no equivalen a madurez bancaria ni porcentajes de certificación.

**ACH Colombia:** I01–I08 incorporados al código, sin intervención funcional desde esta iniciativa CENIT. I09-B (validación compilada) obligatorio pendiente; I09-A (preparación técnica) pendiente solo si el ambiente la necesita. **Total: 2 fases obligatorias** entre ambas cámaras + hasta **1 opcional ACH**; cualquier hallazgo de pruebas podrá ampliar el trabajo. **NO-GO productivo**.

## Prerrequisitos para I06 y gates de seguridad

1. Compilar y ejecutar xUnit I01–I05, aplicar migraciones en BD desechables SQL Server/PostgreSQL, probar concurrencia y rollback de la secuencia; no afirmar certificación sin evidencias ejecutadas.
2. Definir flujo de seguridad de artefactos cifrados, restauración de llaves, retención, expurgo, auditoría y autorización de descarga, ante fallos de nodos o pérdida de respuesta HTTP.
3. Acordar contrato de nombre externo/identificador diario CENIT (en I05 sólo `NO_TRANSMITIR_*` y FileIdModifier interno de prueba); sin naming homologado el archivo NO debe enviarse.
4. Resolver coordinación mutua con el generador manual CENIT existente y prevenir doble devolución en presencia de dos rutas en BD relacional; no modificar ACH Colombia fuera de una necesidad demostrada.
5. Validar CENIT 2026 con golden files firmados/homologados, casos especiales R06/R23/R34, prenotificaciones, devolución de devolución y CTX separados, sin asumir reglas ACH.
6. Introducir acuse idempotente, confirmación de recepción y conciliación **solo tras pruebas**, bajo feature flags apagados por defecto, estado productivo NO-GO hasta homologación. No abrir canales bancarios por crear un archivo.
7. Conservar estado histórico ACH Colombia, especialmente I04-ACH 32/35 e I09-A/B.

**Punto de parada:** I05-CENIT. **Próximo:** I06-CENIT mediante instrucción nueva. Ninguna habilitación productiva.
