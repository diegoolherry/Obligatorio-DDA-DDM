CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) NOT NULL,
    `ProductVersion` varchar(32) NOT NULL,
    PRIMARY KEY (`MigrationId`)
);

START TRANSACTION;
CREATE TABLE `contador_entidad` (
    `prefijo` varchar(3) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `ultimo` int NOT NULL,
    PRIMARY KEY (`prefijo`),
    CONSTRAINT `ck_contador_prefijo` CHECK (prefijo IN ('US-', 'RO-')),
    CONSTRAINT `ck_contador_rango` CHECK (ultimo BETWEEN 0 AND 50000)
);

CREATE TABLE `rol` (
    `rol_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `nombre` varchar(13) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    PRIMARY KEY (`rol_id`),
    CONSTRAINT `ck_rol_id` CHECK (rol_id REGEXP '^RO-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'),
    CONSTRAINT `ck_rol_nombre` CHECK (nombre IN ('Pasajero', 'Cobrador', 'Administrador'))
);

CREATE TABLE `usuario` (
    `usuario_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `identificador_publico` varchar(43) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `correo` varchar(254) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    `contrasena` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    `activo` tinyint(1) NOT NULL,
    PRIMARY KEY (`usuario_id`),
    CONSTRAINT `ck_usuario_activo` CHECK (activo IN (0, 1)),
    CONSTRAINT `ck_usuario_id` CHECK (usuario_id REGEXP '^US-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$')
);

CREATE TABLE `sesion` (
    `huella` char(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `usuario_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `creada_utc` datetime(6) NOT NULL,
    `vence_utc` datetime(6) NOT NULL,
    `revocada_utc` datetime(6) NULL,
    PRIMARY KEY (`huella`),
    CONSTRAINT `ck_sesion_vence` CHECK (vence_utc = DATE_ADD(creada_utc, INTERVAL 7 DAY)),
    CONSTRAINT `fk_sesion_usuario` FOREIGN KEY (`usuario_id`) REFERENCES `usuario` (`usuario_id`) ON DELETE RESTRICT
);

CREATE TABLE `usuario_rol` (
    `usuario_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `rol_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    PRIMARY KEY (`usuario_id`, `rol_id`),
    CONSTRAINT `fk_usuario_rol_rol` FOREIGN KEY (`rol_id`) REFERENCES `rol` (`rol_id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_usuario_rol_usuario` FOREIGN KEY (`usuario_id`) REFERENCES `usuario` (`usuario_id`) ON DELETE RESTRICT
);

CREATE UNIQUE INDEX `uq_rol_nombre` ON `rol` (`nombre`);

CREATE INDEX `IX_sesion_usuario_id` ON `sesion` (`usuario_id`);

CREATE UNIQUE INDEX `uq_usuario_correo` ON `usuario` (`correo`);

CREATE UNIQUE INDEX `uq_usuario_publico` ON `usuario` (`identificador_publico`);

CREATE INDEX `IX_usuario_rol_rol_id` ON `usuario_rol` (`rol_id`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261109202055_EsquemaAutenticacion', '10.0.9');

COMMIT;
