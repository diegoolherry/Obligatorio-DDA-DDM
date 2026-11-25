START TRANSACTION;
ALTER TABLE `ticket` ADD `usado_utc` datetime(6) NULL;

ALTER TABLE `ticket` ADD `validado_por_usuario_id` varchar(8) CHARACTER SET ascii COLLATE ascii_bin NULL;

CREATE INDEX `IX_ticket_validado_por_usuario_id` ON `ticket` (`validado_por_usuario_id`);

ALTER TABLE `ticket` ADD CONSTRAINT `ck_ticket_uso` CHECK ((usado_utc IS NULL AND validado_por_usuario_id IS NULL) OR (usado_utc IS NOT NULL AND validado_por_usuario_id IS NOT NULL));

ALTER TABLE `ticket` ADD CONSTRAINT `fk_ticket_validado_por_usuario` FOREIGN KEY (`validado_por_usuario_id`) REFERENCES `usuario` (`usuario_id`) ON DELETE RESTRICT;

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20261124124915_ConsumoTicket', '10.0.11');

COMMIT;

