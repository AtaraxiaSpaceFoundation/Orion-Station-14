<!-- SPDX-License-Identifier: LicenseRef-OpenSpace-AgentPrompts-Restricted -->

---
name: ss14-databases-migrations
description: Work with SS14 server database models, EF Core DbContexts, SQLite/Postgres migrations, persistence services, admin/player data storage, and schema compatibility.
---

# SS14 Databases And Migrations

Use this skill when a change touches persistent data or database schema.

Database changes are compatibility work. Keep both SQLite and Postgres in sync, avoid runtime-only schema assumptions, and make migrations reviewable.

## Workflow

1. Identify the persistence boundary.
- `Content.Server.Database` owns database models, contexts, design-time factories, and migrations.
- Server systems may call persistence services, but gameplay components should not become database transport objects.
- Shared code should not depend on database projects.

2. Update model and schema together.
- Add or change EF model fields deliberately.
- Generate or update migrations for both providers when the schema changes.
- Keep migration names descriptive and review the generated SQL/model snapshot diff.

3. Preserve data compatibility.
- Consider existing rows, nullable fields, defaults, indexes, and backfills.
- Avoid destructive schema changes unless the task explicitly accepts data loss.
- Keep unique constraints and indexes aligned with query behavior.

4. Keep persistence off hot gameplay paths.
- Do not add blocking database calls to frequent gameplay events or `Update()`.
- Batch, cache, or move work to existing async/service patterns when available.

5. Validate both providers when practical.
- Build database and server projects.
- Run targeted tests or migration commands if the task changes schema.
- State clearly when a migration was not exercised locally.

## Reference Map

- `../ss14-client-server-shared/SKILL.md`
- `../ss14-standard-optimizations/SKILL.md`
- `../ss14-tests-authoring/SKILL.md`
- `../ss14-upstream-maintenance/SKILL.md`

## Good File Anchors

- `Content.Server.Database/**`
- `Content.Shared.Database/**`
- `Content.Server/**/Database/**`
- `Content.Server/**/Persistence/**`

## Common Pitfalls

- Updating only SQLite or only Postgres migrations.
- Using database entities as gameplay state.
- Adding synchronous database work to gameplay hot paths.
- Forgetting indexes or uniqueness rules for new lookup patterns.
