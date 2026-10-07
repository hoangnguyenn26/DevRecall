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

`LearningContentSeeder.Curriculum.cs` contains the current authored batch; the prior eight definitions in `Samples` are retained as one exact bounded upgrade baseline. Explicit Development seeding updates an existing Published, DevRecall-owned lesson only when its title, summary, objectives, sections and candidates still match the original eight-lesson definition or the preceding authored-batch definition. An editor's different text is skipped, not overwritten. This guard intentionally requires review for any customized legacy content that still contains an audit issue.

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

At the authoring-batch checkpoint, the Docker Release API build succeeded and the updated API was healthy. Explicit seed created three lessons, then zero on repeat; complete lesson/section/candidate checksums were unchanged on the repeat. Existing eight IDs/slugs/status/publication times and candidate IDs were preserved, as were live Review rows. Live progress/evidence were empty at that checkpoint; preservation with real Completed state and a sourced Review snapshot was covered by the PostgreSQL regression. The later dogfood checkpoint below also checks live user state.

The existing Markdown parser successfully processed all ten sections across Configuration/Options and API Error Handling, including C# and JSON code fences. This was a parser smoke, not visual browser QA or real timed learner dogfooding. Real curriculum dogfooding and qualitative friction assessment remain the next checkpoint.

## Curriculum dogfood checkpoint

**What worked:** An isolated local QA account used a Junior Backend Developer profile, .NET primary with C#/ASP.NET Core/EF Core focus, backend/interview goals and a 30-minute allowance. Starting from Discover (not source order), initial choices were Auth/Authz, async/await, DI and Options. Completing Auth/Authz replaced it with another relevant lesson; completing Options made API Errors and Concurrency more prominent. Reasons matched declared goals/focus, with no irrelevant padding or weakness claims.

**Learning flow:** Browser-read Auth/Authz, Options and async/await. Opening did not Start; explicit Start and Complete worked. Auth/Authz's takeaway made a useful short Knowledge draft, saved as an independent note. Two deliberately selected concepts entered normal Review. Later in the same walkthrough, without reopening the source, prompts were standalone and answers concise. Good scheduled two days; Again scheduled one. The source CTA appeared after rating. A started-but-uncompleted async lesson was prominent in Continue Learning; History showed exactly two completed lessons in chronological order. No additional navigation system was justified.

**Content issues:** P1 semantic inconsistency in async: summary/comment implied unconditional thread release even though the body qualified incomplete tasks; one paragraph promised “the next lesson” without a learning path. Shortened the summary and clarified incomplete-task behavior/cancellation as a related concept. No estimates were changed from a rapid agent walkthrough: these are editorial allowances, not measured human study time.

**Navigation issues:** P1 Browse action said “Start lesson” although it only opens the reader; changed to “Open lesson”. Continue and Read again remain unchanged. P2: repeated goal reasons are unsurprising for this single foundation vertical; existing technology/topic chips already make cards distinguishable. Keep flat catalog plus existing filters. No new chips, groups, search or primary-topic domain property.

**Gaps:** No observed Critical gap blocked this flow. REST foundations are Useful, not a quota-driven addition. Caching, background jobs, observability, formal sequencing and feedback remain Later. Options and API Errors already exist, so neither was re-authored as a duplicate.

**Days 5–6 decision:** Curriculum **Ready for the internal-foundation checkpoint**; navigation **Sufficient**; Discover **Useful** for this persona. The next bottleneck is broader reference depth and repeated human use, not another scoring mechanism. No external ingestion was implemented here; the closure decision follows below.

This was an agent-operated product/interaction dogfood, not proof of human learning or next-day retention. The Good/Again inputs intentionally exercised scheduling outcomes; they are not measured recall ability. True delayed cold recall and timing remain user validation. The disposable account/data are retained locally for reproducibility, separate from the demo user's data; no credentials are committed.

The async polish upgrades either the exact original eight-lesson text or the exact prior authored-batch text, never an editor's different wording. IDs/slugs, candidate keys/IDs, completion/evidence, saved notes and Review snapshots remain independent. A source text revision does not reopen a Completed lesson.

