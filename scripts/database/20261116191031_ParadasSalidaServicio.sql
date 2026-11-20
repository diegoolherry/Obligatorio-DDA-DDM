START TRANSACTION;
ALTER TABLE `servicio` ADD `orden_desembarque` int NULL;

ALTER TABLE `servicio` ADD `orden_embarque` int NULL;

CREATE TABLE `parada_salida` (
    `salida_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    `orden` int NOT NULL,
    `parada_id` varchar(36) NOT NULL,
    PRIMARY KEY (`salida_id`, `orden`),
    CONSTRAINT `ck_parada_salida_orden` CHECK (orden >= 1),
    CONSTRAINT `fk_parada_salida_parada` FOREIGN KEY (`parada_id`) REFERENCES `parada` (`parada_id`) ON DELETE RESTRICT,
    CONSTRAINT `fk_parada_salida_salida` FOREIGN KEY (`salida_id`) REFERENCES `salida` (`salida_id`) ON DELETE RESTRICT
);

CREATE INDEX `IX_servicio_salida_id_orden_desembarque` ON `servicio` (`salida_id`, `orden_desembarque`);

CREATE INDEX `IX_servicio_salida_id_orden_embarque` ON `servicio` (`salida_id`, `orden_embarque`);

ALTER TABLE `servicio` ADD CONSTRAINT `ck_servicio_rango_paradas` CHECK (((orden_embarque IS NULL AND orden_desembarque IS NULL) OR (salida_id IS NOT NULL AND orden_embarque IS NOT NULL AND orden_desembarque IS NOT NULL AND orden_embarque < orden_desembarque)));

CREATE INDEX `IX_parada_salida_parada_id` ON `parada_salida` (`parada_id`);

ALTER TABLE `servicio` ADD CONSTRAINT `fk_servicio_parada_desembarque` FOREIGN KEY (`salida_id`, `orden_desembarque`) REFERENCES `parada_salida` (`salida_id`, `orden`) ON DELETE RESTRICT;

ALTER TABLE `servicio` ADD CONSTRAINT `fk_servicio_parada_embarque` FOREIGN KEY (`salida_id`, `orden_embarque`) REFERENCES `parada_salida` (`salida_id`, `orden`) ON DELETE RESTRICT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261116191031_ParadasSalidaServicio', '10.0.11');

COMMIT;
