# Preparacion UAT externo ACH Colombia/CENIT - Fase 6D.4

Productivo permanece NO-GO. Este documento prepara coordinacion externa; no ejecuta UAT externo, no carga certificados reales, no crea endpoints reales y no habilita SOAP real.

## Resumen ejecutivo

El paquete UAT interno ya cuenta con matriz trazable, dataset sintetico, evidencia Playwright/CI y hallazgos priorizados. La siguiente fase requiere coordinacion formal con ACH Colombia y Banco de la Republica/CENIT para validar formatos, causales, ventanas, parametros, certificados/endpoints y evidencia oficial en ambiente aislado.

## Objetivo

Definir condiciones de entrada/salida, responsabilidades y evidencias para ejecutar UAT externo ACH Colombia/CENIT sin produccion, sin datos reales no autorizados y sin movimientos monetarios reales.

## Alcance ACH Colombia

- Validacion MAN-004 V32 con archivos NACHA-M salida/entrada.
- Naming `RRRRTTT.ZZZ.N`, records 1/5/6/7/8/9, fixed-width 106, totales, padding y `.RET`.
- Prenotificaciones, respuestas, rechazos/devoluciones y conciliacion read-only.
- Evidencia de causales y estados contra operador ACH Colombia.

## Alcance CENIT/Banco de la Republica

- Validacion DSP-152/Anexo 2 y Anexos A/B de causales.
- Archivos salida/entrada, `.RET`, respuestas diferenciales y ROR.
- Ciclos, colas, neteo y ventanas CENIT con dataset sintetico.
- Evidencia oficial de recepcion/procesamiento sin movimiento monetario real.

## Exclusiones

- Produccion.
- SOAP real sin autorizacion formal.
- Movimientos monetarios reales.
- Datos reales no autorizados.
- Certificacion oficial automatica.
- Endpoints, certificados o credenciales reales en repositorio.
- Legacy como fuente oficial.
- `/NachaExport/{hash}`.

## Precondiciones

- Ambiente UAT aislado, segregado de produccion.
- Dataset sintetico aprobado y cargado.
- Perfiles `nacha-config` publicados/vigentes para pruebas.
- Evidencias CI/Playwright disponibles.
- Endpoints/certificados/credenciales externos definidos por canales seguros, pero no cargados sin autorizacion.
- Aprobacion de Seguridad/Compliance para ventana externa.
- Plan de suspension/no avance aprobado.

## Criterios de entrada

- RACI aceptado por CFA, ACH Colombia y CENIT.
- Ventana UAT aprobada.
- Evidencias internas empaquetadas.
- Matriz de hallazgos abierta y visible.
- Confirmacion de que Productivo sigue NO-GO.

## Criterios de salida

- Evidencia externa recibida o defecto registrado.
- Resultado por escenario actualizado.
- Causales/homologaciones validadas o pendientes documentadas.
- Certificados/endpoints tratados sin secretos en repositorio.
- Decision UAT externo: continuar, repetir o bloquear. No implica GO productivo.

## Resultado esperado

Paquete listo para agenda de UAT externo con terceros. La certificacion oficial y la salida productiva siguen pendientes de aprobacion formal.
