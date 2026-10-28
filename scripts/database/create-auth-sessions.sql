-- SOLO berrutiauthtests, propuesta T2 para revisión MANUAL administrativa.
-- NO ejecutar el archivo completo. Ejecutar UNA sentencia por vez y parar al error.
-- NO crea bases ni grants, NO altera/borrra tablas ni reinicia contadores.
-- Runtime berruti_test tiene DML únicamente: nunca debe ejecutar este DDL.
-- 1. Revisar versión MySQL 8.0.16+ e inventario/definiciones existentes.
SELECT VERSION();

SELECT TABLE_NAME, ENGINE
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'berrutiauthtests'
  AND TABLE_NAME IN ('usuario', 'rol', 'usuario_rol', 'contador_entidad', 'sesion');

-- 2. Si existe sesion: DETENERSE, no repetir/reutilizar/ALTER automáticamente.
-- Las cuatro tablas de fundación deben existir y ser InnoDB. Revisar SHOW CREATE
-- de cada una manualmente; usuario requiere identificador_publico UNIQUE NOT NULL
-- VARCHAR(43) ascii_bin y usuario_id VARCHAR(8) ascii_bin, como foundation actual.
-- Si la fundación es anterior o diferente: DETENERSE y solicitar revisión humana;
-- no aplicar ALTER, reset, seed ni reemplazo implícito. No crear esquema demo.
SHOW CREATE TABLE berrutiauthtests.usuario;

SHOW CREATE TABLE berrutiauthtests.rol;

SHOW CREATE TABLE berrutiauthtests.usuario_rol;

SHOW CREATE TABLE berrutiauthtests.contador_entidad;

-- 3. Solo con inventario aprobado, sesion ausente y sin creador concurrente.
-- DATETIME(6) contiene instantes UTC de aplicación, sin conversión de zona del servidor.
-- Huella SHA256 hex única; nunca almacenar el token Bearer recuperable.
CREATE TABLE berrutiauthtests.sesion (
    huella CHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    usuario_id VARCHAR(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    creada_utc DATETIME(6) NOT NULL,
    vence_utc DATETIME(6) NOT NULL,
    revocada_utc DATETIME(6) NULL,
    PRIMARY KEY (huella),
    CONSTRAINT fk_sesion_usuario FOREIGN KEY (usuario_id)
        REFERENCES berrutiauthtests.usuario (usuario_id) ON DELETE RESTRICT,
    CONSTRAINT ck_sesion_vence CHECK (vence_utc = DATE_ADD(creada_utc, INTERVAL 7 DAY))
) ENGINE=InnoDB;

-- DDL tiene commit implícito: rollback NO deshace CREATE. No hay cuentas/contraseñas
-- ni seed aquí. Su ejecución y compatibilidad con el proveedor siguen SIN verificar.
