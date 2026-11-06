-- ACH Interbank - reparación acotada de orden/etiqueta del menú existente.
-- Solo para bases SQL Server ya instaladas cuando no se desea ejecutar todo el seed.
-- Revisar los registros primero. No modifica permisos, roles, hijos ni estructuras.
-- Ejecutar sobre la base ACHInterbank correcta con copia de seguridad verificada.
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.MenuItems WHERE Id = 1 AND Route = '/dashboard')
    THROW 51001, 'No se encontro el registro canonico Inicio (Id=1). Se cancela sin cambios.', 1;
IF NOT EXISTS (SELECT 1 FROM dbo.MenuItems WHERE Id = 10 AND Route IN ('/nacha-security', '/nacha-security/certificates'))
    THROW 51002, 'No se encontro el registro canonico Seguridad de archivos (Id=10). Se cancela sin cambios.', 1;
IF EXISTS (SELECT 1 FROM dbo.MenuItems WHERE Id <> 10 AND Route = '/nacha-security' AND IsActive = 1)
    THROW 51003, 'Existe otra entrada activa para /nacha-security. Revisar duplicados antes de continuar.', 1;
IF EXISTS (SELECT 1 FROM dbo.MenuItems WHERE Id NOT IN (1, 10) AND ParentId IS NULL
    AND IsActive = 1 AND [Order] IN (10, 60))
    THROW 51004, 'Hay otra raiz activa con orden 10 o 60. Usar primero el seed canonico completo.', 1;

UPDATE dbo.MenuItems
SET Label = N'Inicio', [Order] = 10, ParentId = NULL
WHERE Id = 1 AND Route = '/dashboard';

UPDATE dbo.MenuItems
SET Label = N'Seguridad de archivos', Route = '/nacha-security', [Order] = 60,
    ParentId = NULL, IsActive = 1
WHERE Id = 10 AND Route IN ('/nacha-security', '/nacha-security/certificates');

SELECT Id, Label, Route, ParentId, [Order], IsActive
FROM dbo.MenuItems
WHERE Id IN (1, 10)
ORDER BY [Order], Id;

COMMIT TRANSACTION;
