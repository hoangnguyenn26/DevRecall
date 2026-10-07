# Week 8 — Curated External Resource Discovery checkpoint

## Decision

Close discovery for the current curated catalog. Five specific Microsoft Learn references add framework depth beyond internal lesson mental models. Manual curation is sufficient at this scale; summaries, truthful primary-value metadata, source/kind and safe destinations are the acceptance bar. This editorial/product decision follows the user's Day 7 plan; it is not measured learner satisfaction or retention.

Next phase: **minimal External Resource → Study Plan integration**. No implementation today. StudySessionItem Completed ≠ ExternalResource Completed; LessonsCompleted and LearningContent completion evidence must remain unchanged. Existing session activity and actual duration may apply only through existing Study semantics, without a second resource signal. Estimated resource time is only a planning hint.

## Current semantics

- Lessons: explicit Start → InProgress → Complete → canonical evidence; user chooses Knowledge/Review actions.
- Resources: Open detail → source; null progress, no Start/Complete, objectives, candidates or learning evidence. No Continue/History or implicit mastery.
- Discover: independently bounded lesson/resource sections, semantic match required, no padding or unified score; resources may remain after matching lessons are completed.
- External content is global; recommendation context stays user-specific. No context/ownership code changed on Day 7.
- Current Study resource resolver and conversion availability explicitly require Lesson. External Plan integration remains blocked until deliberately designed.

## Lightweight verification

Day 7 used the computer-use skill for a short desktop browser inspection, not a full UI/regression matrix. The existing dogfood account/profile was unchanged. Discover displayed distinct lesson/resource headings, source/kind, approximate effort, curator summary and semantic reasons. Open resource navigated to the internal detail with Back to Discover. Detail exposed **Why this resource** and **Read on Microsoft Learn**, with no Start/Complete controls. Returning to Discover worked and resource cards remained visible as intended.

Clicking the source left DevRecall on its detail page. The actual rendered link had HTTPS, `target="_blank"`, `rel="noopener noreferrer"`; the in-app browser did not expose a new destination tab in its inventory. Therefore new-tab destination rendering/version/access is **not fully verified**, and no security warning or external access restriction was bypassed. The temporary inspection tab was closed; the pre-existing user tab was left intact.

Before/after browser navigation preserved full-row fingerprints of LearningContent progress and completion evidence, and the Weak Topic profile count. Earlier Days 5–6 smoke additionally preserved weakness row fingerprints and identical Analytics Overview. This supports read-only semantics, not proof of learning. No business/schema changes, new tests, full backend/frontend suite or rebuild were needed on Day 7. Prior scoped API/component/PostgreSQL checks remain the regression evidence, including archived exclusion, exact relevance, owner-specific lesson exclusion and external mutation/Study boundary rejection.

## Limitations and deferred scope

Human timed reading, longer-term usefulness and actual desire after reading are still unmeasured. Broad .NET/backend signals can yield broad but truthful matches; weak-topic taxonomy mapping remains unavailable rather than guessed. The candidate pool is recency-bounded at 100 per type, with no recency score. No saved-resource system, external completion tracking, provider ingestion/interfaces, crawler, scraper, copied articles, AI enrichment or new filters.

Logical checkpoint: `v2-week-08-curated-external-resources`.
