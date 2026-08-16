# DevRecall — Week 6 Curriculum Checkpoint

## Decision

**Week 7: Option B — Continue curriculum + dogfood.**

With eight published lessons across six topics, choosing the next lesson remained trivially easy through
browse and filter alone. No user needed a roadmap, search engine, ranking, or recommendation to decide
what to study. The observed problem is still content coverage and depth, not lesson discovery. Discover
remains an unproven convenience, so building it now would add machinery before a real choice problem
exists.

Week 7 should therefore extend the curriculum toward a coherent .NET Backend Starter path (target roughly
12–15 lessons) and keep dogfooding the Learn → Complete → Remember loop. Discover becomes justified only
if, with that larger catalog, the question "I don't know what to study next" actually starts repeating.

## Scope

Week 6 expanded the Learning Content catalog from 3 to 8 published lessons (.NET Backend Starter
curriculum) and validated the result from a fresh-user perspective:

- Days 1–4 (committed): five new lessons — C# Async/Await Fundamentals, ASP.NET Core Minimal APIs,
  EF Core DbContext & Change Tracking, EF Core Transactions & SaveChanges, EF Core Optimistic
  Concurrency — plus two new topics, endpoint/persistence tests, and seeder updates.
- Day 5: fresh-user dogfood of the full learn loop (browse → choose → start → stop → resume →
  complete → Save to Knowledge → Add to Review → History/Analytics).
- Day 6: delayed cold review of lesson-derived cards, scheduler semantics, completion/evidence
  consistency, responsive 390px, dark mode, and focus visibility.
- Day 7: focused test suites, double reseed idempotency check, and this checkpoint.

## Technical validation

### Focused test results (all green)

| Suite | Focus | Result |
| --- | --- | --- |
| DevRecall.Api.Tests | LearningContent, Review, Knowledge, Analytics endpoints: catalog/detail projection, completion/evidence consistency, post-completion Knowledge/Review behavior, analytics counting and owner isolation | 151/151 passed |
| DevRecall.Domain.Tests | LearningContent progress rules, Review scheduling/history, Knowledge invariants | 75/75 passed |
| DevRecall.Application.Tests | LearningContent consistency, Review handlers, Knowledge handlers, Analytics insights | 102/102 passed |
| DevRecall.IntegrationTests | PostgreSQL persistence for LearningContent, Review, Knowledge (Testcontainers) | 16/16 passed |

### Seeder idempotency

The development learning-content seeder was run twice against the live development database with the API
stopped. Both runs reported `Created 0 lesson(s)` — no duplicate lessons, sections, topics, or review
candidates. All eight lessons and six topics remain keyed by slug.

## Product validation

| Câu hỏi | Evidence | Kết luận |
| --- | --- | --- |
| User có đủ content tốt để bắt đầu học? | Fresh user WD picked **EF Core Transactions & SaveChanges** purely from catalog metadata (topic, difficulty, estimated minutes, summary) and completed it because it was useful in itself; a second lesson (Optimistic Concurrency) was completed the same way. | **Yes** — 8 lessons give a real starting curriculum. |
| Browse/filter có đủ để chọn không? | Both lessons were chosen via topic/difficulty filters and summaries, with no roadmap, search, or decision support. Zero hesitation. | **Yes** — browse/filter is sufficient at this catalog size. |
| Discover có phải pain point thật? | The sentence "tôi không biết học gì tiếp theo" never occurred while the catalog offered real choices. | **No** — not a proven pain point. |
| Content có giúp retention? | Cold review of 4 cards from 2 lessons: every prompt was understandable without rereading, every answer was standalone and self-ratable, ratings (Good/Hard/Again) mapped to correct scheduler intervals (Again/Hard → 1 day, Good → 2 days). | **Yes** — candidates work as real recall material. |

## Dogfood evidence

### Day 5 — choose, learn, retain (fresh user)

- Catalog: 8 lessons listed; filters by topic (EF Core = 3) and difficulty (Beginner = 2) worked;
  empty filter state was clean.
- Learn loop: Start → In progress → stop → return → Continue → Complete all worked; resume picked up
  the in-progress lesson from the Continue Learning section.
- Save to Knowledge: prefilled Key Takeaway draft needed only a light edit; note saved.
- Add to Review: 2 of 3 candidates selected after preview; all answers standalone and self-ratable.
- History showed exactly 1 entry; Analytics showed Lessons completed = 1 with honest
  "No strong learning insights" copy (no mastery claims).

### Day 6 — delayed review + refinement

- Completed a second lesson and added 2 more review cards (4 total due).
- Cold review of all 4 cards: Recall → Reveal → rate. Scheduler semantics correct
  (Again/Hard → next in 1 day, Good → next in 2 days). Session resume after leaving mid-session worked;
  completion summary showed per-rating counts and Still due 0.
- After rating, the source lesson stayed **Completed** — no second completion evidence, no duplicate
  Learning History entry (exactly 2 entries, one per lesson, original timestamps).
- Analytics: Lessons completed = 2, Practice activities = 4, Review = 4.
- "Already in Review" dedup badge prevented duplicate card creation.
- Responsive 390px: Learn catalog, lesson completion, Knowledge dialog (340px), and Review selection
  dialog (340px) all fit; wide code blocks scroll inside `<pre>`; no horizontal overflow.
- Dark mode: good contrast across body, badges, and headings.
- Focus visibility: global `:focus-visible` outline rule present and rendering.

## Friction log

### P0 — Blocking

None found across Days 5–6.

### P1 — High friction

None found. No repeated P1s, so Option C (polish sprint) is not triggered either.

### P2 — Minor friction / needs manual confirmation

| Issue | Status | Note |
| --- | --- | --- |
| Review keyboard shortcuts (Space, 1–4, Enter) could not be exercised in headless automation | Open (verify manually) | Shortcuts did not fire via synthetic key events; likely an automation artifact, but confirm in a real browser session. |
| Two dialogs can coexist briefly in the DOM if a second dialog opens before the first exit transition finishes | Accepted | Cosmetic; both dialogs function correctly. Observed only under automation timing. |

### P3 — Nice to have

| Issue | Status | Note |
| --- | --- | --- |
| Vite dev-server "Outdated Optimize Dep" 504 on first Analytics load | Not an app bug | Dev-server cache behavior; reload fixes. |

## Content refinement

No refinement was needed. All six review candidates authored in Days 1–4 proved standalone and
self-ratable during cold review, so Day 6's bounded refinement slot was intentionally left unused.

## Week 6 Definition of Done — status

- ✅ Catalog has 8 .NET Backend Starter lessons (target 6–8), all meeting the quality rubric.
- ✅ All content seeded idempotently through the development-only seeder path.
- ✅ Every lesson has objective, focused explanation, example, key takeaway, official further reading,
  and 2–3 quality Review candidates.
- ✅ User can browse, start, resume, complete, skip post-actions, save Knowledge, add selected Review
  candidates.
- ✅ Cold Review from new lessons uses the normal scheduler; `Again` does not reset completion.
- ✅ P0 unresolved = 0; no P1 found.
- ✅ Evidence-based decision recorded: **Option B — Continue curriculum + dogfood**.

## Week 7 direction (Option B)

1. Extend the curriculum toward a coherent .NET Backend Starter path (roughly 12–15 lessons), keeping
   the same quality rubric and development-only seeding.
2. Continue dogfooding the Learn → Complete → Remember loop with the larger catalog.
3. Manually confirm the review keyboard shortcuts in a real browser (open P2 item).
4. Re-evaluate Discover only if "I don't know what to study next" becomes a repeated, evidenced pain
   with the larger catalog.
