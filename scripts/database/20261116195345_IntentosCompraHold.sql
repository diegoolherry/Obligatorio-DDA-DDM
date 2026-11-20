START TRANSACTION;
ALTER TABLE `contador_entidad` DROP CONSTRAINT `ck_contador_prefijo`;

CREATE TABLE `intento_compra` (
    `intento_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `usuario_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `creado_utc` datetime(6) NOT NULL,
    `hold_vence_utc` datetime(6) NOT NULL,
    PRIMARY KEY (`intento_id`),
    CONSTRAINT `ck_intento_compra_plazo` CHECK (hold_vence_utc > creado_utc),
    CONSTRAINT `fk_intento_compra_usuario` FOREIGN KEY (`usuario_id`) REFERENCES `usuario` (`usuario_id`) ON DELETE RESTRICT
);

CREATE TABLE `tramo_intento_compra` (
    `intento_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `orden` int NOT NULL,
    `servicio_id` varchar(36) NOT NULL,
    `tramo_id` varchar(36) NOT NULL,
    `origen_id` varchar(36) NOT NULL,
    `origen_nombre` varchar(100) NOT NULL,
    `destino_id` varchar(36) NOT NULL,
    `destino_nombre` varchar(100) NOT NULL,
    `salida` datetime(6) NOT NULL,
    `llegada` datetime(6) NOT NULL,
    `importe` decimal(18,2) NOT NULL,
    `politica_asiento` varchar(24) NOT NULL,
    `numero_asiento` int NULL,
    `salida_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NULL,
    `orden_embarque` int NULL,
    `orden_desembarque` int NULL,
    PRIMARY KEY (`intento_id`, `orden`),
    CONSTRAINT `ck_tramo_intento_asiento` CHECK ((numero_asiento IS NULL AND salida_id IS NULL AND orden_embarque IS NULL AND orden_desembarque IS NULL) OR (numero_asiento IS NOT NULL AND salida_id IS NOT NULL AND orden_embarque IS NOT NULL AND orden_desembarque IS NOT NULL AND orden_embarque < orden_desembarque)),
    CONSTRAINT `ck_tramo_intento_importe` CHECK (importe >= 0),
    CONSTRAINT `ck_tramo_intento_llegada` CHECK (llegada > salida),
    CONSTRAINT `ck_tramo_intento_orden` CHECK (orden >= 1),
    CONSTRAINT `fk_tramo_intento_intento` FOREIGN KEY (`intento_id`) REFERENCES `intento_compra` (`intento_id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_tramo_intento_salida` FOREIGN KEY (`salida_id`) REFERENCES `salida` (`salida_id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_tramo_intento_servicio` FOREIGN KEY (`servicio_id`) REFERENCES `servicio` (`servicio_id`) ON DELETE RESTRICT
);

CREATE TABLE `hold_asiento` (
    `intento_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `orden` int NOT NULL,
    `salida_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `numero_asiento` int NOT NULL,
    `orden_embarque` int NOT NULL,
    `orden_desembarque` int NOT NULL,
    PRIMARY KEY (`intento_id`, `orden`),
    CONSTRAINT `ck_hold_asiento_intervalo` CHECK (orden_embarque >= 1 AND orden_embarque < orden_desembarque),
    CONSTRAINT `ck_hold_asiento_numero` CHECK (numero_asiento >= 1),
    CONSTRAINT `fk_hold_asiento_desembarque` FOREIGN KEY (`salida_id`, `orden_desembarque`) REFERENCES `parada_salida` (`salida_id`, `orden`) ON DELETE RESTRICT,
    CONSTRAINT `fk_hold_asiento_embarque` FOREIGN KEY (`salida_id`, `orden_embarque`) REFERENCES `parada_salida` (`salida_id`, `orden`) ON DELETE RESTRICT,
    CONSTRAINT `fk_hold_asiento_inventario` FOREIGN KEY (`salida_id`, `numero_asiento`) REFERENCES `asiento_salida` (`salida_id`, `numero_asiento`) ON DELETE RESTRICT,
    CONSTRAINT `fk_hold_asiento_seleccion` FOREIGN KEY (`intento_id`, `orden`) REFERENCES `tramo_intento_compra` (`intento_id`, `orden`) ON DELETE RESTRICT
);

ALTER TABLE `contador_entidad` ADD CONSTRAINT `ck_contador_prefijo` CHECK (prefijo IN ('US-', 'RO-', 'SA-', 'IC-'));

INSERT INTO `contador_entidad` (`prefijo`, `ultimo`)
VALUES ('IC-', 0);
SELECT ROW_COUNT();


CREATE INDEX `ix_hold_asiento_inventario` ON `hold_asiento` (`salida_id`, `numero_asiento`);

CREATE INDEX `IX_hold_asiento_salida_id_orden_desembarque` ON `hold_asiento` (`salida_id`, `orden_desembarque`);

CREATE INDEX `IX_hold_asiento_salida_id_orden_embarque` ON `hold_asiento` (`salida_id`, `orden_embarque`);

CREATE INDEX `IX_intento_compra_usuario_id` ON `intento_compra` (`usuario_id`);

CREATE INDEX `IX_tramo_intento_compra_salida_id` ON `tramo_intento_compra` (`salida_id`);

CREATE INDEX `ix_tramo_intento_servicio` ON `tramo_intento_compra` (`servicio_id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261116195345_IntentosCompraHold', '10.0.11');

COMMIT;
