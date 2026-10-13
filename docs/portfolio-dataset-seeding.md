# Portfolio dataset seeding

## Purpose and scope

P1 makes the repository the reviewable source of truth for DevRecall's global
Learn/Discover catalog. Codex may author files, but a human reviews and promotes
them before an explicit tool materializes them in PostgreSQL.

```text
seed/drafts
    -> validate
    -> human review
    -> seed/learning-content
    -> preview / import --dry-run
    -> explicit import
    -> PostgreSQL
```

This document records the file contract and the explicit importer now used for
the portfolio catalog. Approved files are the sole runtime catalog authority.

## Current architecture

The solution is `DevRecall.slnx`. Relevant projects and files are:

- API/composition root: `src/DevRecall.Api/DevRecall.Api.csproj` and
  `src/DevRecall.Api/Program.cs`.
- Use cases: `src/DevRecall.Application/DevRecall.Application.csproj`.
- Aggregate and canonical enums: `src/DevRecall.Domain/DevRecall.Domain.csproj`,
  especially `src/DevRecall.Domain/LearningContent/` and
  `src/DevRecall.Domain/LearningProfiles/`.
- EF Core and development seeders:
  `src/DevRecall.Infrastructure/DevRecall.Infrastructure.csproj`,
  `src/DevRecall.Infrastructure/Persistence/Configurations/`, and
  `src/DevRecall.Infrastructure/Development/`.
- Migrations: `src/DevRecall.Infrastructure/Persistence/Migrations/`.
- Legacy catalog test fixture:
  `LearningContentSeeder.cs`, `LearningContentSeeder.Curriculum.cs`, and
  `LearningContentSeeder.Resources.cs` under
  `src/DevRecall.Infrastructure/Development/`.
- Other local/demo data: `DemoDataSeeder.cs` and the local dogfood seeder under
  the same Development folder. These create user-owned demonstration data and
  must remain separate from the global portfolio catalog.
- Relevant documentation: `docs/learning-content.md`,
  `docs/external-resources.md`, `docs/content-curriculum.md`, and
  `docs/content-authoring.md`.

`tools/DevRecall.SeedTool` is the dedicated deterministic console importer.

## Actual LearningContent model

`LearningContent` is the aggregate root for both `Lesson` and
`ExternalResource`. The root has a caller-supplied UUID, globally unique slug,
title, summary, content type, optional resource kind, difficulty, estimated
minutes, status, source metadata, optimistic-concurrency version, and UTC
created/updated/published timestamps.

The root owns:

- technologies (`Technology` enum values),
- goals (`LearningProfileGoal` enum values),
- topic links to `ContentTopic` rows,
- ordered objectives,
- ordered Markdown sections with `Explanation`, `CodeExample`, or
  `KeyTakeaway` type,
- ordered review candidates with a normalized, unique key.

Domain/database limits include 20 technologies, 20 topics, 10 objectives, 50
sections, 5 review candidates, 50,000 characters per section, and 1-480
estimated minutes. Published content requires at least one topic. A published
Lesson also requires objectives and sections.

An ExternalResource uses the same root, but the domain requires external
provenance, one of `Documentation`, `Guide`, `Tutorial`, or `Reference`, and an
absolute HTTP/HTTPS URL without user information. It cannot have lesson
objectives, sections, or review candidates. This is curated metadata, not a
Lesson with an empty body.

### Identity, references, and deletes

- `learning_contents.id` is never database-generated. `slug` has a unique
  constraint and is the stable public identity.
- Aggregate children cascade when the root is deleted. A topic link uses a
  restricted FK to its canonical `content_topics` row.
- Progress and completion evidence reference the content root with restricted
  deletion. Each is unique per user/content; completion evidence stores a title
  snapshot.
- Knowledge provenance references the root with restricted deletion and stores
  a source-title snapshot.
- Review provenance stores candidate key, title, prompt, and answer snapshots,
  but it also has restricted FKs to both the content root and the exact
  `LearningReviewCandidate` row. Candidate rows therefore cannot be replaced
  after they have been materialized into Review items.
