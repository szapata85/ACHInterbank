-- Ajuste de catálogo ReturnReasons para módulo de devoluciones ACH
-- Agrega bandera de uso en devoluciones y marca causales autorizadas (Rxx)

ALTER TABLE ReturnReasons
ADD COLUMN IF NOT EXISTS IsForReturn BOOLEAN NOT NULL DEFAULT FALSE;

UPDATE ReturnReasons
SET IsForReturn = CASE
    WHEN Code IN (
        'R01','R02','R03','R04','R06','R07','R08','R09','R10','R12','R14','R15','R16','R17','R20','R23','R29','R13','R32','R33','R35'
    ) THEN TRUE
    ELSE FALSE
END;
