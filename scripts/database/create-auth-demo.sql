-- SOLO berrutilocal existente. Propuesta T6 para revisión MANUAL administrativa.
-- NO ejecutar el archivo completo: seleccionar UNA sentencia y parar al primer error.
-- La aplicación no debe estar ejecutándose; excluir otros escritores/administradores.
-- No reprovisiona bases/cuentas/permisos ni reutiliza objetos. DDL hace commits
-- implícitos: no es atómico y rollback NO deshace creación parcial. No reintentar.
-- 1. Confirmar MySQL auténtico 8.0.16+ con CHECK efectivos; otra versión/producto:
-- DETENERSE. SELECT no impone esta puerta: requiere revisión humana de la versión.
SELECT VERSION();

-- Inventario COMPLETO del esquema (incluye vistas), no solo tablas objetivo.
-- Debe terminar sin errores y devolver CERO filas. Un error no significa vacío.
-- Ante cualquier objeto existente o inesperado, detenerse y pedir revisión.
SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE, ENGINE
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'berrutilocal';

-- 2. Solo con inventario cero y versión confirmados, crear de a UNA sentencia.
-- Las cinco definiciones son las propuestas test existentes, solo cambia la base.
CREATE TABLE berrutilocal.contador_entidad (
    prefijo VARCHAR(3) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    ultimo INT NOT NULL,
    PRIMARY KEY (prefijo),
    CONSTRAINT ck_contador_rango CHECK (ultimo BETWEEN 0 AND 50000),
    CONSTRAINT ck_contador_prefijo CHECK (prefijo IN ('US-', 'RO-'))
) ENGINE=InnoDB;

CREATE TABLE berrutilocal.usuario (
    usuario_id VARCHAR(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    identificador_publico VARCHAR(43) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    correo VARCHAR(254) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    contrasena VARCHAR(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    activo TINYINT(1) NOT NULL,
    PRIMARY KEY (usuario_id),
    CONSTRAINT uq_usuario_correo UNIQUE (correo),
    CONSTRAINT uq_usuario_publico UNIQUE (identificador_publico),
    CONSTRAINT ck_usuario_id CHECK (usuario_id REGEXP '^US-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'),
    CONSTRAINT ck_usuario_activo CHECK (activo IN (0, 1))
) ENGINE=InnoDB;

CREATE TABLE berrutilocal.rol (
    rol_id VARCHAR(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    nombre VARCHAR(13) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    PRIMARY KEY (rol_id),
    CONSTRAINT uq_rol_nombre UNIQUE (nombre),
    CONSTRAINT ck_rol_id CHECK (rol_id REGEXP '^RO-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'),
    CONSTRAINT ck_rol_nombre CHECK (nombre IN ('Pasajero', 'Cobrador', 'Administrador'))
) ENGINE=InnoDB;

CREATE TABLE berrutilocal.usuario_rol (
    usuario_id VARCHAR(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    rol_id VARCHAR(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    PRIMARY KEY (usuario_id, rol_id),
    CONSTRAINT fk_usuario_rol_usuario FOREIGN KEY (usuario_id)
        REFERENCES berrutilocal.usuario (usuario_id) ON DELETE RESTRICT,
    CONSTRAINT fk_usuario_rol_rol FOREIGN KEY (rol_id)
        REFERENCES berrutilocal.rol (rol_id) ON DELETE RESTRICT
) ENGINE=InnoDB;

CREATE TABLE berrutilocal.sesion (
    huella CHAR(64) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    usuario_id VARCHAR(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    creada_utc DATETIME(6) NOT NULL,
    vence_utc DATETIME(6) NOT NULL,
    revocada_utc DATETIME(6) NULL,
    PRIMARY KEY (huella),
    CONSTRAINT fk_sesion_usuario FOREIGN KEY (usuario_id)
        REFERENCES berrutilocal.usuario (usuario_id) ON DELETE RESTRICT,
    CONSTRAINT ck_sesion_vence CHECK (vence_utc = DATE_ADD(creada_utc, INTERVAL 7 DAY))
) ENGINE=InnoDB;

-- 3. FIN DDL. Sin cuentas ni contadores inicializados. Revisar las cinco
-- definiciones exactas antes del bootstrap separado seed-auth-demo-passenger.sql.
-- Si hubo error/estado parcial, detenerse: no reparar, borrar ni repetir este archivo.