**Verification:** Existing learning-content component/Markdown suite: 13 passed; targeted PostgreSQL seed preservation regression: 1 passed; ESLint for the two touched frontend files passed. Docker API/web production builds succeeded and both services are healthy. Live checksums preserved progress/evidence, Knowledge notes, Review items/source snapshots and all lesson/candidate identities across the polish seed. Two seed runs created zero lessons; the repeated run preserved full lesson/section checksums, including versions. Browser re-check confirmed Open lesson, the shorter async summary and unchanged In progress state. API container replacement required QA re-login; learning data remained intact.

Reference for the async clarification: [C# asynchronous programming](https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/). No new test matrix, API contract, schema, ranking policy or lesson was added.

## Internal learning foundation — final checkpoint

Logical checkpoint: `v2-week-07-internal-learning-foundation` (a product milestone, not an automatically created Git tag).

### Curriculum status

- Published, editorially strong: **11**; Draft / identified as needing further polish: **0** in this curated seed catalog.
- Critical content gaps for the current learning loop: **none observed**.
- Useful future gap: REST API Fundamentals. Later possibilities: caching, background jobs and observability, only when demand justifies them.
- Four coherent clusters are represented. There is no major duplicate concept ownership, formal curriculum completion percentage or inferred topic mastery.

### What improved

Three new lessons cover configuration/options, authentication/authorization and API error contracts. Existing explanations now focus on control flow, cooperative cancellation, tracking, atomic boundaries and lost updates. Takeaways are concise note seeds; candidates test standalone concepts rather than registration trivia. Metadata differentiates backend, interview and project intent without changing Discover's deterministic ranking.

The production-browser walkthrough exercised Discover → explicit Start → Complete → Knowledge/Review, Continue and History. Source snapshots and scheduling remain independent of later lesson edits. Existing scoped tests and seed checks cover ownership, lifecycle and stable identities; the verification record above distinguishes actual checks from intended behavior.

Final closeout smoke: two existing Discover API cases passed (`Discover_ShouldBeAuthenticatedReadOnlyAndExcludeOnlyTheCurrentUsersProgress` and `DiscoverRanking_ShouldUseExactTechnologySignalsWithoutExposingScoresAndRefreshAfterProfileChange`). These cover read-only/authenticated behavior, current-owner progress exclusion, an EF-focused secondary profile, exact signals and profile-change freshness. Browser re-check confirmed the QA account's started/completed lessons remain absent from Discover and its flat catalog retains Completed/In progress states. Reopened Options still rendered objectives, code and takeaway, then saved a second independent Knowledge note from its takeaway (in addition to Auth/Authz). The success dialog retained the lesson-completed context. API, web and PostgreSQL were healthy. No new test suite, full regression or production rebuild was needed for this documentation-only closeout.

### Remaining validation and friction

Delayed human cold recall and actual reading time remain **pending user validation**. Agent-operated Good/Again submissions demonstrate transitions, not memory performance. Completion remains a historical action, never mastery. Review performance supplies only existing valid attribution; global content topics are not silently mapped into personal Weak Topics.

Flat catalog, existing filters, Continue and History are sufficient at this scale. No repeated sequencing problem was established. Code authoring remains manageable, with baseline duplication and escaped Markdown recorded as technical friction to watch rather than justification for a CMS.

### Product bottleneck and decision

**Decision: Close the internal learning foundation phase.** The current .NET backend curriculum is sufficient to validate Learn, Discover, Knowledge and Review together. Further internal lesson expansion will be demand-driven. This is a product/technical checkpoint, not a declaration that human learning effectiveness has been measured.

### Next phase

**Curated External Learning Resources** is next (chronologically Week 8). Begin with semantics, manual records, provenance and licensing-safe metadata; then narrowly integrate Learn/Discover and dogfood source UX. The plan remains revisable after its first two days.

Internal lessons teach directly; external resources point to trusted deeper or primary material. Prefer official sources and keep short original descriptions, not copied articles. Initial direction: Open external source only, without completion, evidence, Active Days or StudyMinutes. No crawler, provider sync, AI summaries, recommendation feedback, LearningPath or automatic Study Plan integration. No next-phase feature is implemented by this closeout.

## Historical baseline — before the authored batch

Historical scope: one small vertical with eight Published lessons at that time. Target was 8–12 useful lessons, not a quota. The audit/backlog below records the pre-authoring state, not unresolved current defects or today's inventory. Clusters are editorial thinking aids, not entities, persisted ordering or prerequisite rules.

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