- Review submission records also retain restricted root/Review item references.
- Study Plan and Study Session items store a polymorphic `ResourceType` and
  `ResourceId` rather than an EF FK. Session items additionally store a title
  snapshot. The absence of an FK does not make root identity disposable: these
  historical references still depend on a stable content ID.
- Objective and section IDs have no references outside the aggregate. Their
  current editorial update method nevertheless preserves count, order, and IDs.
  Candidate identity is materially stronger because of the direct Review FK.

Consequences for importing:

1. Never delete/recreate a LearningContent root to apply editorial changes.
2. Match existing roots by slug and verify the file UUID matches the stored UUID.
3. Match review candidates by stable `CandidateKey`; never use positional keys
   such as `q1`, and never replace referenced candidate rows.
4. Absence from the approved files is not a delete instruction. Archiving must
   be explicit.
5. Preserve all user-owned progress, evidence, Knowledge, Review, Plan, and
   Session records.

## Legacy seed mechanism

`LearningContentSeeder` was the hardcoded C# authority for nine canonical
topics, 11 lessons, and five external resources. Its API command and production
dependency registration have been removed. The class remains only as a bounded
fixture used by existing integration tests; it is not reachable from normal or
Development application composition. Demo and dogfood seeders remain separate.

The approved files under `seed/learning-content` and the explicit
`DevRecall.SeedTool` command are now the only runtime catalog authority. Nothing
imports during API startup.

## Current taxonomy sources

- Technologies are canonical `Technology` enum values in
  `src/DevRecall.Domain/LearningProfiles/Technology.cs` (17 current values).
- Goals are canonical `LearningProfileGoal` enum values in
  `src/DevRecall.Domain/LearningProfiles/LearningProfileGoal.cs` (6 values).
- Content type, status, difficulty, source type, resource kind, and section type
  are Domain enums under `src/DevRecall.Domain/LearningContent/`.
- Topics are database entities, not enums. The current catalog seeder owns nine
  stable topic UUID/slug/name definitions.

The file validator must read enum names from the Domain assembly and reject
numeric JSON values. Seed files must use exact canonical string names. It must
not introduce a second technology or goal list. Topic files are necessary
because topic rows are data; they will become the single repository authority
only when the new importer replaces the hardcoded topic definitions.

The existing public Learning Content and Learning Profile handlers already parse
case-sensitive enum names with `Enum.TryParse`, require `Enum.IsDefined`, and
explicitly reject values that also parse as integers. The seed parser should
reuse that semantic rule rather than relying on permissive default JSON enum
deserialization.

Existing canonical topics and IDs:

| Slug | Name | Stable ID |
|---|---|---|
| `dependency-injection` | Dependency Injection | `10000000-0000-0000-0000-000000000001` |
| `change-tracking` | Change Tracking | `10000000-0000-0000-0000-000000000002` |
| `request-pipeline` | Request Pipeline | `10000000-0000-0000-0000-000000000003` |
| `asynchronous-programming` | Asynchronous Programming | `10000000-0000-0000-0000-000000000004` |
| `transactions` | Transactions | `10000000-0000-0000-0000-000000000005` |
| `concurrency` | Concurrency | `10000000-0000-0000-0000-000000000006` |
| `configuration` | Configuration | `10000000-0000-0000-0000-000000000007` |
| `access-control` | Access Control | `10000000-0000-0000-0000-000000000008` |
| `api-error-contracts` | API Error Contracts | `10000000-0000-0000-0000-000000000009` |

## Current catalog inventory

The inventory below was projected from the running local PostgreSQL catalog on
Day 1; bodies were intentionally omitted.

