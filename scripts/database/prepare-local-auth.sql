-- Fresh local provisioning only. MySQL 8+. Review the companion guide first.
-- NOT executed as part of preparation. Never run this entire file as a batch.
-- No passwords belong in this file. New accounts remain locked.

-- 1. READ-ONLY PREFLIGHT: select and execute only this section first as admin.
-- Both inventory queries MUST return zero rows. If either fails or finds any
-- target name (including an account at another host), STOP; do not create/reuse.
SELECT SCHEMA_NAME
FROM INFORMATION_SCHEMA.SCHEMATA
WHERE LOWER(SCHEMA_NAME) IN ('berrutilocal', 'berrutiauthtests');

SELECT User, Host, account_locked
FROM mysql.user
WHERE LOWER(User) IN ('berruti_app', 'berruti_test');

-- HUMAN GATE: confirm all target names are absent before selecting section 2.
-- This file does not enforce that gate automatically or prevent concurrent DDL.

-- 2. CREATE: execute ONE statement at a time, stopping at the FIRST error.
-- MySQL DDL/account statements implicitly commit: this is NOT atomic,
-- transactional, rollback-safe or idempotent. Clients may continue after errors.
-- Partial success requires administrator review, never a blind rerun or cleanup.
CREATE DATABASE `berrutilocal`;
CREATE DATABASE `berrutiauthtests`;

CREATE USER 'berruti_app'@'localhost' ACCOUNT LOCK;
CREATE USER 'berruti_test'@'localhost' ACCOUNT LOCK;

-- Database names deliberately contain no GRANT wildcard characters (% or _).
-- Runtime DML only; an administrator must separately review/apply future schema.
GRANT SELECT, INSERT, UPDATE, DELETE ON `berrutilocal`.* TO 'berruti_app'@'localhost';
GRANT SELECT, INSERT, UPDATE, DELETE ON `berrutiauthtests`.* TO 'berruti_test'@'localhost';

-- 3. READ-ONLY POSTCHECKS: only after every creation/grant statement succeeded.
SELECT SCHEMA_NAME
FROM INFORMATION_SCHEMA.SCHEMATA
WHERE LOWER(SCHEMA_NAME) IN ('berrutilocal', 'berrutiauthtests');

SELECT User, Host, account_locked
FROM mysql.user
WHERE LOWER(User) IN ('berruti_app', 'berruti_test');

SHOW GRANTS FOR 'berruti_app'@'localhost';
SHOW GRANTS FOR 'berruti_test'@'localhost';

-- Keep accounts locked until a human locally sets distinct passwords and
-- unlocks them through Workbench account management. Never save secrets here.
