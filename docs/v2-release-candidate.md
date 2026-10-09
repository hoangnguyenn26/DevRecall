# V2 Core Learning OS — release candidate

Checkpoint: 2026-10-09. Tag: `v2-core-learning-os-rc1`.

## Decision

**Outcome A: close V2 feature development and enter sustained local dogfood/release mode.**
No unresolved P0 or meaningful P1 was identified in the scoped checkpoint after fixing
the Learning Profile route. This is an RC, not production certification, proof of human
retention, or a claim that every possible workflow has been tested.

Week 12 does not automatically introduce features. If real use exposes a P0/P1, fix that
specific issue. Bookmark, AI, Learning Paths, richer ranking and additional curriculum
remain deferred until repeated user need justifies them.

## Current contract

- Today uses Due > Ongoing > Planned > weakness-driven > recommended priority. Explicit
  intent wins over suggestions; the current presentation is primary-only, not a metrics grid.
- Internal lessons teach concepts. Start/Complete are explicit; completion is one canonical
  historical fact, not mastery. Knowledge and Review are separate user-selected actions.
- Review snapshots retain prompt/answer; source metadata is optional provenance. Normal
  ratings, scheduler, concurrency and performance evidence remain authoritative. Again
  does not reopen a completed lesson or generate another lesson completion.
- Discover ranks internal lessons and trusted external resources independently. Semantic
  relevance is required; no irrelevant padding, popularity claims or unified content score.
- External source Open is navigation only. Explicitly adding a Published resource to a
  Draft Study Plan records future intent. Finishing its Session task records orchestration,
  not ExternalResource/LearningContent completion, History or automatic weakness improvement.
- Analytics separates canonical lesson completions, Review outcomes and existing Study
  activity. Estimated reading/planned minutes never become actual StudyMinutes.
- New eligibility uses Published content; historical references retain title snapshots and
  unavailable sources. No silent cascade deletion of historical learning facts.

These current semantics supersede historical “future integration” paragraphs retained in
phase logs. See [Learn](learning-content.md), [resources](external-resources.md),
[plans](study-plans.md), [Discover](discover.md), [Today](today-next-best-action.md), and
[authoring](content-authoring.md).

## Release-critical evidence

| Gate | Checkpoint evidence |
| --- | --- |
| Daily priority and mixed execution | Existing PostgreSQL daily-loop and mixed Session API checks passed |
| Unique completion / retry | Concurrent completion, Knowledge submission replay and Review batch replay checks passed |
| Ownership / conflicts | External task and Profile owner isolation/stale-version checks passed; no-op Profile version remains stable |
| Archived history | Existing completion snapshot remains visible after source archive, covered by API smoke |
| Account boundaries | Browser logout/login from a populated account to a separate empty account showed empty Today/Discover/Plans/Sessions and no inherited lesson completion; auth/cache tests passed |
| Recovery UI | Existing Today/Discover refresh-error, Profile draft recovery and invalidation checks passed |
| Profile navigation | Browser exposed nested Settings route swallowing the Profile form; Settings moved to its index route, then production navigation rechecked |
| Query discipline | Source audit: owner filters, compact projections, SQL counts and bounded pagination; Today reads sequentially on its scoped DbContext |
| Seed identity | Source audit confirms stable IDs/slugs/CandidateKeys and guarded updates; earlier Day 1–2 explicit replay inserted zero rows. Seeder was not rerun at this checkpoint |
| Responsive / keyboard | Earlier Days 3–4 sampled desktop/tablet/mobile and Review keyboard/dialog flow; see hardening log for exact observations |

Current checkpoint: **9 existing PostgreSQL API tests and 44 scoped frontend tests passed**.
The Discover test mock was updated for the existing refreshError and Today invalidation
contract; its existing recovery check now covers stale recommendation suppression.
Nuxt type-check, touched-file lint and production web build passed for the route fix.
No full regression suite or new migration is claimed or required by this change.

## Product assessment

Today chooses a coherent next action in the checked priority ladder. The authored internal
curriculum teaches concrete backend concepts; Discover reduces a small catalog to relevant,
explained choices. Plans preserve explicit future intent and mixed Sessions execute that
intent without conflating external reading with lesson completion. Knowledge captures a
personal note; Review supports recall with the normal scheduler. History and Analytics
remain truthful in the scoped evidence checks.

These findings justify an RC, not a measured answer to “will I remember this next week?”
Delayed cold recall and sustained human use remain the next validation step. Agent-operated
smoke is not a substitute for that feedback.

## Known limits and remaining backlog

- P2: public landing Today-priority copy still reflects an older policy; current app behavior
  and this checkpoint are authoritative. Cosmetic public copy cleanup can be separate.
- P2: directly API-created Session items may display zero planned minutes. Actual recorded
  duration is separate; plan-generated duration behavior was not changed.
- Existing Nuxt/Nitro unused H3Error/H3Event import warning remains; no warning-free claim.
- No quantitative query/performance baseline, exhaustive assistive-technology audit,
  frame-by-frame account-switch race verification, or every archived historical UI permutation.
- Days 5–6 have no separate implementation entry in the current hardening log. This Day 7
  source/targeted audit covers selected data paths, not a retroactive claim that all their
  possible gates passed.
- No AI, resource completion, bookmark domain, implicit topic-taxonomy bridge, crawler or
  automatic Week 12 feature scope.

See [hardening log](v2-hardening-log.md) for findings, fixes and the distinction between
observed browser behavior, automated checks and unmeasured product outcomes.
