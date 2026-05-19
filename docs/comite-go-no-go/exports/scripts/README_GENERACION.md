# Generacion de Exportables - Paquete Comite GO/NO-GO

## Script

Archivo:

- `docs/comite-go-no-go/exports/scripts/generate_committee_exports.py`

## Uso

Ejecutar desde la raiz del repositorio:

```powershell
python docs/comite-go-no-go/exports/scripts/generate_committee_exports.py
```

## Dependencias

El script usa:

- `openpyxl` para crear el Excel sin macros y sin automatizacion COM.
- `reportlab` para crear PDFs desde los Markdown fuente.

Instalacion:

```powershell
python -m pip install --user openpyxl reportlab
```

## Salidas

- `docs/comite-go-no-go/exports/PDFs/*.pdf`
- `docs/comite-go-no-go/exports/PDFs/PAQUETE_COMITE_GO_NO_GO_COMPLETO.pdf`
- `docs/comite-go-no-go/exports/Excel/CHECKLIST_UAT_OPERATIVO_ACH_INTERBANK.xlsx`

## Comportamiento Ante Errores

- Si falla la generacion PDF por dependencia faltante, el script reporta el error y continua con Excel si es posible.
- Si falta `openpyxl`, el Excel no puede generarse y debe instalarse la dependencia.
- El script no incluye datos reales, passwords, tokens ni certificados privados.

## Decision

Estos exportables sostienen continuidad de UAT controlado. No constituyen GO productivo.
