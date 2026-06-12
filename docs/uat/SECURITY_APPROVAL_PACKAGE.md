# Paquete aprobacion Seguridad/Compliance UAT - Fase 6D.5

Productivo permanece NO-GO. Este paquete no aprueba seguridad por si mismo, no carga certificados, no crea endpoints reales, no registra secretos y no habilita SOAP real.

## Resumen ejecutivo

Se prepara el flujo de aprobacion de Seguridad/Compliance para permitir, en ambiente UAT aislado, el intercambio controlado de certificados, endpoints, parametros tecnicos y evidencias con ACH Colombia/CENIT. Todos los elementos reales quedan pendientes de aprobacion y custodia corporativa.

## Objetivo

Definir controles, responsables, evidencias y criterios para autorizar preparacion tecnica de UAT externo sin exponer secretos ni afectar produccion.

## Alcance

- Certificados cliente/servidor/CA UAT.
- Endpoints UAT SOAP, transferencia o consulta cuando aplique.
- Custodia de secretos y parametros sensibles.
- Segregacion de ambiente y logging sanitizado.
- Evidencia Seguridad/Compliance previa a cualquier carga.

## Exclusiones

- Produccion.
- Certificados reales en repositorio.
- URLs reales en documentacion.
- Secretos, contrasenas, thumbprints reales o rutas sensibles.
- SOAP real sin autorizacion formal.
- Datos reales de clientes.
- Movimientos monetarios o archivos productivos reales.

## Actores

- CFA Seguridad.
- CFA Tecnologia.
- CFA Operaciones.
- Auditoria/Compliance.
- Mesa UAT.
- ACH Colombia.
- Banco de la Republica/CENIT.
- Proveedor/core/SOAP si aplica.

## Flujo de aprobacion

1. Confirmar alcance UAT externo y Productivo NO-GO.
2. Validar ambiente aislado y dataset sintetico.
3. Revisar registro placeholder de certificados/endpoints.
4. Aprobar canal seguro de recepcion.
5. Validar custodia en mecanismo corporativo de secretos o mecanismo corporativo aprobado.
6. Revisar evidencia de ausencia de secretos en repo/docs/logs.
7. Autorizar carga controlada solo en ambiente UAT aislado.
8. Registrar decision: aprobado, no aprobado o aprobado con observaciones.

## Criterios de entrada

- RACI externo definido.
- Controles de seguridad documentados.
- Dataset sintetico aprobado.
- CI/Playwright disponible.
- Sin secretos, URLs reales, certificados ni datos reales en repo.

## Criterios de salida

- Decision Seguridad/Compliance registrada.
- Responsable de custodia definido.
- Certificados/endpoints en estado aprobado o bloqueado.
- Evidencias sanitizadas anexas.
- SOAP real sigue bloqueado salvo autorizacion formal explicita.

## Evidencias requeridas

Ver `docs/uat/UAT_SECURITY_EVIDENCE_REQUESTS.md`.

## Decision esperada

Estados permitidos: `Aprobado para preparar UAT externo aislado`, `No aprobado`, `Aprobado con observaciones`.

La decision no habilita produccion ni certificacion oficial.
