-- SQL SERVER / ACHInterbank - SOLO LECTURA.
-- Ejecutar en SSMS contra una copia de la BD o la instancia de desarrollo.
USE [ACHInterbank];
SET NOCOUNT ON;

PRINT '01 - Tablas y migraciones';
SELECT COUNT(*) AS UserTables FROM sys.tables WHERE is_ms_shipped = 0;
IF OBJECT_ID(N'dbo.__EFMigrationsHistory', 'U') IS NOT NULL
    SELECT COUNT(*) AS AppliedMigrations, MAX(MigrationId) AS LastAppliedMigration
    FROM dbo.__EFMigrationsHistory;
ELSE PRINT 'No existe tabla de historial EF Core';

PRINT '02 - Navegacion obsoleta y destino valido';
IF OBJECT_ID(N'dbo.MenuItems', 'U') IS NOT NULL
BEGIN
    SELECT Id, Route, Label, ParentId, IsActive
    FROM dbo.MenuItems
    WHERE Route IN ('/operational-restrictions','/soap-integrations','/integraciones/conexion')
    ORDER BY Route, Id;
    SELECT Route, COUNT(*) AS Total, SUM(CASE WHEN IsActive=1 THEN 1 ELSE 0 END) AS ActiveRows
    FROM dbo.MenuItems
    WHERE Route IN ('/operational-restrictions','/soap-integrations','/integraciones/conexion')
    GROUP BY Route;
END;

PRINT '03 - Catalogo y versiones de contratos SOAP';
IF OBJECT_ID(N'dbo.IntegrationMethods','U') IS NOT NULL
BEGIN
    SELECT Id, Code, DisplayName, IsActive
    FROM dbo.IntegrationMethods
    WHERE Code IN ('WSCFAACH.Proc_Contrapartidas','WSCFAACH.Proc_Transacciones','WSAXON.RegistrarRespuestaTransaccion');
END;
IF OBJECT_ID(N'dbo.IntegrationContractVersions','U') IS NOT NULL
BEGIN
    SELECT m.Code, v.VersionLabel, v.Status, v.SourceType, v.SoapAction, v.Revision
    FROM dbo.IntegrationContractVersions v
    JOIN dbo.IntegrationMethods m ON m.Id=v.MethodId
    WHERE m.Code IN ('WSCFAACH.Proc_Contrapartidas','WSCFAACH.Proc_Transacciones','WSAXON.RegistrarRespuestaTransaccion')
    ORDER BY m.Code, v.VersionLabel;
END;

PRINT '04 - Parametros activos del contrato';
IF OBJECT_ID(N'dbo.IntegrationMethodParameters','U') IS NOT NULL
BEGIN
    SELECT m.Code, p.Direction, COUNT(*) AS ActiveParameters,
           SUM(CASE WHEN p.Required=1 THEN 1 ELSE 0 END) AS FunctionalRequired,
           SUM(CASE WHEN p.MinOccurs<>0 THEN 1 ELSE 0 END) AS XsdMinOccursNotZero,
           SUM(CASE WHEN LOWER(p.DataType)='string' AND p.Nillable=0 THEN 1 ELSE 0 END) AS StringNotNillable
    FROM dbo.IntegrationMethodParameters p
    JOIN dbo.IntegrationMethods m ON m.Id=p.MethodId
    WHERE p.IsActive=1 AND m.Code IN ('WSCFAACH.Proc_Contrapartidas','WSCFAACH.Proc_Transacciones','WSAXON.RegistrarRespuestaTransaccion')
    GROUP BY m.Code,p.Direction ORDER BY m.Code,p.Direction;
END;

PRINT '05 - Mappings publicados y origen de publicacion (no muestra valores sensibles)';
IF OBJECT_ID(N'dbo.IntegrationMappingSets','U') IS NOT NULL
BEGIN
    SELECT m.Code, s.Id AS SetId, s.Version, s.Status, s.IsActive, s.PublishedBy, s.ContractVersionId,
           (SELECT COUNT(*) FROM dbo.IntegrationMappingRules r WHERE r.MappingSetId=s.Id AND r.Enabled=1) AS EnabledRules
    FROM dbo.IntegrationMappingSets s
    JOIN dbo.IntegrationMethods m ON m.Id=s.MethodId
    WHERE m.Code IN ('WSCFAACH.Proc_Contrapartidas','WSCFAACH.Proc_Transacciones','WSAXON.RegistrarRespuestaTransaccion')
    ORDER BY m.Code,s.Version DESC;

    SELECT m.Code, COUNT(*) AS ActivePublishedSets
    FROM dbo.IntegrationMappingSets s JOIN dbo.IntegrationMethods m ON m.Id=s.MethodId
    WHERE s.Status=2 AND s.IsActive=1
      AND m.Code IN ('WSCFAACH.Proc_Contrapartidas','WSCFAACH.Proc_Transacciones','WSAXON.RegistrarRespuestaTransaccion')
    GROUP BY m.Code HAVING COUNT(*)>1;
END;

PRINT '06 - Campos funcionalmente requeridos sin mapeo activo';
IF OBJECT_ID(N'dbo.IntegrationMappingRules','U') IS NOT NULL
BEGIN
    SELECT m.Code, s.Id AS PublishedSetId, p.ParameterPath
    FROM dbo.IntegrationMappingSets s
    JOIN dbo.IntegrationMethods m ON m.Id=s.MethodId
    JOIN dbo.IntegrationMethodParameters p ON p.MethodId=m.Id AND p.Required=1 AND p.Direction=1 AND p.IsActive=1
    WHERE s.Status=2 AND s.IsActive=1
      AND m.Code IN ('WSCFAACH.Proc_Contrapartidas','WSCFAACH.Proc_Transacciones','WSAXON.RegistrarRespuestaTransaccion')
      AND NOT EXISTS (SELECT 1 FROM dbo.IntegrationMappingRules r
                      WHERE r.MappingSetId=s.Id AND r.ParameterId=p.Id AND r.Enabled=1)
    ORDER BY m.Code,p.ParameterPath;
END;

PRINT '07 - Estado de configuracion SOAP sin exponer endpoints';
IF OBJECT_ID(N'dbo.SoapIntegrationSettings','U') IS NOT NULL
    SELECT COUNT(*) AS SettingsRows FROM dbo.SoapIntegrationSettings;

PRINT 'NOTA: un mapping con origen publicado no demuestra homologacion bancaria ni permite Live por si solo.';
