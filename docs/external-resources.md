# Curated external learning resources

## Purpose and ownership

DevRecall teaches concepts through internal Lessons and curates trusted deeper references through ExternalResources. Both reuse the global `LearningContent` root; there is no new aggregate, Course, relation table or ingestion pipeline.

An external resource stores curator-owned metadata, not the publisher's article. Title stays close to the destination title. Summary is a short original description of why to open it. Technology, conceptual Topic, canonical Goals, difficulty and approximate reading allowance are manually reviewed. Time is planning metadata, never actual StudyMinutes. Source authority is not inferred from a URL or the Documentation kind.

## Provenance and publishing

`ExternalResource` requires `SourceType.External`, a nonempty SourceName, an absolute HTTP/HTTPS SourceUrl (up to 2,048 characters; no credentials), and ResourceKind: Documentation, Guide, Tutorial or Reference. Internal Lessons use Internal provenance and have no ResourceKind. Published resources require a topic; they have no objectives, sections or Review candidates. The resource kind/type/source pairing is also constrained in PostgreSQL.

Lifecycle is Draft → Published → Archived, with republishing retaining the initial publication timestamp. Curators inspect destination, title, source, summary and metadata before publishing. Archive broken, outdated or unsuitable links rather than replacing identity. There is no background link checker.

Slug is unique; URLs are not made database-unique or aggressively canonicalized. Manual curation must avoid duplicate records for the same page. IDs/slugs remain stable; explicit Development seeding inserts missing records and does not overwrite an existing curated resource or unarchive it.

The ResourceKind migration adds a nullable column without modifying lesson identities. There were no external rows in the deployed pre-migration catalog. An installation with manually inserted legacy external rows needs a reviewed data-migration adaptation to classify them before enforcing the constraint; the default migration fails rather than guessing their kind or removing their bodies.

## Browse and detail contract

Authenticated `GET /api/v1/learning-content` defaults to all Published content. Optional `contentType=Lesson|ExternalResource` combines with existing technology/topic/difficulty filters and bounded pagination; numeric/unsupported enum strings are rejected. Stable publication/UUID ordering is unchanged. List items expose `resourceKind` and `sourceName`, not an outbound URL.

Resources have `progressStatus: null` on lists and `progress: null` on detail. Detail uses the same `/api/v1/learning-content/{slug}` endpoint and DTO, with empty objectives/sections/candidates and source metadata. Lessons retain their existing progress semantics and bodies. Draft/Archived resources are not found through public catalog/detail reads.

`/app/learn` has All / Lessons / Resources filters, URL-driven as `type=lesson|resource`. Cards show kind/source and approximate time, never a resource progress badge. `/app/learn/{slug}` renders a resource-specific view with no fake lesson body. **Read on Microsoft Learn** opens the checked HTTP/HTTPS URL in a new tab with `noopener noreferrer`; the notice explicitly explains leaving DevRecall. Filter context is retained on return.

## Navigation is not evidence

Detail GET and external link opening are navigation only. There is **no Start, Complete, progress row, completion evidence, Continue entry, History entry, Active Day or StudyMinutes** from resource opening. There is no outbound-click/impression tracking or new resource state machine.

