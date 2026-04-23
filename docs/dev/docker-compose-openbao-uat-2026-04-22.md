# Docker Compose UAT (OpenBao on-prem)

## 1) Levantar stack integral
```bash
cp .env.example .env
# editar OPENBAO_API_TOKEN después de inicializar OpenBao
docker compose up -d --build
```

Servicios: `postgres`, `openbao`, `achinterbank-api`, `achinterbank-spa`.

## 2) Inicializar / unseal OpenBao (primera vez)
```bash
bash scripts/openbao/init-openbao.sh achinterbank-openbao
```
El comando genera `ops/openbao/.openbao-init.local.txt` (NO commitear).

Extraer `Initial Root Token` y `Unseal Key 1` del archivo generado.

```bash
export BAO_ADDR=http://127.0.0.1:8200
bao operator unseal <UNSEAL_KEY_1>
bao login <INITIAL_ROOT_TOKEN>
bao secrets enable -path=secret kv-v2 || true
bao policy write ach-api ops/openbao/policy-ach-api.hcl
bao token create -policy=ach-api -period=24h
```

Copiar el token generado a `.env`:
```env
OPENBAO_API_TOKEN=s.xxxxxx
```
Reiniciar API:
```bash
docker compose up -d achinterbank-api
```

## 3) Validar write/read manual de secreto
```bash
bao kv put secret/certificates/uat/ch-1/outboundsigning/v1 pkcs12Base64="dGVzdA==" password="test"
bao kv get secret/certificates/uat/ch-1/outboundsigning/v1
```

## 4) Validación end-to-end
1. Desde SPA, cargar `.pfx` en gestión de certificados con `storageMode=OpenBaoReference`.
2. Verificar respuesta API: solo `SecretRefMasked`.
3. Activar versión.
4. Ejecutar operación de firmado/descifrado y revisar auditoría.
