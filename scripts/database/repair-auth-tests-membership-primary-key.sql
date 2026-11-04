-- SOLO berrutiauthtests.usuario_rol. T18/S20: revisión MANUAL por administrador
-- en Workbench. Preparación autorizada, NO ejecución por agente/runner.
-- NO ejecutar el archivo completo: seleccionar una sentencia por vez, detenerse
-- al primer error o resultado inesperado. berruti_test tiene solo DML: NO ampliar
-- permisos ni usarlo para ALTER. No cambiar demo, datos, columnas ni otras claves.
-- Puertas HUMANAS, no automatizadas: estas consultas NO condicionan el ALTER.
-- Antes de empezar, excluir otros escritores/administradores hasta los postchecks.
-- Revisar resultados frescos y conservar metadatos pre/post sin datos privados.

-- 1. Deben aparecer exactamente estas cuatro BASE TABLE, todas InnoDB.
SELECT TABLE_SCHEMA, TABLE_NAME, TABLE_TYPE, ENGINE
FROM information_schema.TABLES
WHERE TABLE_SCHEMA = 'berrutiauthtests'
  AND TABLE_NAME IN ('contador_entidad', 'rol', 'usuario', 'usuario_rol')
ORDER BY TABLE_NAME;

-- Exactamente usuario_id y rol_id, ambas IS_NULLABLE = NO.
SELECT TABLE_SCHEMA, TABLE_NAME, COLUMN_NAME, IS_NULLABLE
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = 'berrutiauthtests' AND TABLE_NAME = 'usuario_rol'
ORDER BY ORDINAL_POSITION;

-- Estado reparable: UNA fila, usuario_id en posición 1.
-- Si YA hay dos filas usuario_id/1 y rol_id/2: DETENERSE, no reaplicar.
-- Cualquier otro estado: DETENERSE y consultar al administrador.
SELECT CONSTRAINT_NAME, COLUMN_NAME, ORDINAL_POSITION
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'berrutiauthtests' AND TABLE_NAME = 'usuario_rol'
  AND CONSTRAINT_NAME = 'PRIMARY'
ORDER BY ORDINAL_POSITION;

-- Las CUATRO cantidades deben ser 0. No consultar filas de cuentas.
-- Con datos, DETENERSE: este archivo no autoriza borrarlos ni repararlos.
SELECT 'contador_entidad' AS tabla, COUNT(*) AS cantidad FROM berrutiauthtests.contador_entidad
UNION ALL SELECT 'rol', COUNT(*) FROM berrutiauthtests.rol
UNION ALL SELECT 'usuario', COUNT(*) FROM berrutiauthtests.usuario
UNION ALL SELECT 'usuario_rol', COUNT(*) FROM berrutiauthtests.usuario_rol;

-- Exactamente dos FK de una columna, ordinal 1:
-- fk_usuario_rol_usuario: usuario_id -> berrutiauthtests.usuario.usuario_id
-- fk_usuario_rol_rol: rol_id -> berrutiauthtests.rol.rol_id
-- Ambas DELETE_RULE = RESTRICT, UPDATE_RULE = NO ACTION, MATCH_OPTION = NONE.
-- Conservar esta salida para cotejarla exactamente después del ALTER.
SELECT k.CONSTRAINT_NAME, k.COLUMN_NAME, k.ORDINAL_POSITION,
       k.REFERENCED_TABLE_SCHEMA, k.REFERENCED_TABLE_NAME, k.REFERENCED_COLUMN_NAME,
       r.DELETE_RULE, r.UPDATE_RULE, r.MATCH_OPTION
FROM information_schema.KEY_COLUMN_USAGE AS k
JOIN information_schema.REFERENTIAL_CONSTRAINTS AS r
  ON r.CONSTRAINT_SCHEMA = k.CONSTRAINT_SCHEMA
 AND r.TABLE_NAME = k.TABLE_NAME AND r.CONSTRAINT_NAME = k.CONSTRAINT_NAME
