# Guía operativa UAT para usuarios no técnicos — ACHInterbank

## 1. Propósito
Esta guía ayuda a ejecutar pruebas UAT con datos reales o anonimizados, sin necesidad de conocimientos técnicos.

## 2. Qué debe validar el usuario operativo
- Que el archivo correcto se genere o consulte.
- Que el nombre del archivo sea correcto.
- Que el reporte muestre la información esperada.
- Que el ciclo y la cámara correspondan al caso.
- Que la evidencia sea suficiente para revisión y aprobación.
- Que las diferencias se reporten de forma clara.
- Que los defectos se documenten con su impacto.
- Que no se apruebe si hay errores críticos.

**“El usuario operativo no valida código; valida que el resultado funcional sea correcto para la operación.”**

## 3. Qué NO debe hacer el usuario
- No ejecutar comandos.
- No revisar código.
- No subir datos sensibles a Git.
- No compartir contraseñas.
- No adjuntar PFX, llaves privadas ni certificados privados.
- No declarar GO productivo.
- No firmar si hay P0 abierto.

## 4. Flujo de ejecución
1. Recibir caso UAT.
2. Confirmar datos autorizados.
3. Ejecutar operación o consultar resultado.
4. Comparar resultado esperado vs resultado obtenido.
5. Guardar evidencia.
6. Registrar defecto si aplica.
7. Marcar estado del caso.
8. Solicitar aprobación.

## 5. Estados permitidos

| Estado | Uso operativo |
|---|---|
| Pendiente | Caso aún no iniciado. |
| En ejecución | Caso en revisión o ejecución por usuario. |
| Aprobado | Resultado esperado coincide y evidencia completa. |
| Aprobado con observaciones | Se acepta con observaciones no críticas y plan de seguimiento. |
| Rechazado | Resultado no cumple o evidencia es insuficiente. |
| Bloqueado | No puede continuar por dependencia o riesgo crítico. |

## 6. Protección de datos sensibles
- Usar datos anonimizados o enmascarados.
- No mostrar cuentas completas.
- No mostrar identificaciones completas.
- No subir saldos reales CUD sin autorización.
- Usar hash o referencia interna en lugar de datos completos.
- Guardar soportes sensibles en ubicación segura aprobada.

## 7. Escalamiento
- Escalar a **Tecnología** cuando el resultado no aparece, aparece inconsistente o no permite continuar.
- Escalar a **Operaciones** cuando el ciclo, la cámara o el archivo no corresponden al caso.
- Escalar a **Tesorería** cuando exista diferencia en neteo, liquidez o evidencia CUD.
- Escalar a **Seguridad** cuando haya evidencia de firma/cifrado inválido o posible exposición sensible.
- Escalar a **Riesgo/Compliance** cuando el caso tenga impacto normativo, financiero o de control.

## 8. Veredicto
- Esta guía no habilita producción.
- GO productivo: NO.
- NO-GO productivo vigente hasta scorecard y aprobación formal.

## 9. Restricción de cobertura SPA para 12D
- Referencia de brechas SPA vigente: `docs/audits/spa-angular-backend-uat-alignment-gap-matrix-current.md`.
- Si una validación no está disponible completamente en SPA, debe ejecutarse por ruta documental/manual del paquete 12B/12C.
- Esta restricción no habilita GO productivo y mantiene NO-GO productivo vigente.

## 10. Ejecución UAT con apoyo del SPA
**El SPA apoya la ejecución UAT, pero no reemplaza el paquete de evidencias, defectos, aprobadores, actas ni scorecard UAT. La ejecución 12D/12E se realiza de forma híbrida: SPA + Excel/PDF + evidencias externas aprobadas.**

### 10.1 Qué se ejecuta en el SPA y qué se documenta fuera del SPA

