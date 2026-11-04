-- SOLO berrutilocal recién creada: cinco tablas totalmente vacías, sin historia.
-- Propuesta MANUAL, NO ejecutar archivo completo. UNA sentencia; parar al error.
-- Revisar SHOW CREATE de las cinco tablas contra create-auth-demo.sql primero.
-- API/otros escritores detenidos. Conexión dedicada NUEVA, sin transacción pendiente:
-- START TRANSACTION puede confirmar una transacción anterior; no anidar ni reutilizar.
-- 1. Inicialización segura. Nunca escribir una contraseña literal en este archivo.
SET @contrasena_demo = NULL;
-- PAUSA: elegir solo contraseña FICTICIA, no la de MySQL. Asignar privadamente en
-- Workbench @contrasena_demo ENTRE esta inicialización y START, según la guía.
-- Sin esa asignación (NULL por defecto) todos los INSERT quedan sin filas.

START TRANSACTION;
SET @paso = IF(@contrasena_demo IS NOT NULL
    AND CHAR_LENGTH(@contrasena_demo) BETWEEN 1 AND 255
    AND @contrasena_demo REGEXP '[^[:space:]]'
    AND NOT EXISTS (SELECT 1 FROM berrutilocal.contador_entidad)
    AND NOT EXISTS (SELECT 1 FROM berrutilocal.usuario)
    AND NOT EXISTS (SELECT 1 FROM berrutilocal.rol)
    AND NOT EXISTS (SELECT 1 FROM berrutilocal.usuario_rol)
    AND NOT EXISTS (SELECT 1 FROM berrutilocal.sesion), 0, -1);
-- Identificador público aleatorio de 32 bytes; NO es token Bearer ni contraseña.
SET @publico_demo = TRIM(TRAILING '=' FROM
    REPLACE(REPLACE(TO_BASE64(RANDOM_BYTES(32)), '+', '-'), '/', '_'));

-- 2. Cada INSERT y transición exige el paso/cantidad afectada anterior.
INSERT INTO berrutilocal.contador_entidad (prefijo, ultimo)
SELECT semilla.prefijo, 1
FROM (SELECT 'US-' AS prefijo UNION ALL SELECT 'RO-') AS semilla
LEFT JOIN berrutilocal.contador_entidad AS existente ON 1 = 1
WHERE @paso = 0 AND existente.prefijo IS NULL;
SET @filas_contadores = ROW_COUNT();
SET @paso = IF(@paso = 0 AND @filas_contadores = 2, 1, -1);

INSERT INTO berrutilocal.rol (rol_id, nombre)
SELECT 'RO-1', 'Pasajero' WHERE @paso = 1;
SET @filas_rol = ROW_COUNT();
SET @paso = IF(@paso = 1 AND @filas_rol = 1, 2, -1);

INSERT INTO berrutilocal.usuario
    (usuario_id, identificador_publico, correo, contrasena, activo)
SELECT 'US-1', @publico_demo, 'pasajero.demo@example.test', @contrasena_demo, 1
WHERE @paso = 2 AND CHAR_LENGTH(@publico_demo) = 43
    AND @publico_demo REGEXP '^[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]$';
SET @filas_usuario = ROW_COUNT();
SET @contrasena_demo = NULL;
SET @paso = IF(@paso = 2 AND @filas_usuario = 1, 3, -1);

INSERT INTO berrutilocal.usuario_rol (usuario_id, rol_id)
SELECT 'US-1', 'RO-1' WHERE @paso = 3;
SET @filas_membresia = ROW_COUNT();
SET @paso = IF(@paso = 3 AND @filas_membresia = 1, 4, -1);

-- 3. Postchecks en ESTA conexión/transacción. Nunca mostrar contraseña/huella/token.
-- Esperar paso=4 y cantidades 2,1,1,1; bootstrap_correcto=1 y detalles exactos.
SELECT @paso AS paso_final, @filas_contadores AS filas_contadores,
    @filas_rol AS filas_rol, @filas_usuario AS filas_usuario,
    @filas_membresia AS filas_membresia;
SELECT (@paso = 4
    AND (SELECT COUNT(*) FROM berrutilocal.contador_entidad) = 2
    AND (SELECT COUNT(*) FROM berrutilocal.contador_entidad
         WHERE prefijo IN ('US-', 'RO-') AND ultimo = 1) = 2
    AND (SELECT COUNT(*) FROM berrutilocal.usuario) = 1
    AND (SELECT COUNT(*) FROM berrutilocal.usuario
         WHERE usuario_id = 'US-1' AND activo = 1
           AND correo = 'pasajero.demo@example.test'
           AND CHAR_LENGTH(identificador_publico) = 43
           AND identificador_publico REGEXP '^[A-Za-z0-9_-]{42}[AEIMQUYcgkosw048]$') = 1
    AND (SELECT COUNT(*) FROM berrutilocal.rol) = 1
    AND (SELECT COUNT(*) FROM berrutilocal.rol
         WHERE rol_id = 'RO-1' AND nombre = 'Pasajero') = 1
    AND (SELECT COUNT(*) FROM berrutilocal.usuario_rol) = 1
    AND (SELECT COUNT(*) FROM berrutilocal.usuario_rol
         WHERE usuario_id = 'US-1' AND rol_id = 'RO-1') = 1
    AND NOT EXISTS (SELECT 1 FROM berrutilocal.sesion)) AS bootstrap_correcto;
SELECT prefijo, ultimo FROM berrutilocal.contador_entidad ORDER BY prefijo;
SELECT usuario_id, activo, CHAR_LENGTH(identificador_publico) AS longitud_publico
FROM berrutilocal.usuario;
SELECT rol_id, nombre FROM berrutilocal.rol;
SELECT usuario_id, rol_id FROM berrutilocal.usuario_rol;
SELECT COUNT(*) AS sesiones FROM berrutilocal.sesion;
SET @publico_demo = NULL;
-- FIN: transacción ABIERTA. No hay COMMIT ejecutable. La persona debe decidir
-- COMMIT solo si TODO coincide; en otro caso ROLLBACK y revisión, no reintento.
-- Si se ejecutó todo accidentalmente, NO asumir seguridad: resolver manualmente
-- la transacción, limpiar @contrasena_demo y cerrar conexión antes de continuar.
