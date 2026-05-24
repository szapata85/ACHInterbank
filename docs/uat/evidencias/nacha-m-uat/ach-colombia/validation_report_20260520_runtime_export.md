# Validacion Runtime NACHA-M UAT - ACH Colombia

Fecha: 2026-05-20  
Ambiente: Docker Compose local/UAT  
Endpoint: `GET /NachaExport/7301fd9bf4c1bd7383399cd9d844fd1ccbd97649`

## Resultado

| Control | Resultado |
|---|---|
| HTTP status | 200 |
| Archivo generado por sistema | OK |
| Archivo no vacio | OK, 1060 bytes |
| HTML/JSON error | No |
| SHA256 | `8EA137CBDCEA6CC4280E5183A66FD29983FE0BF0D4F42732A477AC18DD211844` |
| Registros | 1:1, 5:1, 6:2, 7:2, 8:1, 9:3 |
| Transmision externa | No |

## Alcance

El archivo contiene una prenotificacion UAT debito y una transaccion credito opcional generadas por API. No se creo transaccion debito monetaria post-prenotificacion porque la regla de 3 dias habiles no puede cumplirse en la misma ejecucion sin backdating.

## Decision

Validacion tecnica de archivo no vacio: **OK parcial**. Validacion normativa campo-a-campo/homologacion: **pendiente**.
