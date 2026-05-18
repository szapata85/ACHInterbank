# Guía de aprobación y firma UAT para usuarios operativos

## 1. Cuándo aprobar
- El resultado esperado coincide con el resultado obtenido.
- La evidencia está completa.
- No hay P0 abierto.
- P1 tiene workaround aprobado si aplica.
- Los datos están protegidos.
- El aprobador corresponde al dominio y cámara.

## 2. Cuándo aprobar con observaciones
- Existen defectos P2/P3.
- Existe P1 con workaround formal.
- La evidencia es suficiente.
- El riesgo fue aceptado por el responsable.

## 3. Cuándo rechazar
- P0 abierto.
- Resultado no coincide.
- Evidencia incompleta.
- Datos sensibles expuestos.
- Cámara incorrecta.
- Naming incorrecto.
- CUD no soportado.
- Firma/cifrado no validado.
- Acta incompleta.

## 4. Aprobadores mínimos

| Rol | Cobertura sugerida |
|---|---|
| QA UAT | Todos los dominios S1 |
| Operaciones | Todos los dominios S1 |
| Tesorería | S1-10 / S1-11 |
| Seguridad | S1-13 |
| Compliance | Todos los dominios S1 |
| Riesgo Operacional | Todos los dominios S1 |
| Dueño de proceso | Cierre funcional |
| Tecnología | Soporte de cierre operativo |

## 5. Relación con GO productivo
- Esta guía no aprueba producción.
- GO productivo requiere scorecard actualizado.
- GO productivo requiere cierre de P0.
- GO productivo requiere comité o autoridad definida.
- NO-GO productivo vigente hasta aprobación formal.
