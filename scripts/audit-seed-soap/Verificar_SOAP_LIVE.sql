-- Diagnóstico DE SOLO LECTURA para la base operativa ACHInterbank.
-- Ejecutar después de /Maintenance/seed y antes de habilitar ejecución de transacciones Live.
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.SoapIntegrationSettings', N'U') IS NULL
BEGIN
  SELECT N'No existe dbo.SoapIntegrationSettings. Verifique migraciones y base de datos.' AS Error;
  RETURN;
END;

;WITH methods AS (
 SELECT s.Id,
        TRY_CONVERT(nvarchar(200), JSON_VALUE(x.value, '$.methodName')) AS MethodName,
        JSON_VALUE(x.value, '$.operatingMode') AS OperatingMode,
        TRY_CONVERT(bit, JSON_VALUE(x.value, '$.enabled')) AS Enabled,
        JSON_VALUE(x.value, '$.endpoint') AS Endpoint,
        JSON_VALUE(x.value, '$.soapAction') AS SoapAction,
        TRY_CONVERT(int, JSON_VALUE(x.value, '$.timeoutSeconds')) AS TimeoutSeconds
 FROM dbo.SoapIntegrationSettings AS s
 CROSS APPLY OPENJSON(s.WscfaachMappingsJson) AS x
 UNION ALL
 SELECT s.Id, JSON_VALUE(x.value, '$.methodName'), JSON_VALUE(x.value, '$.operatingMode'),
        TRY_CONVERT(bit, JSON_VALUE(x.value, '$.enabled')),
        JSON_VALUE(x.value, '$.endpoint'), JSON_VALUE(x.value, '$.soapAction'),
        TRY_CONVERT(int, JSON_VALUE(x.value, '$.timeoutSeconds'))
 FROM dbo.SoapIntegrationSettings AS s
 CROSS APPLY OPENJSON(s.WsAxonRespuestaTransaccionesMappingsJson) AS x
)
SELECT MethodName, OperatingMode, Enabled, Endpoint, SoapAction, TimeoutSeconds,
 CASE WHEN OperatingMode = 'Live' AND LEN(ISNULL(Endpoint, '')) > 0
           AND TimeoutSeconds BETWEEN 1 AND 300 THEN 'CONFIGURADO'
      ELSE 'REVISAR ANTES DE LIVE' END AS ConfigState
FROM methods
WHERE MethodName IN ('Proc_Contrapartidas', 'Proc_Transacciones', 'RegistrarRespuestaTransaccion')
ORDER BY MethodName;

-- Validar manualmente que existen exactamente tres métodos, sus endpoints NO son
-- localhost/test y sus mappings publicados están homologados.