| Slug | Title | Type | Status | Difficulty | Min | Technologies | Topics | Goals | Candidates |
|---|---|---|---|---|---:|---|---|---|---:|
| `api-error-handling` | API Error Handling | Lesson | Published | Intermediate | 15 | DotNet, AspNetCore | api-error-contracts | ImproveBackendFundamentals, BuildProjects | 3 |
| `aspnet-core-cancellation-tokens` | CancellationToken Fundamentals | Lesson | Published | Intermediate | 15 | CSharp, DotNet | asynchronous-programming | ImproveBackendFundamentals | 3 |
| `aspnet-core-configuration-options` | Configuration and Options in ASP.NET Core | Lesson | Published | Intermediate | 20 | DotNet, AspNetCore | configuration | ImproveBackendFundamentals, BuildProjects | 3 |
| `aspnet-core-middleware-pipeline` | ASP.NET Core Middleware Pipeline | Lesson | Published | Intermediate | 15 | DotNet, AspNetCore | request-pipeline | ImproveBackendFundamentals, BuildProjects | 3 |
| `aspnet-core-service-lifetimes` | ASP.NET Core Service Lifetimes | Lesson | Published | Intermediate | 15 | DotNet, AspNetCore | dependency-injection | PrepareForInterviews, ImproveBackendFundamentals | 4 |
| `async-await-fundamentals` | async/await Fundamentals | Lesson | Published | Beginner | 15 | CSharp, DotNet | asynchronous-programming | PrepareForInterviews, ImproveBackendFundamentals | 3 |
| `authentication-vs-authorization` | Authentication vs Authorization | Lesson | Published | Beginner | 15 | DotNet, AspNetCore | access-control | PrepareForInterviews, ImproveBackendFundamentals | 3 |
| `dependency-injection-fundamentals` | Dependency Injection Fundamentals | Lesson | Published | Beginner | 10 | DotNet | dependency-injection | PrepareForInterviews, ImproveBackendFundamentals | 2 |
| `ef-core-optimistic-concurrency` | Optimistic Concurrency Fundamentals | Lesson | Published | Intermediate | 20 | DotNet, EfCore | concurrency | PrepareForInterviews, ImproveBackendFundamentals | 3 |
| `ef-core-tracking-vs-no-tracking` | EF Core Tracking vs No Tracking | Lesson | Published | Intermediate | 10 | DotNet, EfCore | change-tracking | PrepareForInterviews, ImproveBackendFundamentals | 2 |
| `ef-core-transactions` | EF Core Transactions | Lesson | Published | Intermediate | 20 | DotNet, EfCore | transactions | ImproveBackendFundamentals | 3 |
| `aspnet-core-dependency-injection-documentation` | Dependency injection in ASP.NET Core | ExternalResource | Published | Intermediate | 25 | DotNet, AspNetCore | dependency-injection | ImproveBackendFundamentals, BuildProjects | 0 |
| `aspnet-core-middleware-documentation` | ASP.NET Core middleware | ExternalResource | Published | Intermediate | 20 | DotNet, AspNetCore | request-pipeline | ImproveBackendFundamentals, BuildProjects | 0 |
| `dotnet-cancellation-in-managed-threads` | Cancellation in Managed Threads | ExternalResource | Published | Intermediate | 20 | CSharp, DotNet | asynchronous-programming | ImproveBackendFundamentals | 0 |
| `ef-core-handling-concurrency-conflicts` | Handling Concurrency Conflicts | ExternalResource | Published | Intermediate | 20 | DotNet, EfCore | concurrency | PrepareForInterviews, ImproveBackendFundamentals | 0 |
| `ef-core-tracking-documentation` | Tracking vs. No-Tracking Queries | ExternalResource | Published | Intermediate | 15 | DotNet, EfCore | change-tracking | ImproveBackendFundamentals | 0 |

Totals: **11 Lessons**, **5 ExternalResources**, all Published.

Coverage is strongest in DI/lifetimes, asynchronous execution/cancellation,
ASP.NET Core middleware/configuration, EF Core tracking/transactions/concurrency,
authorization, and API error contracts. Topic overlap is intentional where an
internal lesson teaches a concept and an external resource supplies authoritative
depth. There are no duplicate slugs or same-type duplicate lessons. The largest
portfolio gaps are HTTP semantics/API design, validation, relational/query
performance, testing, observability, resilience, caching, and background work.
These are candidate areas, not a quota or authorization to mass-create content.

## Final repository layout

The file pipeline uses only folders with a concrete consumer:

