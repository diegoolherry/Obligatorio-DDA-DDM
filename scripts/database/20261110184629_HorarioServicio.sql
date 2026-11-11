START TRANSACTION;
ALTER TABLE `servicio` ADD `salida` datetime(6) NULL;

ALTER TABLE `servicio` ADD `llegada` datetime(6) NULL;

UPDATE servicio SET salida = TIMESTAMP(fecha, hora_salida), llegada = TIMESTAMP(fecha, hora_llegada) + INTERVAL IF(hora_llegada <= hora_salida, 1, 0) DAY;

ALTER TABLE `servicio` MODIFY `salida` datetime(6) NOT NULL;

ALTER TABLE `servicio` MODIFY `llegada` datetime(6) NOT NULL;

ALTER TABLE `servicio` DROP COLUMN `fecha`;

ALTER TABLE `servicio` DROP COLUMN `hora_llegada`;

ALTER TABLE `servicio` DROP COLUMN `hora_salida`;

ALTER TABLE `servicio` ADD CONSTRAINT `ck_servicio_llegada` CHECK (llegada > salida);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261110184629_HorarioServicio', '10.0.11');

COMMIT;
