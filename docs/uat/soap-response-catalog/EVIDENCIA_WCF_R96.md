# Evidencia WCF R96

Fecha local de Bogotá: 2026-07-16. Ruta validada: directorio local del WCF autorizado. No se copia el log ni el payload.

## Línea base y delta

| Momento | Tamaño | SHA-256 |
| --- | ---: | --- |
| Antes de la única llamada | 1.418 bytes | `1F5F185F5FCA42CFBC7666206039E4328FA25A84D959565A911D6A76A0B8A951` |
| Después de la única llamada | 2.129 bytes | `105C843CA6A3A6BF41E04FBD68BA9A8D9F3BEA8848A7E4712F74E7D5847BE046` |
| Después del gate de duplicidad | 2.129 bytes | mismo hash |
| Después del reinicio y consulta | 2.129 bytes | mismo hash |

El delta fue de 711 bytes y se produjo en la ventana temporal del escenario.

## Validación enmascarada

- Se observó una nueva operación `Proc_Contrapartidas` con respuesta R96.
- No hubo entradas nuevas de `Proc_Transacciones`, `RegistrarRespuestaTransaccion` ni `PLValidarUsuarioBV`.
- El request outbound persistido y las pruebas de caracterización del cliente no contienen elemento `METODO`; el método se expresa mediante la operación y `SOAPAction` contractuales.
- El archivo legacy del WCF incluye una representación interna de logger con etiqueta `METODO` dentro de una trama reconstruida por ese servicio. Esta etiqueta no aparece en el body outbound producido por ACHInterbank. Se conserva como riesgo de ambigüedad de evidencia del logger, no como modificación del contrato enviado.
- El segundo dispatch rechazado y la verificación posterior al reinicio no alteraron tamaño, fecha ni hash, demostrando ausencia de una segunda llamada.

Correlación: se usaron los identificadores internos del escenario, pero no se publican completos.
