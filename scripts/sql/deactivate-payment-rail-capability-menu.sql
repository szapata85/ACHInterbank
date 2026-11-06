-- ACH Interbank - corrección puntual de navegación para una BD ya inicializada.
-- Ejecución manual OPCIONAL: usar solo si el seed no actualizó los registros existentes.
-- No modifica permisos, roles, funciones, ni elimina registros.
-- Ejecutar contra la base ACH Interbank correcta; verificar antes el SELECT.
BEGIN TRANSACTION;

SELECT [Id], [Label], [Route], [IsActive]
FROM [dbo].[MenuItems]
WHERE LOWER(LTRIM(RTRIM([Route]))) LIKE '/payment-rail-capability-registry%';

UPDATE [dbo].[MenuItems]
SET [IsActive] = 0
WHERE LOWER(LTRIM(RTRIM([Route]))) IN
    ('/payment-rail-capability-registry', '/payment-rail-capability-registry/');

SELECT [Id], [Label], [Route], [IsActive]
FROM [dbo].[MenuItems]
WHERE LOWER(LTRIM(RTRIM([Route]))) IN
    ('/payment-rail-capability-registry', '/payment-rail-capability-registry/');

COMMIT TRANSACTION;
