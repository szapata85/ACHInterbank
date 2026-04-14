-- Ajuste de compatibilidad:
-- AchCycle.Id es nvarchar(40) (hash SHA1 hex de 40 chars), por lo que
-- las tablas de despacho a contrapartida deben aceptar el mismo tamaño.

IF COL_LENGTH('ContrapartidaDispatchItems', 'AchCycleId') IS NOT NULL
BEGIN
    ALTER TABLE ContrapartidaDispatchItems ALTER COLUMN AchCycleId NVARCHAR(40) NOT NULL;
END

IF COL_LENGTH('ContrapartidaDispatchBatches', 'AchCycleId') IS NOT NULL
BEGIN
    ALTER TABLE ContrapartidaDispatchBatches ALTER COLUMN AchCycleId NVARCHAR(40) NOT NULL;
END
