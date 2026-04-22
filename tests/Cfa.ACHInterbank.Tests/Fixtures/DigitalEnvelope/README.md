# Digital Envelope Fixtures (Synthetic)

Esta carpeta contiene fixtures **sintéticos** para pruebas de interoperabilidad del sobre digital.

- No contiene certificados reales.
- No contiene private keys reales.
- No contiene datos NACHA reales.

## OfficialVectors

La subcarpeta `OfficialVectors/` está reservada para vectores oficiales futuros (si ACH/CENIT los entrega):

- `official-envelope.env`
- `official-plain-nacha.txt`
- `official-public-cert.cer`
- `official-metadata.json`

Mientras no existan, los tests oficiales quedan en modo *pending* (no fallan la suite).
