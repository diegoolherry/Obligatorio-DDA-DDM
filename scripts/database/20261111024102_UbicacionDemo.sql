START TRANSACTION;
CREATE TABLE `cobrador_servicio` (
    `usuario_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `servicio_id` varchar(36) NOT NULL,
    PRIMARY KEY (`usuario_id`, `servicio_id`),
    CONSTRAINT `fk_cobrador_servicio_servicio` FOREIGN KEY (`servicio_id`) REFERENCES `servicio` (`servicio_id`) ON DELETE CASCADE,
    CONSTRAINT `fk_cobrador_servicio_usuario` FOREIGN KEY (`usuario_id`) REFERENCES `usuario` (`usuario_id`) ON DELETE RESTRICT
);

CREATE TABLE `posicion_recibida` (
    `posicion_id` varchar(36) NOT NULL,
    `usuario_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `servicio_id` varchar(36) NOT NULL,
    `latitud` decimal(9,6) NOT NULL,
    `longitud` decimal(9,6) NOT NULL,
    `recibida_utc` datetime(6) NOT NULL,
    PRIMARY KEY (`posicion_id`),
    CONSTRAINT `ck_posicion_latitud` CHECK (latitud BETWEEN -90 AND 90),
    CONSTRAINT `ck_posicion_longitud` CHECK (longitud BETWEEN -180 AND 180),
    CONSTRAINT `fk_posicion_asignacion` FOREIGN KEY (`usuario_id`, `servicio_id`) REFERENCES `cobrador_servicio` (`usuario_id`, `servicio_id`) ON DELETE CASCADE
);

CREATE INDEX `IX_cobrador_servicio_servicio_id` ON `cobrador_servicio` (`servicio_id`);

CREATE INDEX `IX_posicion_recibida_usuario_id_servicio_id` ON `posicion_recibida` (`usuario_id`, `servicio_id`);

CREATE INDEX `ix_posicion_servicio_recibida` ON `posicion_recibida` (`servicio_id`, `recibida_utc`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261111024102_UbicacionDemo', '10.0.11');

COMMIT;