Direct Start/Complete API calls reject resources with `409 LEARNING_CONTENT_LESSON_REQUIRED`. Lesson-to-Knowledge and lesson-to-Review source lookups exclude resources; Study Plan lesson source/options also exclude them. Thus hiding UI actions is not the only boundary. No Knowledge, Review or Study Plan integration is enabled for resources yet. Discover now projects a separate, read-only `trustedResources` array; see [resource ranking and intent separation](discover.md#trusted-external-resources). It does not change these boundaries.

## Initial manual curation

Five Documentation records, SourceName Microsoft Learn, reviewed against these official pages:

| Stable slug | Destination | Curator intent / approximate minutes |
| --- | --- | --- |
| `aspnet-core-middleware-documentation` | [ASP.NET Core middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0) | Request delegates, ordering and short-circuiting / 20 |
| `aspnet-core-dependency-injection-documentation` | [Dependency injection in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0) | Framework registrations and service-lifetime usage / 25 |
| `dotnet-cancellation-in-managed-threads` | [Cancellation in Managed Threads](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads) | Cooperative observation patterns / 20 |
| `ef-core-tracking-documentation` | [Tracking vs. No-Tracking Queries](https://learn.microsoft.com/en-us/ef/core/querying/tracking) | Tracking, identity resolution and projection choices / 15 |
| `ef-core-handling-concurrency-conflicts` | [Handling Concurrency Conflicts](https://learn.microsoft.com/en-us/ef/core/saving/concurrency) | Token configuration and conflict handling / 20 |

Stable IDs use existing seed suffixes 201–205, distinct from lessons 101–111; canonical topics are reused. Sources were checked manually, not ingested. Run the existing explicit Development `--seed-learning-content` CLI after applying migrations. No startup seeding.

## Deferred scope

Public access is not permission to redistribute. No full article/HTML/Markdown copying, scraping, crawler, OpenGraph preview, iframe, logos, provider API/sync jobs, AI summaries or automatic tag extraction. Broader external engagement, study evidence, personal notes and saved/read states require separate product decisions.

## Foundation verification

Scoped checks passed: 18 LearningContent Domain cases, 20 API smoke cases (catalog/detail/progress including the resource no-evidence boundary, plus a few existing management-list checks), two existing PostgreSQL seed/Discover checks, and 14 Learn component/Markdown cases. Three API cases were rechecked after adding detail goals. Frontend type-check and lint for touched files passed. No full suite or browser UI check was run during these implementation days; visual review remains for the requested Day 7 checkpoint.

API and Nuxt production images built successfully and all three Docker services are healthy. The existing Nitro dependency unused-import warning remains; no dependency upgrade was made. The explicit live migration applied, then seeding added exactly five resources. Repeat seed added zero and preserved complete content-row/candidate-row checksums. Existing lesson identity/version checksum, three progress rows and two completion-evidence rows were unchanged. Authenticated same-origin production API smoke confirmed two EF Core resources under combined filters, null progress, Documentation kind, empty sections and canonical goals.

The existing Discover fixture now chooses lesson-only progress sources and expects its bounded four-plus-three output for the 11-lesson catalog, rather than the old eight-lesson cardinality. Its unpublished/external exclusion and owner-state assertions remain intact. Discover weights and eligibility policy were not changed.

## Discover integration verification

Scoped checks passed: 14 Discover application cases, 14 Discover frontend cases, four API smoke cases and one PostgreSQL reader case. The existing API scenario additionally completes all EF-focused lessons and confirms the two EF resources remain; changing to an unrelated technology/goal clears both pools. The reader verifies published external eligibility, archive exclusion, unchanged lesson results, no tracking and no progress/evidence writes. No full suite or browser check was run.

Type-check, touched-file ESLint/oxlint and API/Nuxt production builds passed. The pre-existing Nitro unused-import warning remains. Rebuilt API/web containers and PostgreSQL are healthy. Authenticated production smoke returned four lessons plus four resources for the existing broad .NET/backend profile, verified resource detail has null progress, repeated results are stable and global progress/evidence counts remain 3/2. These are technical/synthetic checks, not a claim that a human found the sources useful after reading them. The planned next dogfood checkpoint should decide whether curation feels complementary; no bookmark, tracking or study integration is inferred from this release.

## Curation acceptance bar

Before publishing, answer: **Why is this resource in DevRecall instead of asking the learner to search the web?** Publish only when it has a trustworthy source, a specific destination, useful depth beyond the internal lesson, conservative primary-value tags, a useful original summary and a manually checked stable URL. Prefer a concept-specific page to a documentation home. Archive unsuitable destinations; do not retain them to fill four cards.

Summaries should explain coverage, value and when to use the source in one or two sentences, not reproduce the article or become a mini-lesson. Existing summaries already meet this bar sufficiently; they were kept rather than rewritten for cosmetic churn. Intermediate difficulty remains appropriate for sources assuming framework context. Approximate minutes mean a focused pass through relevant sections, not a guaranteed exhaustive reading of every example/link. Especially DI and cancellation require selective reading; human timing remains unvalidated.

## Days 5–6 editorial and semantics checkpoint

The existing dogfood account/profile was left unchanged. Starting from live Discover returned middleware, DI, cancellation and tracking resources with truthful backend-goal/.NET reasons. Detail was then inspected through the same-origin API; source pages were read with the research tool and compared with the internal lessons. Concurrency was also evaluated through Browse because the independent top-four pool need not expose every relevant resource at once. No weights, authority points or taxonomy mappings were changed to make results look better.

| Resource | Complementary value / editorial assessment |
| --- | --- |
| [Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0) | Internal lesson teaches nesting/order; source adds branching and framework ordering/reference examples. Keep; focus on relevant sections rather than every middleware option. |
| [Dependency injection](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0) | Internal DI/lifetimes establish the mental model; source provides concrete framework usage and registration guidance. There is intentional introductory overlap, but implementation depth justifies keeping it. |
| [Cancellation](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads) | Internal lesson prioritizes request/token propagation; source extends observation mechanisms and token-source coordination. Intermediate, targeted reading; legacy thread examples are not a recommended application architecture. |
| [Tracking](https://learn.microsoft.com/en-us/ef/core/querying/tracking) | Adds identity-resolution and projection nuances beyond the internal read/update decision. Specific destination and 15-minute focused allowance remain reasonable editorial judgments. |
| [Concurrency](https://learn.microsoft.com/en-us/ef/core/saving/concurrency) | Internal lesson explains lost updates/expected versions; source adds provider-specific versus application-managed tokens and conflict-resolution details. SQL Server rowversion examples must not be mistaken for a PostgreSQL default. |

These are editorial assessments, not proof of learner satisfaction, retention or a timed reading session. All five sources stay Published; no weak portal-style destination or clearly unrelated record was found to justify archiving. The research extractor exposed version/access notices alongside readable article content on Microsoft Learn; this does not establish a broken link or actual browser access failure. Destination/version presentation should still be checked in the requested Day 7 browser review.

### Findings and fixes

- **P0:** No evidence mutation found in the exercised read path. Live resource catalog/detail reads preserved full-row fingerprints of progress, completion evidence and Weak Topic profiles; Analytics Overview was identical before/after. Outbound CTA remains a plain checked URL/new-tab anchor, not an API mutation. Browser clicking was not exercised during these implementation days.
- **P1:** No demonstrated curation/value defect requiring removal or metadata changes. Subjective usefulness remains a human dogfood question, not a technically passed gate.
- **P2 (fixed):** Discover resource cards duplicated technology/topic chips alongside reasons. Hide these chips for resources only; retain kind/source, difficulty/approximate time and semantic reasons. Detail retains the fuller context.
- **P2 (fixed):** Shared loading/not-found/error copy incorrectly called external content a lesson. Use neutral learning-content wording. Resource detail now explicitly frames its existing curator summary as **Why this resource**; source remains visible above the title and in **Read on Microsoft Learn**.
- **Later:** Bookmark, planned reading and personal Knowledge notes are distinct needs, not defects. No recurring user need for any of them has been established. Feedback was requested rather than invented.

### Interaction decision

**Selected for now: current resource interaction is sufficient; keep Open-only.** Keep the trusted-resource section, with its value provisional pending real-user reading. No resource state, bookmark domain, analytics, completion, History, Study Plan or Knowledge shortcut is added. Returning to Discover or Browse is a valid end to the flow; persistent resource visibility is not itself a bug.

If the user repeatedly reports “I want to study this later,” Study Plan integration is the next design candidate, not an authorized implementation. It must preserve **StudySessionItem completed ≠ ExternalResource completed**: explicit task completion may record the existing study-task fact, never LearningContent completion evidence or LessonsCompleted. Occasional link revisiting alone does not establish this need. Manual Knowledge creation remains available for personal understanding, not as disguised bookmarking.

**Still pending before product closure:** human 10-second scan/visual navigation assessment, actual external-tab interaction, useful-depth verdict after reading, realistic reading-time feedback and the desired post-resource action. Do not mark those as dogfooded merely because API/component checks pass. Day 7 should use that feedback to confirm closure or identify a specific remaining bottleneck.

Validation stayed lightweight: 28 existing Discover/Learn component cases passed (one resource-card assertion extended, no new suite), frontend type-check and touched-file ESLint/oxlint passed. No backend implementation or schema changed, so no full backend regression was rerun. Live navigation smoke covered five Browse resources and four Discover resources; progress/evidence/weakness row fingerprints and Analytics Overview remained identical. Safe internal return routes and the unchanged new-tab/`noopener noreferrer` source anchor were inspected in code; browser behavior is explicitly pending.
