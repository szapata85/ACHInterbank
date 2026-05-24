# Validacion Runtime NACHA-M UAT - CENIT

Fecha: 2026-05-20  
Ambiente: Docker Compose local/UAT  
Endpoint: `GET /NachaExport/52933d1ba0406af3e64800e809c5e73bab36dddd`

## Resultado

| Control | Resultado |
|---|---|
| HTTP status | 200 |
| Archivo generado por sistema | OK |
| Archivo no vacio | OK, 1060 bytes |
| HTML/JSON error | No |
| SHA256 | `248205FCE69769B8047FEED94346E2E9910918B386D553BC46D6F1218B3D125C` |
| Registros | 1:1, 5:1, 6:2, 7:2, 8:1, 9:3 |
| Transmision externa | No |

## Alcance

El archivo contiene una prenotificacion UAT debito y una transaccion credito opcional generadas por API. No se creo transaccion debito monetaria post-prenotificacion porque la regla de 3 dias habiles no puede cumplirse en la misma ejecucion sin backdating.

## Decision

Validacion tecnica de archivo no vacio: **OK parcial**. Validacion normativa campo-a-campo/homologacion: **pendiente**.
