START TRANSACTION;
ALTER TABLE `servicio` ADD `salida_catalogo_id` varchar(36) NULL;

CREATE TABLE `bloque_horario` (
    `bloque_id` varchar(36) NOT NULL,
    `clave` varchar(16) NOT NULL,
    `nombre` varchar(100) NOT NULL,
    `empresa` varchar(100) NOT NULL,
    `vigente_desde` date NULL,
    `vigente_hasta` date NULL,
    `fuente_url` varchar(200) NOT NULL,
    PRIMARY KEY (`bloque_id`),
    CONSTRAINT `ck_bloque_vigencia` CHECK (vigente_hasta IS NULL OR vigente_desde IS NULL OR vigente_hasta >= vigente_desde)
);

CREATE TABLE `feriado` (
    `fecha` date NOT NULL,
    `nombre` varchar(100) NOT NULL,
    PRIMARY KEY (`fecha`)
);

CREATE TABLE `excepcion_calendario` (
    `excepcion_id` varchar(36) NOT NULL,
    `bloque_id` varchar(36) NULL,
    `desde` date NOT NULL,
    `hasta` date NOT NULL,
    `tipo` varchar(24) NOT NULL,
    `motivo` varchar(200) NOT NULL,
    PRIMARY KEY (`excepcion_id`),
    CONSTRAINT `ck_excepcion_rango` CHECK (hasta >= desde),
    CONSTRAINT `FK_excepcion_calendario_bloque_horario_bloque_id` FOREIGN KEY (`bloque_id`) REFERENCES `bloque_horario` (`bloque_id`) ON DELETE CASCADE
);

CREATE TABLE `ruta` (
    `ruta_id` varchar(36) NOT NULL,
    `bloque_id` varchar(36) NOT NULL,
    `clave` varchar(16) NOT NULL,
    `familia` varchar(100) NOT NULL,
    `nombre` varchar(100) NOT NULL,
    PRIMARY KEY (`ruta_id`),
    CONSTRAINT `FK_ruta_bloque_horario_bloque_id` FOREIGN KEY (`bloque_id`) REFERENCES `bloque_horario` (`bloque_id`) ON DELETE RESTRICT
);

CREATE TABLE `parada_ruta` (
    `ruta_id` varchar(36) NOT NULL,
    `orden` int NOT NULL,
    `parada_id` varchar(36) NOT NULL,
    PRIMARY KEY (`ruta_id`, `orden`),
    CONSTRAINT `ck_parada_ruta_orden` CHECK (orden >= 1),
    CONSTRAINT `FK_parada_ruta_parada_parada_id` FOREIGN KEY (`parada_id`) REFERENCES `parada` (`parada_id`) ON DELETE RESTRICT,
    CONSTRAINT `FK_parada_ruta_ruta_ruta_id` FOREIGN KEY (`ruta_id`) REFERENCES `ruta` (`ruta_id`) ON DELETE CASCADE
);

CREATE TABLE `salida_catalogo` (
    `salida_id` varchar(36) NOT NULL,
    `ruta_id` varchar(36) NOT NULL,
    `dias` varchar(24) NOT NULL,
    `codigos` varchar(16) NULL,
    `nota` varchar(200) NULL,
    PRIMARY KEY (`salida_id`),
    CONSTRAINT `FK_salida_catalogo_ruta_ruta_id` FOREIGN KEY (`ruta_id`) REFERENCES `ruta` (`ruta_id`) ON DELETE CASCADE
);

CREATE TABLE `hora_salida` (
    `salida_id` varchar(36) NOT NULL,
    `orden` int NOT NULL,
    `hora` time(0) NOT NULL,
    PRIMARY KEY (`salida_id`, `orden`),
    CONSTRAINT `ck_hora_salida_orden` CHECK (orden >= 1),
    CONSTRAINT `FK_hora_salida_salida_catalogo_salida_id` FOREIGN KEY (`salida_id`) REFERENCES `salida_catalogo` (`salida_id`) ON DELETE CASCADE
);

CREATE UNIQUE INDEX `ux_servicio_generado` ON `servicio` (`salida_catalogo_id`, `tramo_id`, `salida`);

CREATE UNIQUE INDEX `ux_bloque_clave` ON `bloque_horario` (`clave`);

CREATE INDEX `IX_excepcion_calendario_bloque_id` ON `excepcion_calendario` (`bloque_id`);

CREATE INDEX `IX_parada_ruta_parada_id` ON `parada_ruta` (`parada_id`);

CREATE INDEX `IX_ruta_bloque_id` ON `ruta` (`bloque_id`);

CREATE UNIQUE INDEX `ux_ruta_clave` ON `ruta` (`clave`);

CREATE INDEX `IX_salida_catalogo_ruta_id` ON `salida_catalogo` (`ruta_id`);

ALTER TABLE `servicio` ADD CONSTRAINT `FK_servicio_salida_catalogo_salida_catalogo_id` FOREIGN KEY (`salida_catalogo_id`) REFERENCES `salida_catalogo` (`salida_id`) ON DELETE RESTRICT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261111163728_CatalogoHorarios', '10.0.11');

COMMIT;
