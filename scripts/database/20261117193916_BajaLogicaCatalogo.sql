START TRANSACTION;
ALTER TABLE `salida_catalogo` ADD `activo` tinyint(1) NOT NULL DEFAULT TRUE;

ALTER TABLE `ruta` ADD `activo` tinyint(1) NOT NULL DEFAULT TRUE;

ALTER TABLE `bloque_horario` ADD `activo` tinyint(1) NOT NULL DEFAULT TRUE;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261117193916_BajaLogicaCatalogo', '10.0.11');

COMMIT;
