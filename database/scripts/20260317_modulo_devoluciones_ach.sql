-- Módulo de Devoluciones ACH (NACHA-M)
-- Script referencial para SQL Server/PostgreSQL (ajustar tipos según motor)

CREATE TABLE IF NOT EXISTS Ciclos_Operativos_Recibidos (
    id_ciclo            VARCHAR(64) PRIMARY KEY,
    id_camara           INT NOT NULL,
    fecha_proceso       DATE NOT NULL,
    hora_corte          TIME NOT NULL,
    creado_en           TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS Transacciones_Recibidas (
    id_transaccion_recibida      BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_ciclo                     VARCHAR(64) NOT NULL,
    trace_original               VARCHAR(15) NOT NULL,
    codigo_entidad_origen        VARCHAR(8) NOT NULL,
    codigo_entidad_destino       VARCHAR(8) NOT NULL,
    cuenta_origen                VARCHAR(17) NOT NULL,
    cuenta_destino               VARCHAR(17) NOT NULL,
    monto                        NUMERIC(18,2) NOT NULL,
    es_prenotificacion           BOOLEAN NOT NULL DEFAULT FALSE,
    codigo_transaccion           VARCHAR(2) NOT NULL,
    referencia                   VARCHAR(80) NULL,
    estado                       VARCHAR(20) NOT NULL DEFAULT 'RECIBIDA',
    creado_en                    TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_trx_recibidas_ciclo FOREIGN KEY (id_ciclo) REFERENCES Ciclos_Operativos_Recibidos(id_ciclo)
);

CREATE TABLE IF NOT EXISTS Devoluciones_Generadas (
    id_devolucion                BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    id_transaccion_recibida      BIGINT NOT NULL,
    id_ciclo_devolucion          VARCHAR(64) NOT NULL,
    codigo_error_rxx             VARCHAR(4) NOT NULL,
    secuencia_nueva              VARCHAR(15) NOT NULL,
    secuencia_original           VARCHAR(15) NOT NULL,
    codigo_entidad_receptora     VARCHAR(8) NOT NULL,
    codigo_entidad_originadora   VARCHAR(8) NOT NULL,
    monto_devolucion             NUMERIC(18,2) NOT NULL,
    nombre_archivo_ret           VARCHAR(120) NOT NULL,
    generado_en                  TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_dev_trx FOREIGN KEY (id_transaccion_recibida) REFERENCES Transacciones_Recibidas(id_transaccion_recibida),
    CONSTRAINT fk_dev_ciclo FOREIGN KEY (id_ciclo_devolucion) REFERENCES Ciclos_Operativos_Recibidos(id_ciclo),
    CONSTRAINT uq_dev_trx UNIQUE (id_transaccion_recibida)
);

CREATE INDEX IF NOT EXISTS ix_trx_recibidas_ciclo ON Transacciones_Recibidas(id_ciclo);
CREATE INDEX IF NOT EXISTS ix_devoluciones_ciclo ON Devoluciones_Generadas(id_ciclo_devolucion);
CREATE INDEX IF NOT EXISTS ix_devoluciones_rxx ON Devoluciones_Generadas(codigo_error_rxx);