WHERE k.TABLE_SCHEMA = 'berrutiauthtests' AND k.TABLE_NAME = 'usuario_rol'
  AND k.REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY k.CONSTRAINT_NAME, k.ORDINAL_POSITION;

-- Referencias ENTRANTES: CERO filas. Buscar desde cualquier esquema, no solo test.
-- El administrador debe tener visibilidad suficiente de metadatos de todos los
-- esquemas: una salida vacía con visibilidad limitada NO prueba ausencia.
-- Si aparece cualquier referencia, DETENERSE para revisión, no eliminar FK.
SELECT CONSTRAINT_SCHEMA, TABLE_SCHEMA, TABLE_NAME, CONSTRAINT_NAME, COLUMN_NAME,
       REFERENCED_TABLE_SCHEMA, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME
FROM information_schema.KEY_COLUMN_USAGE
WHERE REFERENCED_TABLE_SCHEMA = 'berrutiauthtests'
  AND REFERENCED_TABLE_NAME = 'usuario_rol'
ORDER BY TABLE_SCHEMA, TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION;

-- 2. Solo con TODAS las puertas anteriores aprobadas por el humano, ejecutar
-- únicamente esta sentencia. DDL tiene commits implícitos, NO garantía de rollback
-- ni seguridad de lote/idempotencia. No envolver en START TRANSACTION.
-- Si falla, PARAR: no reintentar automáticamente, no soltar FK ni añadir índices
-- como alternativa. Requiere nueva revisión administrativa, sin cambios extra.
ALTER TABLE berrutiauthtests.usuario_rol
    DROP PRIMARY KEY,
    ADD PRIMARY KEY (usuario_id, rol_id);

-- 3. Postchecks de solo lectura. Exigir exactamente usuario_id/1 y rol_id/2.
SELECT CONSTRAINT_NAME, COLUMN_NAME, ORDINAL_POSITION
FROM information_schema.KEY_COLUMN_USAGE
WHERE TABLE_SCHEMA = 'berrutiauthtests' AND TABLE_NAME = 'usuario_rol'
  AND CONSTRAINT_NAME = 'PRIMARY'
ORDER BY ORDINAL_POSITION;

-- Misma definición de las dos FK que antes, sin cambios.
SELECT k.CONSTRAINT_NAME, k.COLUMN_NAME, k.ORDINAL_POSITION,
       k.REFERENCED_TABLE_SCHEMA, k.REFERENCED_TABLE_NAME, k.REFERENCED_COLUMN_NAME,
       r.DELETE_RULE, r.UPDATE_RULE, r.MATCH_OPTION
FROM information_schema.KEY_COLUMN_USAGE AS k
JOIN information_schema.REFERENTIAL_CONSTRAINTS AS r
  ON r.CONSTRAINT_SCHEMA = k.CONSTRAINT_SCHEMA
 AND r.TABLE_NAME = k.TABLE_NAME AND r.CONSTRAINT_NAME = k.CONSTRAINT_NAME
WHERE k.TABLE_SCHEMA = 'berrutiauthtests' AND k.TABLE_NAME = 'usuario_rol'
  AND k.REFERENCED_TABLE_NAME IS NOT NULL
ORDER BY k.CONSTRAINT_NAME, k.ORDINAL_POSITION;

-- Las cuatro cantidades siguen en 0. Sin seed ni escenario normal/frontera aún.
SELECT 'contador_entidad' AS tabla, COUNT(*) AS cantidad FROM berrutiauthtests.contador_entidad
UNION ALL SELECT 'rol', COUNT(*) FROM berrutiauthtests.rol
UNION ALL SELECT 'usuario', COUNT(*) FROM berrutiauthtests.usuario
UNION ALL SELECT 'usuario_rol', COUNT(*) FROM berrutiauthtests.usuario_rol;

-- Si algo no coincide, detenerse y conservar evidencia: no reparar/revertir a ciegas.
-- Solo tras confirmación humana de ejecución real y postchecks, el coordinador
-- reutilizará --inspect una vez para comprobar la pareja. No es PASS de T1/MVP.
