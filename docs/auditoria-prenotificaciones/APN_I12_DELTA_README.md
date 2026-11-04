# Delta APN-I12 — lectura y aplicación

El delta contiene solo archivos modificados o nuevos frente al ZIP completo APN-I11, con las rutas intactas `ACHInterbank_ajustado/...`.

**Para Sources se recomienda el ZIP completo I12**; no combinar delta y ZIP completos. El delta se utiliza para revisión del diff y despliegue controlado después de homologación .NET/SQL. El cursor se guarda en la tabla ya existente `TaskParameters`; no se crean estados ni migraciones.
