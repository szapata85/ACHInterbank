# Exportables UAT 12C

Este directorio contiene entregables **derivados** para usuarios operativos:

- `UAT_ACHInterbank_Guia_Operativa_Usuarios.pdf`
- `UAT_ACHInterbank_Set_Pruebas_Operativas.xlsx`

## Reglas clave
- Los archivos PDF/XLSX son **generados localmente**; no se versionan en Git.
- Se generan desde script reproducible:
  - `python tools/uat/generate_uat_operator_deliverables.py`
- Los archivos quedan en:
  - `docs/uat/operator-guides/exports/`
- No contienen datos reales ni evidencias reales.
- No deben incluir cuentas, identificaciones, saldos, PFX, passwords, llaves privadas ni certificados privados.
- Las versiones diligenciadas deben almacenarse en repositorio documental seguro aprobado.
- Este paquete no habilita producción.
- GO productivo: **NO**.
- NO-GO productivo: **vigente**.
