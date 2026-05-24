# Precheck Runtime UAT SOAP End-to-End

Fecha: 2026-05-23 18:58:26 -05:00
Commit: cdd126cf
Ambiente: Docker/UAT/local

## Git status

``text

``

## Docker compose ps

``text
NAME                    IMAGE                    COMMAND                  SERVICE            CREATED          STATUS                PORTS
achinterbank-api        achinterbank-api:local   "/app/entrypoint.sh"     achinterbank-api   2 hours ago      Up 2 hours            0.0.0.0:843->8080/tcp, [::]:843->8080/tcp
achinterbank-postgres   postgres:16              "docker-entrypoint.s…"   postgres           4 days ago       Up 4 days (healthy)   127.0.0.1:5432->5432/tcp
achinterbank-spa        achinterbank-spa:local   "/docker-entrypoint.…"   achinterbank-spa   26 minutes ago   Up 26 minutes         0.0.0.0:743->80/tcp, [::]:743->80/tcp
``

## Health

- /health/live: 200
- /health/ready: 200

## Login demo

- Usuario: admin
- Login: validado sin imprimir token ni password
- Roles sanitizados: Admin, ACH.Operator

## Restricciones

- Transmision externa: no ejecutada.
- Productivo: NO-GO.
