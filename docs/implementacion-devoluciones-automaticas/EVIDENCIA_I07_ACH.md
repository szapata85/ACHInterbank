# EVIDENCIA I07-ACH — ejecución realizada y pendientes

**Archivos fuente:** véase `MANIFIESTO_I07_ACH.json`. Comando ejecutado desde la raíz del proyecto:

```bash
python3 scripts/test/automatic_returns_i07_ach_static_check.py
python3 scripts/test/automatic_returns_i06_ach_static_check.py
python3 scripts/test/automatic_returns_i05_ach_static_check.py
python3 scripts/test/automatic_returns_i03_ach_static_check.py
python3 scripts/test/automatic_returns_i02_ach_static_check.py
python3 scripts/validate-solution-projects.py
```

| Verificación | Resultado observado | Afirmación permitida |
|---|---:|---|
| I07 fuente | 24/24 | PASS_STATIC |
| I06 fuente | 28/28 | PASS_STATIC |
| I05 fuente | 33/33 | PASS_STATIC |
| I03 fuente | 25/25 | PASS_STATIC |
| I02 fuente | 18/18 | PASS_STATIC |
| Grafo de solución | 7 proyectos correctos | Referencias correctas, sin compilación |
| I04 fuente | 32/35 | Tres reglas de no-generación/no-descarga obsoletas, NO aprobar script completo |
| .NET SDK | Ausente | Sin build / xUnit |
| SQL Server/PostgreSQL | No utilizados | Sin prueba de persistencia/concurrencia |
| Homologación y archivos reales | No ejecutada | NO-GO |

## Casos xUnit añadidos (no ejecutados)

- Bytes reconstruidos alterados — bloquear.
- Evento de generación ausente — bloquear.
- Evento JSON inválido — bloquear.
- Nombre de archivo/cámara/causal/estado original inconsistentes — bloquear.
- Evento y huella correctos — recuperar sin regenerar.
- La huella autoritativa es la del **evento**, no el texto `ContentSha256` proporcionado por el reconstruidor.
- El endpoint ACH Colombia debe rechazar candidatas CENIT.

No se contabilizan estos casos como PASS. El punto de parada y las pruebas UAT necesarias figuran en `MATRIZ_UAT_I07_ACH.md`.