```text
seed/
  README.md
  taxonomy/
    topics.json
  instructions/
    lesson-authoring.md
    resource-curation.md
  drafts/
    lessons/<slug>/content.json + body.md
    resources/<slug>.json
  learning-content/
    lessons/<slug>/content.json + body.md
    resources/<slug>.json
  demo/                         # reserved for user-owned demo data, not catalog import
tools/
  DevRecall.SeedTool/
```

`drafts` contains unapproved work and is validated but never imported.
`learning-content` contains approved catalog source. Promotion is a reviewed Git
change that moves a valid item and, normally, changes its status to Published.
The tool must never infer approval from file contents in `drafts`.

## Lesson contract (schema version 1)

Each Lesson has `content.json` plus one `body.md`:

```json
{
  "schemaVersion": 1,
  "id": "10000000-0000-0000-0000-000000000106",
  "slug": "aspnet-core-cancellation-tokens",
  "title": "CancellationToken Fundamentals",
  "summary": "Learn why cancellation is cooperative and how to propagate it.",
  "contentType": "Lesson",
  "status": "Published",
  "difficulty": "Intermediate",
  "estimatedMinutes": 15,
  "source": { "type": "Internal", "name": "DevRecall" },
  "technologies": ["CSharp", "DotNet"],
  "topics": ["asynchronous-programming"],
  "goals": ["ImproveBackendFundamentals"],
  "objectives": [
    "Explain why cancellation in .NET is cooperative."
  ],
  "sections": [
    { "bodyKey": "cooperative-model", "type": "Explanation", "heading": "Cooperative cancellation" },
    { "bodyKey": "propagation-example", "type": "CodeExample", "heading": "Propagate the token" },
    { "bodyKey": "takeaway", "type": "KeyTakeaway", "heading": null }
  ],
  "reviewCandidates": [
    {
      "key": "cancellation-is-cooperative",
      "prompt": "Why is .NET cancellation described as cooperative?",
      "answer": "The caller signals cancellation, and each operation must observe that signal and stop safely."
    }
  ]
}
```

The UUID is source-controlled mechanical identity. Slug is immutable public
identity and folder name. On an existing row, both must match; neither is an
editable field. Enum values are exact Domain enum names. Topic values are
canonical topic slugs. Array order supplies objective, section, and candidate
position. Candidate `key` is semantic identity and must remain stable while its
prompt/answer may be edited.

### Markdown section mapping

`body.md` uses one explicit, prose-independent marker per declared `bodyKey`:

````markdown
<!-- devrecall-section: cooperative-model -->
Cancellation is a signal, not a force-kill operation.

<!-- devrecall-section: propagation-example -->
```csharp
await repository.LoadAsync(id, cancellationToken);
```

<!-- devrecall-section: takeaway -->
Accept the caller's token and pass it to cancellable I/O.
````

Markers must be unique and appear in the same order as `sections`. Text from a
marker up to the next marker becomes `BodyMarkdown`; type and optional heading
come from `content.json`. This keeps one readable Markdown body while avoiding
rules based on exact headings or prose. The `bodyKey` is a file parsing key, not
a new product/domain entity. Objective/section positions remain the current
database mapping.

For the first importer, changing objective/section counts or the set of candidate
keys on an existing Lesson is reported as an unsupported structural change.
Candidate reordering is an Update keyed by CandidateKey, never new identity.
Text and metadata changes remain safe. Structural synchronization can be added
later only with explicit preservation rules; it must not be hidden in a
delete/recreate operation.

## ExternalResource contract (schema version 1)

Each resource is one JSON file:

```json
{
  "schemaVersion": 1,
  "id": "10000000-0000-0000-0000-000000000205",
  "slug": "ef-core-handling-concurrency-conflicts",
  "title": "Handling Concurrency Conflicts",
  "summary": "Official EF Core guidance for concurrency tokens and conflict handling.",
  "contentType": "ExternalResource",
  "status": "Published",
  "difficulty": "Intermediate",
  "estimatedMinutes": 20,
  "resourceKind": "Documentation",
  "source": {
    "type": "External",
    "name": "Microsoft Learn",
    "url": "https://learn.microsoft.com/ef/core/saving/concurrency"
  },
  "technologies": ["DotNet", "EfCore"],
  "topics": ["concurrency"],
  "goals": ["PrepareForInterviews", "ImproveBackendFundamentals"]
}
```

