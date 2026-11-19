START TRANSACTION;
ALTER TABLE `contador_entidad` DROP CONSTRAINT `ck_contador_prefijo`;

ALTER TABLE `contador_entidad` ADD CONSTRAINT `ck_contador_prefijo` CHECK (prefijo IN ('US-', 'RO-', 'SA-', 'IC-', 'PG-', 'CO-', 'CS-', 'TI-'));

INSERT INTO `contador_entidad` (`prefijo`, `ultimo`)
VALUES ('PG-', 0);
SELECT ROW_COUNT();

INSERT INTO `contador_entidad` (`prefijo`, `ultimo`)
VALUES ('CO-', 0);
SELECT ROW_COUNT();

INSERT INTO `contador_entidad` (`prefijo`, `ultimo`)
VALUES ('CS-', 0);
SELECT ROW_COUNT();

INSERT INTO `contador_entidad` (`prefijo`, `ultimo`)
VALUES ('TI-', 0);
SELECT ROW_COUNT();


CREATE TABLE `pago` (
    `pago_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `intento_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `importe_esperado` decimal(10,2) NOT NULL,
    `importe_pagado` decimal(10,2) NOT NULL,
    `moneda` varchar(3) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `estado` varchar(24) NOT NULL,
    `clave_creacion` char(36) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `clave_devolucion` char(36) CHARACTER SET ascii COLLATE ascii_bin NULL,
    `orden_proveedor_id` varchar(256) NULL,
    `checkout_url` varchar(2048) NULL,
    `creado_utc` datetime(6) NOT NULL,
    `actualizado_utc` datetime(6) NOT NULL,
    `ultima_verificacion_utc` datetime(6) NULL,
    `devolucion_solicitada_utc` datetime(6) NULL,
    `devolucion_confirmada_utc` datetime(6) NULL,
    PRIMARY KEY (`pago_id`),
    CONSTRAINT `ak_pago_intento` UNIQUE (`pago_id`, `intento_id`),
    CONSTRAINT `ck_pago_estado` CHECK (estado IN ('Pendiente', 'Desconocido', 'Aprobado', 'Rechazado', 'DevolucionPendiente', 'Devuelto')),
    CONSTRAINT `ck_pago_id` CHECK (pago_id REGEXP '^PG-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'),
    CONSTRAINT `ck_pago_importes` CHECK (importe_esperado > 0 AND importe_pagado >= 0 AND importe_pagado <= importe_esperado),
    CONSTRAINT `ck_pago_moneda` CHECK (moneda = 'UYU'),
    CONSTRAINT `fk_pago_intento` FOREIGN KEY (`intento_id`) REFERENCES `intento_compra` (`intento_id`) ON DELETE RESTRICT
);

CREATE TABLE `compra` (
    `compra_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `intento_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `pago_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `importe_total` decimal(10,2) NOT NULL,
    `confirmado_utc` datetime(6) NOT NULL,
    PRIMARY KEY (`compra_id`),
    CONSTRAINT `ak_compra_intento_pago` UNIQUE (`compra_id`, `intento_id`, `pago_id`),
    CONSTRAINT `ck_compra_id` CHECK (compra_id REGEXP '^CO-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'),
    CONSTRAINT `ck_compra_importe` CHECK (importe_total > 0),
    CONSTRAINT `fk_compra_intento` FOREIGN KEY (`intento_id`) REFERENCES `intento_compra` (`intento_id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_compra_pago_intento` FOREIGN KEY (`pago_id`, `intento_id`) REFERENCES `pago` (`pago_id`, `intento_id`) ON DELETE RESTRICT
);

CREATE TABLE `tramo_pago` (
    `pago_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `orden` int NOT NULL,
    `intento_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `importe` decimal(10,2) NOT NULL,
    PRIMARY KEY (`pago_id`, `orden`),
    CONSTRAINT `ck_tramo_pago_importe` CHECK (importe >= 0),
    CONSTRAINT `ck_tramo_pago_orden` CHECK (orden >= 1),
    CONSTRAINT `fk_tramo_pago_pago_intento` FOREIGN KEY (`pago_id`, `intento_id`) REFERENCES `pago` (`pago_id`, `intento_id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_tramo_pago_seleccion` FOREIGN KEY (`intento_id`, `orden`) REFERENCES `tramo_intento_compra` (`intento_id`, `orden`) ON DELETE RESTRICT
);

CREATE TABLE `compra_servicio` (
    `compra_servicio_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `compra_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `intento_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `pago_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `orden` int NOT NULL,
    `importe` decimal(10,2) NOT NULL,
    PRIMARY KEY (`compra_servicio_id`),
    CONSTRAINT `ck_compra_servicio_id` CHECK (compra_servicio_id REGEXP '^CS-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'),
    CONSTRAINT `ck_compra_servicio_importe` CHECK (importe >= 0),
    CONSTRAINT `ck_compra_servicio_orden` CHECK (orden >= 1),
    CONSTRAINT `fk_compra_servicio_compra_intento_pago` FOREIGN KEY (`compra_id`, `intento_id`, `pago_id`) REFERENCES `compra` (`compra_id`, `intento_id`, `pago_id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_compra_servicio_tramo_pago` FOREIGN KEY (`pago_id`, `orden`) REFERENCES `tramo_pago` (`pago_id`, `orden`) ON DELETE RESTRICT
);

CREATE TABLE `ticket` (
    `ticket_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `compra_servicio_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `qr_opaco` varchar(128) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `emitido_utc` datetime(6) NOT NULL,
    PRIMARY KEY (`ticket_id`),
    CONSTRAINT `ck_ticket_id` CHECK (ticket_id REGEXP '^TI-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'),
    CONSTRAINT `fk_ticket_compra_servicio` FOREIGN KEY (`compra_servicio_id`) REFERENCES `compra_servicio` (`compra_servicio_id`) ON DELETE RESTRICT
);

CREATE INDEX `IX_compra_pago_id_intento_id` ON `compra` (`pago_id`, `intento_id`);

CREATE UNIQUE INDEX `uq_compra_intento` ON `compra` (`intento_id`);

CREATE UNIQUE INDEX `uq_compra_pago` ON `compra` (`pago_id`);

CREATE INDEX `IX_compra_servicio_compra_id_intento_id_pago_id` ON `compra_servicio` (`compra_id`, `intento_id`, `pago_id`);

CREATE INDEX `IX_compra_servicio_pago_id_orden` ON `compra_servicio` (`pago_id`, `orden`);

CREATE UNIQUE INDEX `uq_compra_servicio_orden` ON `compra_servicio` (`compra_id`, `orden`);

CREATE INDEX `ix_pago_intento_creado` ON `pago` (`intento_id`, `creado_utc`);

CREATE UNIQUE INDEX `uq_pago_clave_creacion` ON `pago` (`clave_creacion`);

CREATE UNIQUE INDEX `uq_pago_clave_devolucion` ON `pago` (`clave_devolucion`);

CREATE UNIQUE INDEX `uq_pago_orden_proveedor` ON `pago` (`orden_proveedor_id`);

CREATE UNIQUE INDEX `uq_ticket_compra_servicio` ON `ticket` (`compra_servicio_id`);

CREATE UNIQUE INDEX `uq_ticket_qr_opaco` ON `ticket` (`qr_opaco`);

CREATE INDEX `IX_tramo_pago_intento_id_orden` ON `tramo_pago` (`intento_id`, `orden`);

CREATE INDEX `IX_tramo_pago_pago_id_intento_id` ON `tramo_pago` (`pago_id`, `intento_id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261117020122_PagosComprasTickets', '10.0.11');

COMMIT;

