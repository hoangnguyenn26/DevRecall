# Database Integrity and Query Performance

DevRecall uses PostgreSQL 17 through EF Core migrations. The API never calls
`EnsureCreated`; migrations run only through the explicit `--migrate` deployment
step or the PostgreSQL integration-test fixture.

## Integrity boundaries

| Area | Database guarantee | Application/domain guarantee |
| --- | --- | --- |
| Identity | Unique normalized email; one learning-preference row per user; bounded preference checks | Email normalization, password policy, ownership |
| Knowledge | User and parent foreign keys; unique normalized tag per user; unique node/tag pair; version concurrency token | Parent ownership, self/descendant cycle prevention, archive transitions |
| Interview | Unique answer version number and one draft per question; ordered follow-up index; immutable practice snapshots; owner-scoped submission ID | Published answers are immutable; question and follow-up ownership |
| DSA | Unique attempt number per problem; immutable practice snapshot; owner-scoped submission ID | Attempt and mastery transitions; problem ownership |
| Review | One active item per user/resource; non-negative schedule checks; owner-scoped submission ID; review-count concurrency token | Rating transition and atomic schedule/history write |
| Study | Unique resources per plan/session; lifecycle checks; version concurrency tokens; unique completion submission ID | Capacity, ordering, lifecycle and evidence ownership |
| Insights | One weak-topic profile per user/resource; one active recommendation per user/resource/type; score/version checks | Signal calculation, recommendation lifecycle and resource availability |

Physical deletion is not part of normal user workflows: resources are archived.
Account deletion cascades user-owned data. Aggregate-owned rows such as plan items,
session items and follow-up attempt snapshots cascade only with their aggregate.
Practice attempts reference source questions/problems with `RESTRICT`, preserving
history when a source is no longer available.

All `DateTimeOffset` properties map to `timestamp with time zone`. API and analytics
ranges use half-open UTC intervals: `fromUtc <= value < toUtc`.

## Canonical analytics sources

- Study minutes and completed sessions: completed `study_sessions`.
- Completed study items: completed `study_session_items`, owner-scoped through the session.
- Review outcomes: immutable `review_histories`, owner-scoped through `review_items`.
- Interview outcomes: immutable `interview_practice_attempts`.
- DSA outcomes: immutable `dsa_attempts`, owner-scoped through `dsa_problems`.
- Weak topics and recommendations are derived projections and are not counted as
  new learning activity.

This prevents a single action from being counted once as source evidence and again
as a derived insight.

## Migration audit

On 10 August 2026, `dotnet ef migrations has-pending-model-changes` reported no
model drift. A clean PostgreSQL database accepted all 30 migrations through the
explicit API `--migrate` path. A populated upgrade rehearsal stopped immediately
before `AddWeakTopicProfileVersion`, inserted an existing profile, then applied the
remaining migrations successfully. The migration now initializes existing profile
versions to `1` before enforcing `version > 0`.

Useful checks:

```powershell
$env:ConnectionStrings__Database = '<temporary PostgreSQL connection string>'
dotnet ef migrations has-pending-model-changes `
  --project src/DevRecall.Infrastructure `
  --startup-project src/DevRecall.Api
dotnet run --project src/DevRecall.Api -- --migrate
```

The PostgreSQL integration fixture creates a clean database, calls `MigrateAsync`,
and verifies that every migration defined by the assembly is applied with none
pending.

## Index and query-plan audit

Indexes are tied to production query shapes, not entity properties in isolation:

- due reviews: `(user_id, status, due_at_utc)`;
- active/history study sessions: `(user_id, status, created_at_utc)` and
  `(user_id, completed_at_utc)`;
- Today priorities: owner/status indexes for plans, recommendations and weak topics;
- global search: GIN expression indexes matching each `to_tsvector('simple', ...)` expression;
- child history: `(review_item_id, reviewed_at_utc)` and
  `(dsa_problem_id, attempted_at_utc)`.

`EXPLAIN (ANALYZE, BUFFERS)` was run after `ANALYZE` against 20,000 Knowledge rows,
50,000 Review rows and 50,000 Study Session rows. Representative warm local results:

| Query | Selected plan | Execution |
| --- | --- | ---: |
| Knowledge full-text search | Bitmap Index Scan on `ix_knowledge_nodes_search` | 5.6 ms |
| First 20 due reviews | Index Scan on `ix_review_items_user_status_due_at` plus incremental sort | 1.7 ms |
| 90-day completed-session aggregate | Bitmap Index Scan on `ix_study_sessions_user_completed_at` | 4.4 ms |
| First 50 in-progress sessions | Backward Index Scan on `ix_study_sessions_user_status_created_at` | 0.5 ms |

These are local development measurements, not service-level objectives. The audit
did not justify another index: each critical predicate used its existing index and
the bounded secondary sorts remained small. Re-run plans with production-like data
before changing an index; extra indexes increase write cost and migration risk.

Read paths project DTOs, use `AsNoTracking`, filter and paginate before
materialization, and avoid parallel operations on the same `DbContext`. Today is a
single HTTP composition endpoint, but its database reads remain sequential because
EF Core contexts do not support concurrent commands.