Lesson-only fields and `body.md` are forbidden. Importing or opening a resource
must not create LearningContent progress/evidence. Its URL must be an absolute
HTTP/HTTPS URL with a host and no user information. Opening and study-task
completion retain their existing orchestration-only semantics.

## Topic strategy

The nine current topics and UUIDs are copied unchanged into
`seed/taxonomy/topics.json`. Add a topic only when at least one reviewed item
needs a distinct concept. Do not create a synonym for an existing topic merely
to match wording.

Likely portfolio additions, to approve incrementally with content, are:

- `http-semantics`
- `input-validation`
- `relational-modeling`
- `query-performance`
- `integration-testing`
- `structured-logging`
- `fault-tolerance`
- `caching`
- `background-processing`

These names describe concepts that can support Discover and honest weak-topic
attribution. Vague SEO categories such as `best-practices`,
`advanced-programming`, and `backend-development` are not accepted.

## Validation specification

### Errors (block preview/import)

- duplicate UUID or slug across drafts and approved sources;
- folder/file slug mismatch, invalid normalized slug, empty UUID, or an existing
  slug whose stored root UUID differs;
- unsupported `schemaVersion`;
- missing/unknown fields, invalid enum string, numeric enum value, unknown
  technology/goal/topic, or duplicate list values;
- violation of actual Domain lengths/counts or estimated minutes outside 1-480;
- invalid status transition or content/source/resource-kind combination;
- Lesson without body, objective, section, or topic when Published;
- missing/duplicate/undeclared Markdown markers or empty parsed section body;
- more than 5 candidates, duplicate/invalid CandidateKey, positional keys such
  as `q1`/`q2`, or a candidate structural change unsupported by the first
  importer;
- ExternalResource with body/objectives/sections/candidates, missing source
  metadata/resource kind, or an unsafe/non-HTTP(S) URL;
- approved and draft items sharing an identity with different content;
- destructive replacement, implicit delete, or a mutation that would break a
  restricted provenance/reference FK.

Unknown JSON properties should be errors so typos do not silently disappear.
All errors include file path, field/line when available, stable validation code,
and a human-readable message.

### Warnings (do not block)

- estimated minutes below 5 or above 90 while still inside the Domain range;
- more than 5 objectives, more than 4 topics, or unusually broad technology/
  goal metadata;
- fewer than 2 or more than 4 review candidates (the hard maximum remains 5);
- a Published Lesson lacking a KeyTakeaway section;
- a possible same-type duplicate concept based on normalized title and topic
  overlap;
- catalog imbalance by technology, topic, difficulty, content type, or source;
- a very broad external destination or curator summary that does not explain
  why the resource is useful.

Editorial style remains guidance unless a rule protects a domain invariant or
stable identity.

## Implemented tooling

The explicit console project is `tools/DevRecall.SeedTool`. It references Domain
for canonical enums and Infrastructure for read-only EF comparison. It is not
registered in the API and never runs during API startup.

```powershell
dotnet run --project tools/DevRecall.SeedTool -- validate
dotnet run --project tools/DevRecall.SeedTool -- validate --include-drafts
dotnet run --project tools/DevRecall.SeedTool -- preview
dotnet run --project tools/DevRecall.SeedTool -- import --dry-run
dotnet run --project tools/DevRecall.SeedTool -- import --dry-run --slug <slug>
```

- `validate` reads taxonomy and approved files without a database. Drafts are
  deliberately ignored unless `--include-drafts` is supplied.
- `preview` summarizes catalog counts, coverage, and warnings without a DB.
- `import --dry-run` connects to PostgreSQL, produces a deterministic create /
  update / no-op plan using only no-tracking queries and performs no writes.
- `--slug` narrows the printed DB plan after full-catalog validation and
  comparison.
