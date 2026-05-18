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