| Actividad | ¿Se ejecuta en SPA? | Pantalla sugerida | Evidencia esperada | Registro en Excel/PDF | Observación operativa |
|---|---|---|---|---|---|
| Consultar reportes transaccionales | Sí | `reports` | Captura/reporte consultado | Sí | No reemplaza aprobación humana. |
| Exportar Accounting Review | Sí | `reports` (exportación operativa) | PDF/CSV/XLSX descargado | Sí | NO contabiliza y no genera asientos. |
| Validar reportes de devoluciones/rechazos | Sí | `reports`, `transactions` | Capturas + IDs de caso | Sí | Validar por cámara y matriz aplicable. |
| Validar archivos/ciclos NACHA | Sí (parcial) | `reports` (archivos/ciclos) | Captura/reporte | Sí | Comparar contra regla operativa. |
| Revisar conciliación/reportes | Sí | `reports` (conciliación) | Captura + export si aplica | Sí | Soporte de revisión contra terceros. |
| Revisar auditoría/histórico/trazabilidad | Sí | `reports`, `audit-logs` | Captura/reporte | Sí | No reemplaza acta de aprobación. |
| Consultar CENIT neteo/posiciones | Sí | `cenit` | Capturas de neteo/posición | Sí | Resultado operativo, no cierre final. |
| Consultar decisiones internas de liquidez CENIT | Sí | `cenit` | Capturas de decisión/estado | Sí | Decisión interna, no liquidación firme. |
| Validar frontera CUD operacional/manual | Parcial | `cenit`, `reports` | Captura + soporte externo/manual | Sí | No 100% SPA; requiere evidencia externa aprobada. |
| Revisar certificados/sobre digital | Sí (controlado) | `nacha-security` | Captura/resultado validación | Sí | Sin exponer secretos ni material sensible. |
| Registrar evidencia UAT | No | Plantillas 12B/12C | Índice de evidencias | Sí (obligatorio) | Gestión documental/manual. |
| Registrar defectos UAT | No | Plantilla de defectos | Defecto documentado | Sí (obligatorio) | Gestión documental/manual. |
| Registrar aprobadores | No | Acta/scorecard | Aprobadores y firmas | Sí (obligatorio) | Gestión documental/manual. |
| Consolidar scorecard UAT | No | Scorecard GO/NO-GO | Scorecard actualizado | Sí (obligatorio) | Gestión documental/manual. |
| Firmar acta UAT | No | Acta UAT | Acta firmada | Sí (obligatorio) | Sin acta no hay GO UAT formal. |

### 10.2 Pantallas SPA para la ejecución UAT

| Dominio | Pantalla/ruta SPA | Uso operativo | Evidencia a guardar | Restricción |
|---|---|---|---|---|
| Reportes | `reports` | Consultar reportes y exportaciones | PDF/CSV/XLSX + captura si aplica | No representa aprobación humana. |
| Accounting Review | `reports` (exportación operativa) | Exportar reporte operativo de revisión | Archivo PDF/CSV/XLSX | NO contabiliza, no genera asientos, no reemplaza evidencia externa. |
| CENIT | `cenit` | Revisar neteo, posiciones netas y decisiones internas de liquidez | Captura o reporte asociado | Liquidez simulada no equivale a saldo real CUD. |
| NACHA Security | `nacha-security` | Revisar certificados/sobre digital | Captura/control de validación | No incluir PFX, claves privadas ni contraseñas en evidencias no seguras. |
| Transacciones | `transactions` | Consultar transacciones, devoluciones, rechazos y ROR | Capturas/reportes/identificadores | Validar contra matriz y cámara aplicable. |
| Auditoría | `audit-logs` | Consultar trazabilidad/auditoría | Captura o export si aplica | No reemplaza acta de aprobación. |
| Inbound | `incoming-nacha-command-center` | Revisar operación inbound/colas (si aplica) | Captura/estado | No reemplaza validación normativa. |

### 10.3 Procedimiento para exportar Accounting Review desde SPA
1. Ingresar al SPA con perfil autorizado.
2. Ir a **Reportes**.
3. Ubicar **Exportar reporte operativo de revisión**.
4. Seleccionar formato: **PDF, CSV o Excel**.
5. Ajustar filtros operativos si aplica.
6. Confirmar checks requeridos.
7. Presionar **Descargar reporte**.
8. Guardar el archivo en ubicación segura aprobada por UAT.
9. Registrar el archivo en el índice de evidencias.
10. Si contiene datos sensibles, no subir a Git ni a repositorios no aprobados.

Advertencias obligatorias:
- El reporte **NO contabiliza**.
- **No genera asientos**.
- No reemplaza evidencia externa.
- No reemplaza aprobación humana.
- No habilita GO productivo.

### 10.4 Procedimiento para validar CENIT/liquidez/CUD desde SPA
1. Ir a la sección **CENIT**.
2. Revisar posiciones netas.
3. Revisar liquidez simulada.
4. Revisar decisiones internas de liquidez.
5. Si aparece **DXX-LIQ**, tratarlo como causal interna.
6. Validar soporte externo CUD por canal aprobado.
7. Registrar evidencia externa/manual en índice de evidencias.
8. Marcar defectos si hay diferencias.

