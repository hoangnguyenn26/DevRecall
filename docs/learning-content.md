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
| `POST` | `/api/v1/knowledge/from-learning-content/{slug}` | Explicitly saves an editable personal Knowledge note with immutable lesson provenance. |
| `GET` | `/api/v1/study-plans/learning-content/{slug}/options` | Lists editable plans and existing lesson membership in one bounded projection. |
| `POST` | `/api/v1/study-plans/{planId}/learning-content/{slug}` | Ensures a published, unfinished lesson is present in an owned Draft plan. |

## Saving a lesson to Knowledge

Completing a lesson never creates Knowledge automatically. After completion, the user may open the
“Save to Knowledge” flow, edit a title and note body, and optionally select an existing Knowledge topic
and tags. The request includes a client-generated `submissionId`; retries with the same user and
submission ID return the original note, while a new submission ID may create another note from the
same lesson.

The saved note is a snapshot and remains independently editable. Its detail exposes only public source
metadata (`type`, title snapshot, slug, and availability). If the lesson is later archived, the source
title remains visible but the UI does not link to the unavailable lesson. This workflow does not create
topics, tags, Review items, recommendations, or analytics signals.

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

## Learn experience

Authenticated users browse published lessons at `/app/learn` and read an individual lesson at `/app/learn/{slug}`. Technology and difficulty filters are URL-driven, use AND semantics, and reset pagination to page one when changed. Browser history therefore retains filter and page context.

The catalog is learning-oriented rather than an administration table. Cards expose title, summary, difficulty, estimated time, progress state, and a small number of meaningful technology/topic tags. The reader keeps the normal application shell but bounds the reading column, distinguishes explanation, large code example, and key-takeaway sections, and provides no editing controls or fake percentage progress.

Opening or reading a lesson creates no progress. Starting records intent only. Completion creates one immutable learning-completion evidence record without fabricating study duration or practice performance.

The reusable Learn Markdown renderer creates Vue text nodes and safe elements rather than injecting generated HTML. Raw HTML remains visible as text, executable URL schemes are not linked, external HTTP/HTTPS links receive `noopener noreferrer`, code fences scroll horizontally, and copy failures remain isolated to the button.

## Progress and completion semantics

- Opening a lesson is read-only; `NotStarted` is represented by the absence of a progress row.
- Starting is explicit and idempotent. It records learning intent, not completion evidence.
- Completing is explicit and may directly complete an unstarted lesson.
- Completed progress is immutable in the MVP; reading again never resets it.
- Completion and one immutable, title-snapshotted evidence record are saved atomically.
- A retry after completion returns the canonical state without duplicate evidence.
- Progress is always scoped to the authenticated user; requests never accept a user id.

Estimated duration is metadata, not actual study time. Progress does not create Review items, Weak Topics,
Knowledge notes, Recommendations, or Interview/DSA practice activity.

## Progress recovery and learning history

The Learn home has three distinct responsibilities:

- **Continue learning** shows at most five Published lessons whose current user's progress is `InProgress`, newest `StartedAtUtc` first. Browse filters do not change this list.
- **Browse lessons** remains the deterministic Published catalog. In-progress lessons may also appear here because browsing and recovery answer different questions.
- **Learning history** at `/app/learn/history` is a paginated, newest-first record of immutable completion evidence.

Simply opening a lesson never makes it eligible for Continue learning and does not update a last-viewed timestamp. Completing a lesson removes it from Continue learning and adds one history entry. Reading it again does not create another evidence record, change the original completion timestamp, or increment Analytics.

History uses the completion evidence title snapshot. A later archived source remains visible as historical fact but is marked unavailable and has no broken lesson link. Continue learning excludes Draft or Archived sources while preserving their underlying progress rows. Both reads are owner-scoped and use no read-side mutation.

### Progress/evidence consistency

For each `(UserId, LearningContentId)`, normal state is restricted to:

- no progress and no evidence (`NotStarted`);
- `InProgress` progress and no evidence;
- `Completed` progress and exactly one completion evidence.

Completion writes progress and evidence in one `SaveChanges` transaction. PostgreSQL uniquely constrains both progress and evidence per user/lesson, and concurrent completion resolves to the canonical row without changing the original completion time or version. History and Analytics read evidence; Continue learning and catalog status read progress.

Normal reads never repair inconsistent data. Start or Complete returns the stable `LEARNING_CONTENT_COMPLETION_INCONSISTENT` conflict when it encounters mismatched progress/evidence. Known Development data can be repaired explicitly with:

```powershell
dotnet run --project src/DevRecall.Api -- --repair-learning-content-consistency
```

The command is Development-only and idempotent. It uses the trusted progress completion timestamp when evidence is missing; when evidence already exists, that historical fact wins and the current progress is repaired to Completed. It is not a background service and never runs during API startup.

## Review candidates and Learn → Review

A published lesson may expose zero to five ordered review candidates. Each candidate has a stable,
lowercase hyphenated key plus a concise prompt and answer. Candidates are platform suggestions, not
user Review items: completing a lesson never creates cards automatically.

After completion, the user can explicitly select one or more candidates and submit them through
`POST /api/v1/review/from-learning-content/{slug}`. The batch is atomic and retry-safe through one
client-generated `submissionId`. Active cards are unique per user and candidate; selecting an existing
candidate is a successful no-op, while archiving that Review item makes the candidate available again.

Lesson-created Review items reuse the existing Review scheduler and start with its normal defaults.
Their prompt, answer, and lesson title are snapshots, so later lesson edits do not alter an existing
card. Adding a card is preparation rather than recall evidence: it does not create Review history,
change lesson completion, update Weak Topics, or record an evaluation outcome.

## Study Plan and Study Session integration

An unfinished published lesson can be explicitly added to an existing Draft Study Plan. The plan stores
the lesson UUID and duration metadata; it never copies objectives, sections, or Markdown. Duplicate
references in one plan are success-equivalent, while separate plans may reference the same lesson.
Adding a lesson does not start or complete it and creates no analytics activity. Completed lessons are
excluded until repeat-learning attempts exist; Review and Read again cover the current retention flow.

Plan conversion maps the item to the stable `LearningContent` Study resource type. Opening the lesson
from a Study Session carries navigation context only. Lesson completion returns the canonical evidence
ID, and the client separately completes the Session item. The Study module validates the user, lesson,
and that the evidence was created no earlier than the Session start.

## Writing a good lesson

- Teach one primary concept; split broad subjects into separate lessons.
- Target roughly 10–20 minutes, with 2–4 specific and observable objectives.
- Use 3–6 natural sections: explanation, a focused concrete example, a common mistake or nuance, and a clear takeaway.
- Explanation sections may include small code fences. Use `CodeExample` when the whole section is a substantial example.
- Prefer conceptually compilable examples over full-application snippets.
- Use one to three meaningful technologies and topics; do not tag every adjacent concept.
- Write a summary that answers “What will this teach me?” rather than generic promotional text.

## Future boundaries

Future matching may compare Learning Profile signals with content Technology, Topic, Difficulty, and Estimated Minutes. No matching or ranking algorithm exists yet.

The following remain out of scope: Discover, automatic or AI-generated Review cards, section progress,
timers, saved scroll position, relearning sessions, authoring APIs, content providers, crawlers, AI,
attachments, and public lesson SEO.
