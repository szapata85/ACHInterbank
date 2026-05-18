# Plantilla operativa de reporte de defectos UAT

## 1. Plantilla de defecto

| ID defecto | ID caso UAT | Dominio S1 | Cámara | Descripción del problema | Pasos para reproducir | Resultado esperado | Resultado obtenido | Evidencia | Severidad | Impacto operativo | ¿Bloquea aprobación? | Workaround | Responsable | Fecha objetivo | Estado | Decisión |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
|  |  |  |  |  |  |  |  |  | P0/P1/P2/P3 |  | Sí/No |  |  |  |  |  |

## 2. Severidades en lenguaje operativo
- **P0:** Error crítico que bloquea la operación, genera riesgo normativo, riesgo financiero, riesgo de seguridad o impide aprobar UAT.
- **P1:** Error importante con posible workaround aprobado.
- **P2:** Error medio que no bloquea la prueba, pero debe corregirse o aceptarse.
- **P3:** Mejora menor, texto, presentación o ajuste no bloqueante.

## 3. Reglas de decisión
- P0 abierto bloquea GO UAT formal.
- P1 requiere workaround aprobado.
- P2/P3 pueden aceptarse con plan.
- Todo defecto debe tener responsable.
