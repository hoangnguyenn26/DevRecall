# DevRecall V2 — Week 5 Validation

## Decision

**Pause before Week 6 and dogfood the current loop.**

The foundation is ready for daily use: Learn → Complete → Knowledge / Review works without a P0 issue,
preserves user control, and keeps completion separate from recall performance. Discover is not yet the
largest user problem. The catalog currently has only three concise lessons, so ranking or recommending
them would add machinery before there is a meaningful choice problem.

The next product investment should be driven by real use. If lesson choice becomes the bottleneck after
a coherent 5–8 lesson .NET backend curriculum exists, Week 6 Discover is justified. Until then, improve
content depth and continue dogfooding rather than adding Recommendation, AI, mastery, paths, timers, or
section progress.

## Scope tested

The dogfood account completed these user-visible scenarios without opening developer tools first:

1. Opened Learn from navigation, scanned the catalog, started **EF Core Tracking vs No Tracking**, left
   it In Progress, returned to Learn, and resumed it from the prominent Continue Learning section.
2. Completed the lesson, edited only the suggested note title, kept the deterministic Key Takeaway draft,
   saved one Knowledge note, previewed two Review answers, and selected both candidates.
3. Completed **Dependency Injection Fundamentals** and returned to Learn without saving Knowledge or
   adding Review. The UI used no guilt or blocking copy.
4. Used the lesson-derived Service Lifetimes cards in Focus Review without rereading first, rated one
   `Good` and one `Again`, then opened the source lesson. The saved rating survived and the lesson stayed
   Completed.
5. Opened Learning History and Analytics to verify that past learning is understandable without implying
   mastery. Study Plan was not used because planning another lesson did not arise naturally.

The database smoke after dogfooding showed three progress rows, three completion evidence rows, one
lesson-sourced Knowledge note, four lesson-sourced Review cards, and two Review outcomes. Completion and
evidence counts matched; card creation had not fabricated Review outcomes.

## What worked

- Learn presents useful metadata and a direct Start action without setup or mode selection.
- Continue Learning has the right priority and refreshes immediately after start/completion changes.
- Restarting a short lesson at the top was acceptable; scroll-position persistence is not justified yet.
- Completion clearly becomes **Lesson completed → Keep what matters** with two optional retention actions.
- The Key Takeaway draft was useful with only a title edit; it did not require rewriting from scratch.
- Review selection needed no interval, deck, priority, or scheduler configuration.
- Answer Preview helped reject or accept a card before creation without creating evidence.
- EF Core candidates were clear, standalone, important, answerable, and worth seeing again.
- Review provenance gave a useful path back to context after weak recall without changing Review state.
- History answers “what have I studied?” and Analytics explicitly describes completed lessons rather than
  skill gained.

## Friction found

### P0 — Blocking

None found. Completion, Knowledge persistence, Review creation/rating, Continue Learning, and source
revisit all completed successfully.

### P1 — High friction

| Issue | Status | Resolution |
| --- | --- | --- |
| Optional Topic and Tags looked as important as the actual note | Resolved | Moved them behind an accessible **Additional details** disclosure. Title and Note are now the default flow. Options load only when requested. |
| Catalog is too small and lessons are too concise to prove Discover value | Deferred intentionally | Treat content quality/coverage as the next validation input. Build a coherent 5–8 lesson curriculum through the existing development seeder only when content work resumes. Do not build Discover yet. |

## Fixes completed

- Moved optional Knowledge organization behind an accessible **Additional details** disclosure, so Title and Note remain the default post-lesson action.
- Made Topic and Tag option loading demand-driven rather than part of opening the Knowledge dialog.
- Kept completion, personal Knowledge, Review-card creation, Review outcomes, and analytics as separate facts throughout the flow.

## Deferred items

### P2 — Minor friction

| Issue | Status | Reason |
| --- | --- | --- |
| History timestamps are precise and somewhat visually verbose | Deferred | Rows remain easy to scan and correctly answer the history question. |

### P3 — Nice to have

| Issue | Status | Reason |
| --- | --- | --- |
| Resume last scroll position | Deferred | Existing lessons are short and restart-at-top was acceptable. |
| Related lessons, history search/filter, paths, AI summaries/questions | Won't fix now | None blocked the observed learning or retention flow. |

## Technical gate

The Day 7 clean gate passed on 2026-08-16.

| Gate | Result |
| --- | --- |
| `dotnet clean` and `dotnet restore` | Pass |
| .NET format verification and build | Pass — 0 warnings, 0 errors |
| Full backend test suite | Pass — 845 tests |
| Frontend type-check and lint | Pass |
| Full frontend test suite | Pass — 158 tests |
| Nuxt production build | Pass |
| Fresh PostgreSQL migration and second migration run | Pass — 39/39 migrations applied |
| Learning-content seed run twice | Pass — 3 published lessons, no duplicates |
| Demo seed run twice | Pass — one demo user; second run made no changes |
| Fresh-database duplicate audit | Pass — 0 duplicate progress rows, completion evidence rows, and lesson-review sources |

The fresh-database work used the dedicated `devrecall_week5_validation` database. It did not overwrite the local database used for dogfooding.

## Exit criteria

| Criterion | Result |
| --- | --- |
| Begin without authoring learning material | Pass — published lessons are immediately available. |
| Find, start, resume, and complete a lesson | Pass. |
| Preserve a concise personal note | Pass — editable deterministic draft and retry-safe save. |
| Create retrieval practice without authoring every card | Pass — explicit candidate selection. |
| Recall later through the normal scheduler | Pass — generic Review flow and intervals. |
| Preserve user control | Pass — no automatic Knowledge, Review, mastery, or topic mapping. |
| Recover ongoing work | Pass — Continue Learning is prominent and current. |
| See past learning | Pass — History and Analytics use completion evidence. |
| Avoid duplicate or fake evidence | Pass — completion, Knowledge, Review, and Analytics counts remain separate. |
| Make lesson discovery the next proven bottleneck | Not yet — catalog scale is insufficient. |

## Product gate

The core product claim passes at the current scale: a user can open curated material without first
authoring notes or cards, complete it explicitly, retain only what is useful, and recall selected concepts
later through the established scheduler. A weak recall result remains valid performance evidence and does
not rewrite the historical fact that the lesson was completed.

The remaining P1 is curriculum depth, not a missing workflow mechanism. Current lesson selection remains
simple enough to browse manually, and the observed flow did not create a need for a recommendation or
discovery layer.

## Product conclusion

DevRecall now proves the core claim at a small scale: a user can learn provided material, keep only the
parts they value, and practice worthwhile recall later without manually creating every note and question.
The mechanics are ready to dogfood. The honest next checkpoint is content usefulness over repeated daily
use, not a new discovery algorithm.
