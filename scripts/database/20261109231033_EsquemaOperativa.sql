START TRANSACTION;
CREATE TABLE `parada` (
    `parada_id` varchar(36) NOT NULL,
    `nombre` varchar(100) NOT NULL,
    PRIMARY KEY (`parada_id`)
);

CREATE TABLE `tramo` (
    `tramo_id` varchar(36) NOT NULL,
    `origen_id` varchar(36) NOT NULL,
    `destino_id` varchar(36) NOT NULL,
    PRIMARY KEY (`tramo_id`),
    CONSTRAINT `FK_tramo_parada_destino_id` FOREIGN KEY (`destino_id`) REFERENCES `parada` (`parada_id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_tramo_parada_origen_id` FOREIGN KEY (`origen_id`) REFERENCES `parada` (`parada_id`) ON DELETE RESTRICT
);

CREATE TABLE `servicio` (
    `servicio_id` varchar(36) NOT NULL,
    `tramo_id` varchar(36) NOT NULL,
    `fecha` date NOT NULL,
    `hora_salida` time NOT NULL,
    `hora_llegada` time NOT NULL,
    `capacidad` int NOT NULL,
    `politica_asiento` varchar(24) NOT NULL,
    PRIMARY KEY (`servicio_id`),
    CONSTRAINT `FK_servicio_tramo_tramo_id` FOREIGN KEY (`tramo_id`) REFERENCES `tramo` (`tramo_id`) ON DELETE RESTRICT
);

CREATE TABLE `tarifa` (
    `tarifa_id` varchar(36) NOT NULL,
    `tramo_id` varchar(36) NOT NULL,
    `importe` decimal(18,2) NOT NULL,
    `vigente_desde` date NOT NULL,
    `vigente_hasta` date NOT NULL,
    PRIMARY KEY (`tarifa_id`),
    CONSTRAINT `FK_tarifa_tramo_tramo_id` FOREIGN KEY (`tramo_id`) REFERENCES `tramo` (`tramo_id`) ON DELETE RESTRICT
);

CREATE INDEX `IX_servicio_tramo_id` ON `servicio` (`tramo_id`);

CREATE INDEX `IX_tarifa_tramo_id` ON `tarifa` (`tramo_id`);

CREATE INDEX `IX_tramo_destino_id` ON `tramo` (`destino_id`);

CREATE INDEX `IX_tramo_origen_id` ON `tramo` (`origen_id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261109231033_EsquemaOperativa', '10.0.11');

COMMIT;

