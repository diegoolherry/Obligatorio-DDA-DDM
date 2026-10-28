-- SOLO berrutiauthtests. Propuesta T1: revisión/ejecución MANUAL por administrador.
-- NO ejecutar archivo completo. Ejecutar cada sentencia individualmente y parar al
-- primer error. DDL NO es atómico: commits implícitos; rollback no deshace CREATE.
-- No aplicar en berrutilocal, no repetir bootstrap, no modificar usuarios/permisos.
-- Runtime berruti_test tiene únicamente DML y NO debe ejecutar este archivo.
-- 1. Inventario obligatorio: debe devolver CERO filas y completarse sin errores.
-- Si existe cualquier tabla objetivo, detenerse y pedir revisión administrativa
-- de su definición y contadores históricos. No reutilizar ni reparar a ciegas.
SELECT TABLE_SCHEMA, TABLE_NAME, ENGINE
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'berrutiauthtests'
  AND TABLE_NAME IN ('usuario', 'rol', 'usuario_rol', 'contador_entidad');

-- 2. Solo tras confirmar inventario vacío, sin otro creador concurrente.
-- MySQL 8.0.16+ para CHECK efectivos. Confirmar versión antes de crear.
CREATE TABLE berrutiauthtests.contador_entidad (
    prefijo VARCHAR(3) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    ultimo INT NOT NULL,
    PRIMARY KEY (prefijo),
    CONSTRAINT ck_contador_rango CHECK (ultimo BETWEEN 0 AND 50000),
    CONSTRAINT ck_contador_prefijo CHECK (prefijo IN ('US-', 'RO-'))
) ENGINE=InnoDB;

CREATE TABLE berrutiauthtests.usuario (
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

CREATE TABLE berrutiauthtests.rol (
    rol_id VARCHAR(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    nombre VARCHAR(13) CHARACTER SET utf8mb4 COLLATE utf8mb4_bin NOT NULL,
    PRIMARY KEY (rol_id),
    CONSTRAINT uq_rol_nombre UNIQUE (nombre),
    CONSTRAINT ck_rol_id CHECK (rol_id REGEXP '^RO-([1-9][0-9]{0,3}|[1-4][0-9]{4}|50000)$'),
    CONSTRAINT ck_rol_nombre CHECK (nombre IN ('Pasajero', 'Cobrador', 'Administrador'))
) ENGINE=InnoDB;

CREATE TABLE berrutiauthtests.usuario_rol (
    usuario_id VARCHAR(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    rol_id VARCHAR(8) CHARACTER SET ascii COLLATE ascii_bin NOT NULL,
    PRIMARY KEY (usuario_id, rol_id),
    CONSTRAINT fk_usuario_rol_usuario FOREIGN KEY (usuario_id)
        REFERENCES berrutiauthtests.usuario (usuario_id) ON DELETE RESTRICT,
    CONSTRAINT fk_usuario_rol_rol FOREIGN KEY (rol_id)
        REFERENCES berrutiauthtests.rol (rol_id) ON DELETE RESTRICT
) ENGINE=InnoDB;

-- 3. Aquí termina la creación del esquema, SIN inicializar contadores.
-- Revisar y elegir exactamente UN archivo de fixture inicial antes de asignar IDs:
-- seed-auth-tests-normal.sql   -> US-/RO- en 0 (escenario --mysql).
-- seed-auth-tests-boundary.sql -> US-/RO- en 49998 (escenario --mysql-boundary).
-- Son alternativas sobre tablas totalmente vacías, NO pasos consecutivos.
-- No reiniciar/adelantar contadores históricos ni repetir una fixture consumida.
-- El runner rechaza cualquier estado inicial distinto del escenario elegido.
-- No se cargan cuentas ni contraseñas. El runner crea sus fixtures ficticias.
