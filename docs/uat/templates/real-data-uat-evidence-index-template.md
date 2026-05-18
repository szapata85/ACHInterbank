# Plantilla — Índice de evidencias UAT con datos reales o anonimizados

> Uso: consolidar referencias de evidencia sin exponer datos sensibles en Git.

## 1. Reglas de custodia y sensibilidad
- No subir PII, cuentas, saldos reales, llaves privadas, PFX, passwords ni certificados privados.
- Registrar hashes, IDs de evidencia y ubicaciones seguras en lugar de adjuntos sensibles.
- Clasificar sensibilidad y técnica de enmascaramiento por cada evidencia.
- Toda evidencia debe tener responsable de custodia.

## 2. Índice de evidencias

| ID evidencia | Dominio | Cámara | Caso UAT | Archivo/Hash/Referencia | Ubicación segura | Sensibilidad | Enmascaramiento aplicado | Responsable | Estado |
|---|---|---|---|---|---|---|---|---|---|
| EV-UAT-REAL-0001 | S1-10 | CENIT | UAT-REAL-S1-10-001 |  |  |  |  |  | Pendiente |
| EV-UAT-REAL-0002 | S1-10 | CENIT | UAT-REAL-S1-10-002 |  |  |  |  |  | Pendiente |
| EV-UAT-REAL-0003 | S1-11 | CENIT/CUD | UAT-REAL-S1-11-001 |  |  |  |  |  | Pendiente |
| EV-UAT-REAL-0004 | S1-11 | CENIT/CUD | UAT-REAL-S1-11-002 |  |  |  |  |  | Pendiente |
| EV-UAT-REAL-0005 | S1-12 | ACH | UAT-REAL-S1-12-001 |  |  |  |  |  | Pendiente |
| EV-UAT-REAL-0006 | S1-12 | CENIT | UAT-REAL-S1-12-002 |  |  |  |  |  | Pendiente |
| EV-UAT-REAL-0007 | S1-13 | ACH/CENIT | UAT-REAL-S1-13-001 |  |  |  |  |  | Pendiente |
| EV-UAT-REAL-0008 | S1-13 | ACH/CENIT | UAT-REAL-S1-13-002 |  |  |  |  |  | Pendiente |
| EV-UAT-REAL-0009 | S1-20 | Ambas | UAT-REAL-S1-20-001 |  |  |  |  |  | Pendiente |
| EV-UAT-REAL-0010 | S1-20 | Ambas | UAT-REAL-S1-20-002 |  |  |  |  |  | Pendiente |

## 3. Verificación mínima
- [ ] Cada caso UAT tiene al menos una evidencia trazable.
- [ ] Cada evidencia sensible está referenciada por hash/ID y ubicación segura.
- [ ] Cada evidencia tiene responsable y estado.
- [ ] No hay datos sensibles en el repositorio Git.
