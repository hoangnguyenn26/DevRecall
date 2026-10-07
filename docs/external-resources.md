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

Direct Start/Complete API calls reject resources with `409 LEARNING_CONTENT_LESSON_REQUIRED`. Lesson-to-Knowledge and lesson-to-Review source lookups exclude resources; Study Plan lesson source/options also exclude them. Thus hiding UI actions is not the only boundary. No Knowledge, Review or Study Plan integration is enabled for resources yet. Discover still ranks Lessons only; resource recommendation integration is deferred to the next requested days.

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

Public access is not permission to redistribute. No full article/HTML/Markdown copying, scraping, crawler, OpenGraph preview, iframe, logos, provider API/sync jobs, AI summaries or automatic tag extraction. Broader external engagement, study evidence, personal notes, saved/read states and recommendation presentation require separate product decisions.

## Foundation verification

Scoped checks passed: 18 LearningContent Domain cases, 20 API smoke cases (catalog/detail/progress including the resource no-evidence boundary, plus a few existing management-list checks), two existing PostgreSQL seed/Discover checks, and 14 Learn component/Markdown cases. Three API cases were rechecked after adding detail goals. Frontend type-check and lint for touched files passed. No full suite or browser UI check was run during these implementation days; visual review remains for the requested Day 7 checkpoint.

API and Nuxt production images built successfully and all three Docker services are healthy. The existing Nitro dependency unused-import warning remains; no dependency upgrade was made. The explicit live migration applied, then seeding added exactly five resources. Repeat seed added zero and preserved complete content-row/candidate-row checksums. Existing lesson identity/version checksum, three progress rows and two completion-evidence rows were unchanged. Authenticated same-origin production API smoke confirmed two EF Core resources under combined filters, null progress, Documentation kind, empty sections and canonical goals.

The existing Discover fixture now chooses lesson-only progress sources and expects its bounded four-plus-three output for the 11-lesson catalog, rather than the old eight-lesson cardinality. Its unpublished/external exclusion and owner-state assertions remain intact. Discover weights and eligibility policy were not changed.
