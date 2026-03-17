-- Ajuste de catálogo ReturnReasons para módulo de devoluciones ACH (SQL Server)

IF COL_LENGTH('ReturnReasons', 'IsForReturn') IS NULL
BEGIN
    ALTER TABLE ReturnReasons
    ADD IsForReturn BIT NOT NULL CONSTRAINT DF_ReturnReasons_IsForReturn DEFAULT (0);
END

UPDATE ReturnReasons
SET IsForReturn = CASE
    WHEN Code IN ('R01','R02','R03','R04','R06','R07','R08','R09','R10','R12','R13','R14','R15','R16','R17','R20','R23','R29','R30') THEN 1
    ELSE 0
END;
