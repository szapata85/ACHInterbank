# Modelo de custodia de secretos UAT - Fase 6D.5

Productivo permanece NO-GO. No se documentan secretos reales, certificados, contrasenas, URLs, thumbprints ni rutas sensibles.

## Principios

- Minimo privilegio.
- Separacion de funciones entre entrega, custodia, carga y validacion.
- Trazabilidad completa de acceso y cambios.
- No exposicion en repositorio, logs, screenshots, CI o documentacion.
- Carga solo en ambiente UAT aislado y aprobado.

## Segregacion de funciones

| Funcion | Responsable primario | Restriccion |
| --- | --- | --- |
| Recepcion segura | CFA Seguridad | No compartir por canales no aprobados |
| Custodia | CFA Seguridad | OpenBao/Vault o mecanismo corporativo aprobado |
| Carga UAT | CFA Tecnologia | Solo con autorizacion formal |
| Validacion | Seguridad + Mesa UAT | Sin revelar secreto |
| Auditoria | Compliance | Evidencia sanitizada |

## Almacenamiento permitido

- OpenBao/Vault o mecanismo corporativo aprobado.
- Secret manager corporativo con auditoria, control de acceso y rotacion.
- Nunca Git, documentos, tickets sin cifrado ni variables no controladas.

## Prohibiciones

- Secretos en Git.
- Secretos en logs.
- Secretos en documentacion.
- Secretos en screenshots.
- Secretos en variables no controladas.
- Certificados o llaves privadas en artefactos CI.
- Payloads SOAP completos con datos sensibles.

## Rotacion y revocacion

- Definir fecha de vencimiento y responsable antes de cargar.
- Rotar ante vencimiento, incidente, cambio de responsable o cierre UAT.
- Revocar inmediatamente ante sospecha de exposicion.

## Auditoria y acceso minimo

- Registrar aprobacion, acceso, carga, rotacion y revocacion.
- Revisar accesos antes y despues de cada ventana.
- Mantener evidencia sanitizada para Compliance.

## Procedimiento ante incidente

1. Suspender ventana UAT.
2. Revocar secreto/certificado afectado.
3. Preservar evidencia sanitizada.
4. Notificar Seguridad, Compliance y Mesa UAT.
5. Abrir hallazgo y bloquear avance hasta cierre.
