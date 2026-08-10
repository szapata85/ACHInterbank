# RET-GAP-017 — Contrato Return Out ACH Colombia

## Alcance y propiedad

ACHInterbank genera el Return Out con el perfil NACHA-M vigente, crea el sobre digital mediante la infraestructura criptográfica existente y deposita el artefacto `.ENV` de forma atómica en el directorio de handoff administrado por CFA. El MFT de CFA y su conexión SFTP con ACH Colombia quedan fuera del proceso de ACHInterbank, de acuerdo con el modelo de transporte descrito en ACH Colombia V35, sección 2.4.2.

La configuración `AchOutboundReturnTransport` está deshabilitada por defecto. El único modo soportado es `CfaManagedHandoff`; no se admiten rutas raíz, nombres con traversal, artefactos sin extensión `.ENV`, contenido vacío ni contenido cuya huella SHA-256 no coincida.

## Identidad, dispatch e idempotencia

La identidad técnica del envío está formada por:

- exportación persistida y cámara ACH Colombia;
- nombre externo del sobre digital;
- SHA-256 y tamaño del contenido cifrado;
- clave de idempotencia del dispatch.

El contenido cifrado se persiste antes del primer intento y se reutiliza byte por byte en los retries. Un depósito existente con el mismo nombre y contenido es éxito idempotente; el mismo nombre con contenido distinto es una colisión funcionalmente definitiva. El estado `Transmitted` solo se registra después de que el handoff atómico confirma el artefacto.

## Resultado y correlación

El resultado externo entra por `POST /ach-returns/transport/results` bajo autenticación y la política de generación de devoluciones. Debe incluir identificador externo de evento, nombre de archivo, referencia de transmisión, outcome, código y fecha del resultado.

La correlación exige coincidencia exacta de nombre y referencia con una exportación Return Out cifrada. Cada evento y su fingerprint funcional son únicos. Un duplicado no reaplica estado; una correlación inexistente o ambigua queda en revisión manual; y un resultado final contradictorio no revierte un `Accepted` o `Rejected` previo.

Estados terminales soportados:

- `Accepted`: acuse/resultado aceptado y persistido;
- `Rejected`: rechazo funcional persistido;
- `Acknowledged`: recepción técnica persistida cuando el resultado no es aún terminal.

## Separación de responsabilidades

Este contrato no usa respuestas diferenciales ni operaciones SOAP. Tampoco declara homologación o aceptación externa de ACH Colombia: demuestra la frontera aplicativa real, la entrega al canal administrado por CFA y el procesamiento contractual e idempotente de su resultado.
