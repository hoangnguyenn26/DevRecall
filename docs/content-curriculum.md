# .NET Backend Foundations — curriculum audit

## Current authored catalog

The catalog now has **11 Published internal lessons**. Three lessons were authored to close real gaps, and seven existing lessons were polished; Service Lifetimes remains unchanged. No Course, prerequisite ordering, new UI or ranking weights were introduced. REST API Fundamentals is intentionally still deferred, not silently counted as existing content.

| Cluster | Lessons | Current editorial result |
| --- | --- | --- |
| Application Foundations | DI Fundamentals; Service Lifetimes; Middleware Pipeline; Configuration and Options | DI before/after example; nested middleware trace, terminal behavior and corrected routing statement; new typed configuration/validation and options-lifetime lesson |
| Async / Reliability | async/await Fundamentals; CancellationToken Fundamentals | Task completion and synchronous-await nuance; cooperative cancellation, propagation and cancellation versus failure |
| Data Access | EF Core Tracking vs No Tracking; EF Core Transactions; Optimistic Concurrency Fundamentals | Scalar DTO versus entity projection; smallest atomic DB boundary, short transactions and external-side-effect limits; lost update, expected version and explicit resolution |
| API Fundamentals | Authentication vs Authorization; API Error Handling | Identity versus resource permissions; safe, intentional Problem Details contracts and stable error identifiers |

New stable identities:

| Slug | ID suffix | Topic | Technologies | Goals | Level / minutes |
| --- | --- | --- | --- | --- | --- |
| `aspnet-core-configuration-options` | 109 | Configuration | DotNet, AspNetCore | BF, BP | Intermediate / 20 |
| `authentication-vs-authorization` | 110 | Access Control | DotNet, AspNetCore | BF, IP | Beginner / 15 |
| `api-error-handling` | 111 | API Error Contracts | DotNet, AspNetCore | BF, BP | Intermediate / 15 |

ID suffixes use the existing `10000000-0000-0000-0000-{suffix:12 digits}` convention. New global topic IDs 7–9 reuse that convention. Configuration, Access Control and API Error Contracts are durable concepts; separate method-level or duplicate topic tags were not added.

Cancellation's title is now CancellationToken Fundamentals, but its existing `aspnet-core-cancellation-tokens` slug/ID remain stable. It directly teaches .NET/C# cooperative cancellation, with HTTP as an example: Technology is now CSharp/DotNet and its goal is BF only. Transactions now has BF only. Concurrency keeps BF/IP. Other existing technology/topic sets remain unchanged.

Distribution: BF 11/11, IP 6/11, BP 3/11; DotNet 11, AspNetCore 5, EfCore 3, CSharp 2. Broad DotNet coverage is intentional in this single vertical, not an implied match to every ASP.NET-specific profile. Candidates remain 2–4 per lesson, concise and standalone. Initial new lessons were reviewed against objectives, concept ownership, examples, takeaways, recall and metadata before being admitted to the seed's Draft → Published construction.

### In-place editorial upgrades

`LearningContentSeeder.Curriculum.cs` contains the current authored batch; the prior eight definitions in `Samples` are retained only as the exact bounded upgrade baseline. Explicit Development seeding updates an existing Published, DevRecall-owned lesson only when its title, summary, objectives, sections and candidates still match that baseline. An editor's different text is skipped, not overwritten. This guard intentionally requires review for any customized legacy content that still contains an audit issue.

Revisions keep lesson IDs/slugs, objective/section IDs and positions, candidate IDs/keys, publication time and lifecycle. Only source wording and the stated metadata change. Created Knowledge/Review snapshots, progress, completion evidence and scheduling are untouched. This is not automatic source-to-user synchronization. Repeated seeding is a no-op after the upgrade. No migration or new authoring API is required.

### Editorial references

Technical checks use primary documentation, not copied tutorials. Examples are small explanatory fragments, not standalone executable applications:

