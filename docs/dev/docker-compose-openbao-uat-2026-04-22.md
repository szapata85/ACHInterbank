# Docker Compose UAT (OpenBao on-prem)

> **Ámbito:** Este bootstrap es para UAT/laboratorio controlado (no hardening productivo).

## Levantar todo en un solo flujo
```bash
cp .env.example .env
docker compose up -d --build
```

Con ese comando se levantan: `postgres`, `openbao`, `openbao-bootstrap`, `achinterbank-api`, `achinterbank-spa`.

## Qué automatiza `openbao-bootstrap`
El contenedor `openbao-bootstrap` ejecuta `scripts/openbao/bootstrap-openbao-uat.sh` y deja OpenBao listo para uso API:
1. Espera disponibilidad de OpenBao.
2. Inicializa (`operator init`) si es primera vez.
3. Hace `unseal` con la llave del estado local.
4. Habilita KV v2 en mount configurado (default `secret`).
5. Aplica policy mínima `ach-api`.
6. Emite/reutiliza token de API.
7. Publica token en volumen compartido `ach_openbao_bootstrap` (`/openbao-bootstrap/api-token`).

La API consume ese token vía `DigitalEnvelope:OpenBao:ApiTokenFilePath=/openbao-bootstrap/api-token`, sin requerir edición manual de `.env` ni reinicio posterior.

## Verificación operativa rápida
```bash
docker compose ps
docker compose logs openbao-bootstrap --tail=100
docker compose exec achinterbank-api sh -c 'test -s /openbao-bootstrap/api-token && echo token-ok'
```

## Validación funcional
1. Cargar `.pfx` privado desde la consola de certificados.
2. Confirmar respuesta solo con `SecretRefMasked`.
3. Ejecutar operación de firma/descifrado y revisar auditoría.

## Notas de seguridad
- No usar secretos/certificados reales en este modo.
- El bootstrap UAT prioriza reproducibilidad operativa sobre hardening.
- Para producción: separar init/unseal, usar auth method no-token estático de operación, y controles de rotación/segregación de funciones.

## Caso real controlado: expired-but-retained historical decrypt
1. Levantar stack con `docker compose up -d --build`.
2. Cargar certificado privado de prueba y activar versión.
3. Simular versión histórica vencida (`NotAfter` pasado) reteniendo `SecretRef`.
4. Ejecutar decrypt de sobre histórico con `recipientInfo.certificateInfo` del cert vencido.
5. Verificar auditoría `OperationType=HistoricalDecrypt` y `contextJson` con `SecretRefMasked`.

## Script de ejecución E2E controlado
Para ejecutar el caso completo en ambiente con Docker:
```bash
bash scripts/openbao/run-historical-decrypt-e2e-uat.sh
```

El caso usa resolución histórica por `recipientInfo.certificateInfo.issuer + serial` (con fallback opcional por thumbprint) y exige auditoría `OperationType=HistoricalDecrypt`.

## Ejecución en este entorno (2026-04-23 UTC)
Se intentó ejecutar la secuencia E2E completa, pero este entorno no dispone de Docker (`docker: command not found`).
Para evidencia real, ejecutar exactamente el script en un host Docker-capable:
```bash
bash scripts/openbao/run-historical-decrypt-e2e-uat.sh
```
