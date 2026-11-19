START TRANSACTION;
ALTER TABLE `contador_entidad` DROP CONSTRAINT `ck_contador_prefijo`;

ALTER TABLE `servicio` ADD `salida_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NULL;

CREATE TABLE `salida` (
    `salida_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `fecha` date NOT NULL,
    PRIMARY KEY (`salida_id`),
    CONSTRAINT `ck_salida_id` CHECK (salida_id REGEXP '^SA-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$')
);

CREATE TABLE `asiento_salida` (
    `salida_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `numero_asiento` int NOT NULL,
    PRIMARY KEY (`salida_id`, `numero_asiento`),
    CONSTRAINT `ck_asiento_salida_numero` CHECK (numero_asiento >= 1),
    CONSTRAINT `fk_asiento_salida_salida` FOREIGN KEY (`salida_id`) REFERENCES `salida` (`salida_id`) ON DELETE RESTRICT
);

CREATE INDEX `IX_servicio_salida_id` ON `servicio` (`salida_id`);

ALTER TABLE `contador_entidad` ADD CONSTRAINT `ck_contador_prefijo` CHECK (prefijo IN ('US-', 'RO-', 'SA-'));

ALTER TABLE `servicio` ADD CONSTRAINT `fk_servicio_salida` FOREIGN KEY (`salida_id`) REFERENCES `salida` (`salida_id`) ON DELETE RESTRICT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261116181813_InventarioSalidaDemo', '10.0.11');

COMMIT;
