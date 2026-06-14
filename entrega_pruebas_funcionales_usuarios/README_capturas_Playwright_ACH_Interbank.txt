README - Capturas con Chromium/Playwright para la guía ACH Interbank / CENIT

Objetivo
--------
Tomar printscreen automáticos de las pantallas principales del ambiente local y generar una copia del Word con las capturas anexadas.

Archivos entregados
-------------------
1. Guia_funcional_pruebas_locales_ACH_Interbank_CENIT_con_espacios_printscreen.docx
   - Versión de la guía con anexo visual y espacios para capturas.

2. capturar_y_generar_guia_con_printscreen.py
   - Script que abre Chromium con Playwright, navega por las rutas principales, guarda PNG y genera un Word con capturas reales.

Requisitos en el equipo donde esté levantado ACH Interbank
---------------------------------------------------------
- Python 3.10 o superior.
- Acceso local a la URL de la SPA, por ejemplo: http://localhost:743
- Usuario de prueba autorizado.
- No usar credenciales productivas.

Instalación de dependencias
---------------------------
pip install playwright python-docx
python -m playwright install chromium

Ejecución recomendada
---------------------
Opción A - pasando la contraseña por variable de entorno:

Windows PowerShell:
$env:ACH_TEST_PASSWORD="CONTRASENA_DE_PRUEBA"
python capturar_y_generar_guia_con_printscreen.py --base-url http://localhost:743 --username admin --docx-in Guia_funcional_pruebas_locales_ACH_Interbank_CENIT.docx --docx-out Guia_funcional_pruebas_locales_ACH_Interbank_CENIT_con_capturas_reales.docx

Linux/macOS:
export ACH_TEST_PASSWORD="CONTRASENA_DE_PRUEBA"
python capturar_y_generar_guia_con_printscreen.py --base-url http://localhost:743 --username admin --docx-in Guia_funcional_pruebas_locales_ACH_Interbank_CENIT.docx --docx-out Guia_funcional_pruebas_locales_ACH_Interbank_CENIT_con_capturas_reales.docx

Opción B - mostrando el navegador durante la captura:
python capturar_y_generar_guia_con_printscreen.py --base-url http://localhost:743 --username admin --no-headless --docx-in Guia_funcional_pruebas_locales_ACH_Interbank_CENIT.docx --docx-out Guia_funcional_pruebas_locales_ACH_Interbank_CENIT_con_capturas_reales.docx

Pantallas que captura el script
-------------------------------
01. Pantalla inicial / Login
02. Menú principal
03. Cámaras de compensación
04. Ciclos ACH / CENIT
05. Configuración funcional de ciclos: /transactions/cycle-configs
06. Transacciones
07. Entidades financieras
08. Descripciones / conceptos de entrada

Notas importantes
-----------------
- Los usuarios funcionales no deben ejecutar este script. Debe hacerlo soporte o desarrollo delegado.
- Si el login cambia visualmente o los campos tienen nombres distintos, el script puede capturar la pantalla de login en lugar del módulo. En ese caso, ejecutar con --no-headless para revisar qué está ocurriendo.
- Si alguna ruta aún no está habilitada, el script intentará capturar la respuesta visible del sistema para dejar evidencia.
- Revise visualmente el Word final antes de enviarlo a usuarios.
