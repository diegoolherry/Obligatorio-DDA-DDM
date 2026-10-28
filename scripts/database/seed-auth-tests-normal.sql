-- SOLO berrutiauthtests: revisión/ejecución MANUAL después de crear las tablas.
-- Elegir exactamente UNA fixture inicial: esta NORMAL (0) O boundary (49998).
-- NO ejecutar ambos archivos, no ejecutar en paralelo ni con otros escritores.
-- Ejecutar cada sentencia individualmente, detenerse al primer error.
-- No es reparación ni reinicio: si hay cualquier dato, INSERT no hace nada.
-- Si hubo uso anterior, pedir decisión administrativa; nunca borrar/recrear historia.
-- El único INSERT prepara ambas filas juntas o ninguna, sin UPSERT/REPLACE/UPDATE.
INSERT INTO berrutiauthtests.contador_entidad (prefijo, ultimo)
SELECT semilla.prefijo, 0
FROM (SELECT 'US-' AS prefijo UNION ALL SELECT 'RO-') AS semilla
LEFT JOIN berrutiauthtests.contador_entidad AS existente ON 1 = 1
WHERE existente.prefijo IS NULL
  AND NOT EXISTS (SELECT 1 FROM berrutiauthtests.usuario)
  AND NOT EXISTS (SELECT 1 FROM berrutiauthtests.rol)
  AND NOT EXISTS (SELECT 1 FROM berrutiauthtests.usuario_rol);

-- Postcheck obligatorio: esperar exactamente US-/RO- en 0 y fixture_fresca_correcta=1.
-- Cualquier otro resultado: detenerse. No probar la otra semilla ni modificar datos.
SELECT prefijo, ultimo FROM berrutiauthtests.contador_entidad ORDER BY prefijo;
SELECT (COUNT(*) = 2 AND
        SUM(CASE WHEN prefijo IN ('US-', 'RO-') AND ultimo = 0 THEN 1 ELSE 0 END) = 2 AND
        NOT EXISTS (SELECT 1 FROM berrutiauthtests.usuario) AND
        NOT EXISTS (SELECT 1 FROM berrutiauthtests.rol) AND
        NOT EXISTS (SELECT 1 FROM berrutiauthtests.usuario_rol)) AS fixture_fresca_correcta
FROM berrutiauthtests.contador_entidad;
-- --mysql vuelve a comprobar solo por lectura este estado ANTES de cualquier DML.