- `import --dry-run [--slug <slug>]` prints the shared semantic plan and writes
  nothing.
- `import [--slug <slug>]` applies Create and Update entries from that same plan;
  Unchanged entries are not tracked or saved.

Set the standard connection string environment variable before dry-run or import:

```powershell
$env:ConnectionStrings__Database = "Host=localhost;Port=5432;Database=devrecall;Username=devrecall;Password=<local-password>"
```

Validation returns structured severity, stable code, path, optional slug, and
message. Errors block preview/planning; warnings remain visible. Preview reports
catalog totals, difficulty, technology/topic/goal coverage, candidate counts,
low-coverage topics, and warning totals.

Planning resolves roots by slug, verifies the source UUID, compares
technology/topic/goal values as sets, and compares objectives/sections/candidate
positions in order. Candidates are matched by CandidateKey, so wording or
position changes are Updates. Missing approved files never generate Delete or
Archive actions; unmatched DB rows are warnings only.

Before any database write, the entire approved catalog is parsed and validated
and the complete database plan is built. Canonical topics are synchronized by
stable slug/UUID. Each changed LearningContent is then committed in its own
transaction, so a failed item rolls back cleanly while previously committed
items remain safe to replay. Known unique-constraint failures are reported by
constraint name rather than by parsing PostgreSQL messages.

Root rows are resolved by slug. A matching source UUID is mandatory, and an
existing root is always updated in place. Technologies, topics, and goals are
set-like. Objectives and sections are synchronized by ordered position;
unchanged rows are retained and safe obsolete editorial rows may be removed.
Review candidates are synchronized by stable CandidateKey, preserving existing
candidate IDs through wording and position changes. Removing a candidate that
has Review provenance is rejected during planning; an unreferenced obsolete
candidate may be removed. Historical snapshots are never rewritten.

An identical replay produces only Unchanged entries and does not increment root
Version, change UpdatedAt, or rewrite children. Import never creates Progress,
completion evidence, Knowledge, Review, Study Plan, or Study Session state.

No command generates content, calls an AI provider, repairs data on GET, or
imports drafts.

## Migration from the hardcoded catalog

1. The 9 topic definitions, 11 Lessons, and 5 resources have been converted to
   version-1 files using the existing root UUIDs, slugs, candidate keys, text,
   metadata, and ordering.
2. The files validate and preview as a 16-item approved catalog.
3. `import --dry-run` against the existing seeded database returns 16 Unchanged,
   with no root/candidate replacement and zero database writes.
4. A clean migrated PostgreSQL rehearsal created 16 contents (11 lessons and
   five resources), nine topics, and 32 candidates; its second import was 16
   Unchanged.
5. An existing lesson update preserved its root/candidate IDs, two Progress
   rows, two completion evidence rows, two Knowledge sources, four Review
   sources, and its Study Session reference. Historical Review snapshots kept
   their original wording.
6. The API `--seed-learning-content` command and production DI registration were
   removed. The hardcoded class remains test-only while approved files and the
   explicit tool are the sole runtime authority.
7. Never interpret a removed file as deletion. Archive through an approved file
   status before any future source cleanup.

Safe editorial structural synchronization is now part of the aggregate. Root
identity remains stable, and referenced candidate removal is blocked.

## Explicit non-goals

- AI API/SDK/runtime generation or provider abstractions;
- CMS/admin authoring UI;
- crawler, scraper, feed/provider ingestion, or link-health jobs;
- generic ETL/workflow framework;
- Course, Module, or LearningPath domain entities;
- runtime startup seeding, GET repair, implicit deletion, or database reset;
- resource completion/progress semantics;
- mass authoring during Day 1.

## Safe authoring and import workflow

1. Create or revise content under `seed/drafts`.
2. Run `validate --include-drafts`.
3. Have a human review the content and metadata.
4. Promote the approved item into `seed/learning-content`.
5. Run `preview`.
6. Run `import --dry-run`, optionally with `--slug <slug>`.
7. Run `import`, using the same optional slug for a focused batch.
8. Verify the item through Learn/Discover and rerun import to confirm Unchanged.
