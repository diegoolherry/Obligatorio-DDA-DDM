-- Ejecutar UNA sola vez en Workbench con la cuenta administradora local.
-- Da permiso de esquema a las cuentas de la aplicación sobre las bases locales descartables
-- (decisión del equipo), para que las migraciones de EF Core y reset-local-db.ps1 funcionen.
-- Las bases y cuentas ya existen (prepare-local-auth.sql). No contiene contraseñas.
GRANT CREATE, DROP, ALTER, INDEX, REFERENCES ON `berrutilocal`.* TO 'berruti_app'@'localhost';
GRANT CREATE, DROP, ALTER, INDEX, REFERENCES ON `berrutiauthtests`.* TO 'berruti_test'@'localhost';

SHOW GRANTS FOR 'berruti_app'@'localhost';
SHOW GRANTS FOR 'berruti_test'@'localhost';