- [ASP.NET Core middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/): nesting, short-circuiting and security order.
- [Options](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options): binding, validation and cached/scoped values.
- [Cooperative cancellation](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads): cancellation signal versus forced termination.
- [EF transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions), [tracking](https://learn.microsoft.com/en-us/ef/core/querying/tracking) and [concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency): provider boundaries, materialized entities and original tokens.
- [Resource authorization](https://learn.microsoft.com/en-us/aspnet/core/security/authorization/resource-based) and [API errors](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api): server-side permission checks and safe error contracts.

### Authoring batch smoke

Passed one targeted Domain revision test, the existing PostgreSQL seed/progress/evidence/snapshot regression and eleven existing API cases for paging/filtering, two-profile Discover signals and Review snapshot stability. The snapshot assertion now checks the captured original prompt/answer rather than hard-coding editorial wording. No full-suite run or new per-lesson test matrix was added.

The Docker Release API build succeeded and the updated API is healthy. Explicit seed created three lessons, then zero on repeat; complete lesson/section/candidate checksums were unchanged on the repeat. Existing eight IDs/slugs/status/publication times and candidate IDs were preserved, as were live Review rows. Live progress/evidence are empty; preservation with real Completed state and a sourced Review snapshot is covered by the PostgreSQL regression.

The existing Markdown parser successfully processed all ten sections across Configuration/Options and API Error Handling, including C# and JSON code fences. This was a parser smoke, not visual browser QA or real timed learner dogfooding. Real curriculum dogfooding and qualitative friction assessment remain the next checkpoint.

## Historical baseline — before the authored batch

Scope: one small vertical, currently eight Published lessons. Target is 8–12 useful lessons, not a quota. Clusters below are editorial thinking aids, not entities, persisted ordering or prerequisite rules.

## Inventory and concept ownership

Goals: **BF** = ImproveBackendFundamentals; **IP** = PrepareForInterviews; **BP** = BuildProjects. Minutes are editorial reading/example/understanding allowances, not recorded study time.

| Lesson / stable slug | Technology | Topic | Goals | Level / min | Quality decision and ownership |
| --- | --- | --- | --- | --- | --- |
| DI Fundamentals / `dependency-injection-fundamentals` | .NET | Dependency Injection | BF, IP | Beginner / 10 | IMPROVE: owns composition and explicit dependencies; needs a concrete before/after construction example |
| Service Lifetimes / `aspnet-core-service-lifetimes` | .NET, ASP.NET Core | Dependency Injection | BF, IP | Intermediate / 15 | KEEP: owns instance reuse and captive dependencies; not a repeat of DI composition |
| Middleware Pipeline / `aspnet-core-middleware-pipeline` | .NET, ASP.NET Core | Request Pipeline | BF, BP | Intermediate / 15 | IMPROVE: owns ordering, next and short-circuiting; correct the static-file ordering overstatement |
| async/await / `async-await-fundamentals` | C#, .NET | Asynchronous Programming | BF, IP | Beginner / 15 | KEEP: owns nonblocking I/O versus parallel execution; retain the completed-task nuance |
| CancellationToken / `aspnet-core-cancellation-tokens` | C#, ASP.NET Core | Asynchronous Programming | BF, BP | Intermediate / 15 | KEEP: owns cooperative request cancellation and propagation, not basic await behavior |
| Tracking / `ef-core-tracking-vs-no-tracking` | .NET, EF Core | Change Tracking | BF, IP | Intermediate / 10 | IMPROVE: owns read/update tracking choice; clarify pure scalar DTO projections versus projections containing entities |
| Transactions / `ef-core-transactions` | .NET, EF Core | Transactions | BF, BP | Intermediate / 20 | IMPROVE: owns database atomic boundaries; distinguish external HTTP effects from database transaction participants |
| Optimistic Concurrency / `ef-core-optimistic-concurrency` | .NET, EF Core | Concurrency | BF, IP | Intermediate / 20 | KEEP: owns stale-write detection and explicit resolution; remove product-specific aside in a later editorial pass |

10 minutes covers narrow introductory/read-query decisions; 15 includes following a control-flow example; 20 includes reasoning through atomicity/conflict scenarios. These are not stopwatch measurements or Review time. Difficulty is unchanged: introduction versus framework/lifecycle tradeoffs, not estimated mastery.

## Six-question quality review

Order: clear concept / observable objectives / explains why / useful example / useful takeaway / worthwhile recall. All eight have one owned concept, three observable objectives, a concrete C# example, one takeaway, and 2–4 standalone candidates. Each has 3–5 sections. No lesson fails three criteria; none needs a wholesale rewrite or quota-driven replacement.

- DI: five clear passes; example is illustrative but too skeletal (an empty class). Keep current short lesson and prioritize before/after example improvement.
- Lifetimes: six passes; registration supports per-request/state-sharing explanation. Review asks lifetime behavior and captive dependency, not only registration syntax.
- Middleware: structure and recall are sound, but one technical overstatement needs correction. `UseRouting` alone is not a terminal component that makes later static-file middleware impossible; reason about earlier short-circuiting instead. [Official middleware guidance](https://learn.microsoft.com/aspnet/core/fundamentals/middleware).
- Async and cancellation: six passes; one releases waiting threads, the other stops abandoned work. No redundant learning objective. Avoid implying every await yields: the text already specifies an unfinished operation.
- Tracking: concept/why/takeaway/recall pass; strengthen the example explanation. A DTO projection with no entity instances does not require tracking; projections containing entities can still track them. [Official tracking guidance](https://learn.microsoft.com/en-us/ef/core/querying/tracking).
- Transactions: objectives/example/takeaway/recall pass, but the paragraph grouping external calls with DB steps is misleading. A local EF transaction cannot automatically roll back a remote HTTP side effect. Keep next authoring pass scoped to DB participants, not a distributed-transaction lesson. [Official EF transaction boundaries](https://learn.microsoft.com/en-us/ef/core/saving/transactions).
- Concurrency: six passes; exception handling intentionally shows a decision point, not a complete retry solution. Generalize the DevRecall aside later.

These editorial findings are an explicit IMPROVE backlog, not a claim that seeded bodies were rewritten today. Existing source candidates and user-owned snapshots remain unchanged.

## KEEP / IMPROVE / ADD / DEFER

**KEEP:** all eight stable IDs/slugs, existing useful objectives/takeaways/candidate keys, distinct concept ownership. No duplicated lesson serves the same learning need; DI→lifetimes, await→cancellation and transaction→concurrency are complementary.

**IMPROVE first:** middleware wording and transaction boundary ambiguity; tracking projection nuance; DI example; then shorten the async/cancellation takeaways and remove the concurrency product aside. Technical corrections take precedence over cosmetic expansion.

**ADD, in priority order (not implemented yet):**

1. API Error Handling: expected versus unexpected errors and consistent Problem Details; bridges cancellation/concurrency to API behavior.
2. Authentication vs Authorization: identity versus permission checks; connects to middleware ordering.
3. REST API Fundamentals: resource/method/status-code semantics; generic, technology-neutral if appropriate.
4. Configuration and Options: typed options and validation; completes the application-foundations cluster.

Informal clusters: Application Foundations (DI/lifetimes/middleware, future options); Async/Reliability (await/cancellation); Data Access (tracking/transactions/concurrency); API Fundamentals (three planned lessons). There is no REST lesson today; illustrative rows from the plan are not inventory facts. Stop below twelve if authoring quality suffers.

**DEFER:** React/Java/Python, Docker curriculum, DSA expansion, system design/microservices, Course/LearningPath, prerequisite graph, CMS and external ingestion. No source-file extraction yet: eight code-authored lessons are still manageable.

## Metadata cleanup and distribution

DI teaches framework-level dependency composition, not ASP.NET-specific request behavior: normalize its single Technology from AspNetCore to DotNet. Other technology/topic/goal sets remain truthful, with no co-usage tag inflation or new topic just to influence a card. Six global concepts remain; Cancellation can reuse the asynchronous concept without adding an unused taxonomy bridge.

After cleanup: BF 8/8, IP 5/8 (62.5%), BP 3/8. Broad BF is intentional because this is one foundation vertical. Technologies: .NET 7/8, ASP.NET Core 3/8, EF Core 3/8, C# 2/8. Topic counts: Dependency Injection 2, Asynchronous Programming 2, each remaining topic 1. A topic used once is still a durable concept, not an API-method label.

DI no longer matches an ASP.NET-only profile by Technology, but still matches an explicit backend/interview goal. This is semantic cleanup, not score tuning. Broad .NET focus remains broad by design; canonical weak-topic mapping remains unavailable. Weights and reason policy are unchanged.

## Safe seed maintenance

Explicit Development seeding upgrades known DevRecall-owned ID/slug metadata in place. DI's exact old technology set and exact old duration values are recognized independently; nonmatching customized fields are preserved. It does not change status, publication time, bodies/objectives/takeaways, candidate keys/IDs, or user progress/evidence/provenance. Repeated seeding performs no further version bump after normalization. A custom value indistinguishable from the old seed value is necessarily treated as legacy; this is not a general metadata synchronization API.

Authoring standards: [content-authoring.md](content-authoring.md). Historical Week 6 audit tables describe the earlier metadata, not current estimates.

## Verification checkpoint

- Targeted checks passed: one Domain metadata test, the extended seed preservation regression, three Discover persistence tests and three Discover API tests. No full-suite or browser run was needed for this metadata/documentation change.
- API Release image built successfully and the recreated Docker API is healthy. Explicit Development seeding twice created zero extra lessons; the second run preserved the complete lesson-row checksum, including versions.
- Live database checks preserved all eight lesson identities/slugs/status/publication timestamps and the existing six Review rows. The live catalog has no lesson progress/evidence yet; the PostgreSQL regression separately verifies an existing Completed progress and completion evidence survive normalization, alongside candidate identities and customized metadata.
- No migration, ranking algorithm change or new lesson was introduced. The prioritized content corrections and additions remain the next authoring work, not completed features.
