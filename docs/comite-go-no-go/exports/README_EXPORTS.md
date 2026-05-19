# Exportaciones Paquete Comite GO/NO-GO - ACH Interbank

Fecha: 2026-05-19
Estado: Continuar UAT controlado / NO-GO productivo

## Contenido

Esta carpeta contiene entregables exportables del paquete de comite GO/NO-GO:

- PDFs individuales por documento Markdown del paquete.
- PDF consolidado `PAQUETE_COMITE_GO_NO_GO_COMPLETO.pdf`.
- Excel operativo `CHECKLIST_UAT_OPERATIVO_ACH_INTERBANK.xlsx`.
- Script reproducible de generacion.

## PDFs Generados

Los PDFs se generan en:

- docs/comite-go-no-go/exports/PDFs/

Incluyen encabezado de proyecto, fecha de generacion, estado UAT controlado / NO-GO productivo y paginacion.

## Excel Generado

El Excel operativo se genera en:

- docs/comite-go-no-go/exports/Excel/CHECKLIST_UAT_OPERATIVO_ACH_INTERBANK.xlsx

Hojas incluidas:

- Resumen.
- Checklist_UAT_Operativo.
- Set_Pruebas_Operativas.
- Evidencias.
- Defectos.
- Datos_Sinteticos.
- Criterios_GO_NO_GO.
- Firmas_Aprobaciones.

## Como Regenerar

Ejecutar desde la raiz del repositorio:

```powershell
python docs/comite-go-no-go/exports/scripts/generate_committee_exports.py
```

## Requisitos

- Python 3.9+.
- openpyxl para Excel.
- reportlab para PDF.

Instalacion sugerida si faltan dependencias:

```powershell
python -m pip install --user openpyxl reportlab
```

## Advertencias

- No contiene datos reales.
- No contiene passwords.
- No contiene tokens completos.
- No contiene certificados privados.
- No constituye aprobacion productiva.
- Productivo sigue NO-GO.
