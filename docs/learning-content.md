# Learning Content

## Purpose

Learning Content is the global catalog of material that DevRecall offers users to learn. It is separate from user-owned Knowledge: Learning Content is platform content; Knowledge is what an individual user explicitly saves and organizes.

This foundation stores and serves lessons. It does not track reading progress, create Knowledge notes, schedule Reviews, or generate Recommendations.

## Content types

The stable content types are `Lesson` and `ExternalResource`. Week 2 supplies internal lessons and prepares the provenance invariant for future external resources without implementing external ingestion.

- A published `Lesson` requires at least one topic, objective, and section.
- An `ExternalResource` uses external provenance, requires a safe HTTP/HTTPS URL, and may have no sections.

## Lesson structure

A `LearningContent` aggregate owns metadata, technologies, topic links, objectives, sections, lifecycle, version, and timestamps. Objectives and sections use zero-based canonical positions assigned by the aggregate; arbitrary client positions are not trusted.

Defensive bounds include 1–480 estimated minutes, at most 10 objectives, at most 50 sections, and at most 50,000 Markdown characters per section.

## Lifecycle

The minimal lifecycle is:

```text
Draft → Published → Archived
          ↑            │
          └────────────┘
```

Republishing retains the first `publishedAtUtc`. Slugs are stable identifiers and are not derived again when a title changes. User-facing readers return only `Published` content; Draft and Archived slugs resolve as not found.

## Technology and topic

Technology describes an ecosystem or tool, while Topic describes a concept. For example, `AspNetCore` is a Technology and `dependency-injection` is a Topic.

Learning Content reuses the exact `Technology` taxonomy introduced for Learning Profile. It does not maintain aliases such as `AspNet`, `ASP.NET`, or `DotNetWeb`. Topics are a small global, flat, application-controlled taxonomy; Week 2 does not implement topic hierarchy or user-created topics.

## Markdown

`bodyMarkdown` is the source of truth. Rendered HTML is not persisted.

Markdown is untrusted content. A frontend must not use `v-html` with raw Markdown. Rendering must disable raw HTML by default or pass generated HTML through a trusted sanitizer. Markdown content is display data and is never executable code.

## Provenance

Every item records `sourceType`, `sourceName`, and optional `sourceUrl`.

- Internal content uses `Internal`, `DevRecall`, and no URL.
- External content uses `External` and requires an absolute HTTP/HTTPS URL.

No external fetching, provider integration, licensing model, or content ingestion is implemented in this foundation.

## Published read API

All endpoints require authentication and expose global content rather than user-owned data.

| Method | Route | Behavior |
| --- | --- | --- |
| `GET` | `/api/v1/learning-content` | Bounded published catalog with optional `technology`, `topic`, and `difficulty` filters. |
| `GET` | `/api/v1/learning-content/{slug}` | Published lesson detail including objectives, sections, and provenance. |

Pagination defaults to 20 and is limited to 50. Catalog order is `publishedAtUtc DESC`, then ID. Technology and difficulty accept case-sensitive canonical strings and reject numeric enum representations. An unknown topic slug returns an empty catalog page.

The list projection excludes objective and section bodies. Detail reads one bounded lesson. Both paths use `AsNoTracking`; no generic repository or user ownership predicate is involved because the catalog is shared.

## Seed content

Development content is created only through the explicit command:

```powershell
dotnet run --project src/DevRecall.Api -- --seed-learning-content
```

The command is guarded to the Development environment and is idempotent by stable slug. Existing lessons are skipped rather than overwritten. It currently creates three usable lessons:

- Dependency Injection Fundamentals;
- ASP.NET Core Service Lifetimes;
- EF Core Tracking vs No Tracking.

Content is never seeded automatically during normal API startup.

## Future boundaries

Future matching may compare Learning Profile signals with content Technology, Topic, Difficulty, and Estimated Minutes. No matching or ranking algorithm exists yet.

The following remain out of scope for Day 1–2: Learn UI, progress, completion, Discover, recommendation, Review creation, authoring APIs, content providers, crawlers, AI, attachments, and public lesson SEO.
