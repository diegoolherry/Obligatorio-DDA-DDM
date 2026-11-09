# Local database via EF Core migrations
Replace manual Workbench SQL with EF Core migrations, a generated SQL script and a one-command reset + demo seed, to unblock MVP-001-03 and MVP-001-04.
Branch: `feat/local-db-migrations` (from `main`). Rules: AGENTS.md "Base de datos local" (branch `docs/lean-process`).

## Specs

- S1 — "Migraciones en EF Core, vimos los comandos, pero tmb queremos los scripts de sql para tenerlos en el repo y enzo lo pueda ver"
- S2 — Local DB is disposable: "Sí, descartable" (drop + migraciones + seed con un comando, sin gates manuales en Workbench).
- S3 — Migration account: "Darle DDL a berruti_app y berruti_test".
- S4 — The migrated schema must keep the approved constraints of `scripts/database/create-auth-demo.sql` (CHECKs, collations, `CHAR(64)`).
- S5 — Seed fictitious demo accounts for passenger, collector and admin without versioning passwords (AGENTS.md: "no versiones contraseñas").
- S6 — "si tenes alguna duda o algun espacio vacio sobre que hacer consulta no inventes".

## Tasks

- T1 (S1, S4) — EF tooling, model constraints, initial migration, generated `scripts/database/migrations.sql`. Inline. Done: 2120513.
- T2 (S2, S3, S5) — `--reset-local` command (drop tables, migrate, seed), `scripts/reset-local-db.ps1`, one-time `scripts/database/grant-local-ddl.sql`. Inline. Done: 2120513 (build OK; sin MySQL, el reset sale con exit 2 por falta de contraseña).
- T3 (S1-S6) — Short how-to guide; human runs GRANT + reset; real login check for MVP-001-03/04. Commit: pending.

## Log

- L1 — User: "podemos terminar el MVP esta semana? es ambisiosa pero, el tema uqe se interrumpe mucho en cada implementacion estoy ,ucho en el loop. Podriamos cambiar un poco"; first step chosen: "Cerrar 001-03 y 001-04".
- L2 — Decision: one shared fictitious demo password read from private `.env` key `DemoSeed__Contrasena`; emails `pasajero.demo@`, `cobrador.demo@`, `admin.demo@example.test` (first and last already used in docs).
- L3 — Correction of L2 (agent implementation decision, not a user decision): the demo password is NOT read from `.env`; `scripts/reset-local-db.ps1` asks for it with `Read-Host -AsSecureString` because the `.env` parser of `start-api-local.ps1` accepts a single key. Independent verify `mw9pl6ld-2-xpb0`: PASS S1-S5 and risk items 1, 2, 5; real MySQL reset and login pending (T3).