Advertencias obligatorias:
- Liquidez simulada no equivale a saldo real CUD.
- DXX-LIQ no representa rechazo oficial CUD por sí sola.
- Decisión interna no representa liquidación firme.
- Evidencia CUD no equivale a API CUD bancaria.
- No se contabiliza desde esta pantalla.

### 10.5 Procedimiento para certificados/sobre digital
1. Ir a **NACHA Security** o sección equivalente.
2. Revisar certificado, estado y validación.
3. Revisar resultado de firma/cifrado si aplica.
4. Guardar solo evidencia permitida.
5. Registrar resultado en índice de evidencias.

Advertencias:
- No guardar PFX en Git.
- No guardar llaves privadas.
- No guardar passwords de certificados.
- Solo evidencia segura/enmascarada.
- Error de certificado debe registrarse como defecto/hallazgo.

### 10.6 Procedimiento para devoluciones/rechazos/ROR
1. Ir a **Transacciones** o **Reportes**.
2. Buscar transacción o archivo del caso.
3. Validar estado de devolución/rechazo/ROR.
4. Comparar contra matriz de causal/cámara.
5. Registrar evidencia y resultado.
6. Si hay discrepancia, registrar defecto.

Advertencia:
No mezclar causales ACH Colombia con CENIT sin fuente normativa aplicable.

### 10.7 Qué NO se debe hacer en el SPA
- No declarar aprobación productiva.
- No interpretar Accounting Review como asiento contable.
- No interpretar liquidez simulada como saldo real CUD.
- No interpretar DXX-LIQ como rechazo oficial CUD por sí solo.
- No subir secretos, PFX, llaves privadas o passwords.
- No registrar datos sensibles fuera del repositorio documental aprobado.
- No cerrar UAT sin acta humana.
- No reemplazar el Excel/PDF de evidencias.
- No ejecutar UAT 100% SPA-only.

### 10.8 Registro de evidencias
Toda evidencia generada desde SPA debe registrarse en:
- índice de evidencias 12B/12C;
- acta UAT (si aplica);
- plantilla de defectos (si hay hallazgos).

Campos mínimos:
- ID caso UAT.
- Dominio S1.
- Cámara: ACH Colombia / CENIT / Transversal.
- Pantalla SPA usada.
- Archivo o captura generada.
- Responsable.
- Fecha.
- Resultado: aprobado / aprobado con observación / rechazado.
- Defecto asociado (si aplica).

### 10.9 Defectos y hallazgos
Registrar defecto cuando:
- el SPA no muestra información esperada;
- el reporte descargado no corresponde al filtro;
- la semántica visible es confusa;
- no se puede diferenciar CUD real vs liquidez simulada;
- falta evidencia externa;
- hay diferencia contra matriz normativa;
- hay error de certificado/firma/cifrado;
- hay inconsistencia entre backend, reporte y SPA.

### 10.10 Condición para 12D y 12E
12D puede iniciar con restricciones si:
- usuarios reciben esta guía;
- usuarios reciben paquete PDF/Excel UAT;
- se explica ejecución híbrida SPA + evidencia manual;
- se explica que SPA readiness sigue parcial;
- se define custodia de evidencias;
- se define responsable de registrar defectos;
- se mantiene NO-GO productivo.

12E no debe ejecutarse como UAT 100% SPA-only.

### 10.11 Estado GO/NO-GO (vigente)
- GO técnico: parcial/controlado.
- GO UAT formal: pendiente.
- GO productivo: NO.
- NO-GO productivo: vigente.

Esta guía no autoriza producción.

### 10.12 Mini checklist antes de iniciar
- [ ] Tengo acceso al SPA.
- [ ] Tengo el PDF/Excel UAT.
- [ ] Conozco dónde guardar evidencias.
- [ ] Conozco cómo registrar defectos.
- [ ] Conozco quién aprueba.
- [ ] Entiendo que CUD se valida con soporte externo/manual.
- [ ] Entiendo que Accounting Review no contabiliza.
- [ ] Entiendo que no hay GO productivo.

### 10.13 Mini checklist al finalizar
- [ ] Guardé reportes/capturas permitidas.
- [ ] Registré evidencias en el índice.
- [ ] Registré defectos.
- [ ] Marqué resultado por caso.
- [ ] Solicité aprobación humana.
- [ ] No subí datos sensibles a Git.
- [ ] No marqué GO productivo.
