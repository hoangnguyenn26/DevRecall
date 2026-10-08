# V2 core learning loop — Week 10 checkpoint

## Decision

The V2 core learning loop is functionally complete enough to close major feature
development for now and enter sustained dogfood, reliability hardening and release
readiness. This is a functional milestone, **not release approval** or evidence that
users already trust Today in daily use. Future features require repeated user friction,
not unused slots in the previous roadmap.

## Today policy

Due → Ongoing → Planned → evidence-based practice suggestion → recommended Lesson.
Active Session precedes its contained work; InProgress Lesson precedes its planned or
recommended duplicate. Ready and actionable Draft plans precede new suggestions.
Recommendations remain fallback work, not obligations.

The final presentation budget is **one primary and at most one useful secondary**.
Currently zero secondaries are rendered: no alternative has demonstrated enough value.
Do not fill an Up next slot for symmetry. Reasons and action-specific CTAs explain the
selected work; empty Today offers Explore Discover. Setup is optional, not competing work.

External resources reach Today only through explicit Plan/Session intent, never directly
from Trusted Resources ranking. Today remains an authenticated, owner-scoped read-only
composer: no Start, Plan mutation, impression, evidence, persisted NBA or background job.

## Daily-loop result

The existing isolated PostgreSQL/API scenario exercises actual domain mutations:

- Due Review → rating → Continue Lesson.
- Continue Lesson → canonical Complete → actionable Draft Plan.
- Draft → Ready → explicit conversion/start → Continue Session.
- Mixed Session → explicit item completion → Session completed → recommended Lesson.
- Open recommendation → no state change; Start → Continue; Complete removes that candidate.
- Adding a recommended Lesson to a Plan records planned intent, not learning evidence.

Five canonical Lesson completions and two Review outcomes remain separate. External task
completion creates no Lesson completion or Learning History entry. A stale Session
attachment cannot undo a successful Lesson completion. These are scoped technical results,
not automation of every vertical or proof of long-term retention.

Today invalidation is targeted to meaningful changes: Review, Lesson Start/Complete,
Plan/Session mutations, Profile and relevant weakness/recommendation changes. Review and
Session flows invalidate rather than fetching Today per item. Next Today entry fetches
normally; unrelated Knowledge edits and external Open do not trigger NBA refresh.
Mutation success remains success if a later read fails.

## Day 7 verification

Reused existing tests; no new test matrix or business/UI feature:

- 13 Today priority policy tests.
- 19 Today presentation, targeted invalidation and best-effort refresh frontend tests.
- 8 Today API/mixed-session daily-loop tests on PostgreSQL.
- Source audit of authenticated ownership, ordered policy, no-tracking/bounded candidate
  projections, lazy fallback lookup and the primary-only UI.

No full suite, production rebuild, browser/mobile check, latency benchmark or sustained
human dogfood is claimed. Prior Days 5–6 type-check and changed-file lint passed; this
checkpoint changes documentation only. See [policy and transition audit](today-next-best-action.md)
for detailed results and [Today contract](today-dashboard.md) for composition boundaries.

## Known limits and release risks

No P0 or new P1 was observed in the scoped technical checks; this is not a blanket
absence-of-bugs claim. The eager Today refresh coupling identified earlier was fixed.
Live back-navigation freshness, account switching, mobile ergonomics, accessibility,
network/conflict recovery and repeated real-session usefulness still require hardening.
Today retains legacy bounded preview reads for API compatibility; actual latency/query
cost is unmeasured. Discover's global-content-to-user-Weak-Topic mapping remains unavailable;
do not claim automatic taxonomy attribution or weakness resolution after completion.

Content state ≠ planning state ≠ evidence state. External resource Open is read-only;
finishing a planned resource task is not global resource completion. Estimates are not
study duration. Review performance does not reset Lesson completion or manufacture mastery.

## Next direction: Week 11

1. Days 1–2: manual-first vertical dogfood of Today/Review/Learn, Lesson → Knowledge/Review,
   resource → Plan → mixed Session, History reopening and Profile → Discover/Today.
2. Days 3–4: error/empty/conflict/archive/network recovery, responsive and accessibility;
   fix P0/P1 only.
3. Days 5–6: targeted query performance, evidence correctness, cache/account isolation and
   seed stability audit. Add checks only where a real risk or defect requires them.
4. Day 7: decide release/long-term dogfood readiness, one focused polish week, or a concrete
   remaining bottleneck. Do not automatically turn Week 12 into another feature week.

Release bar: no known P0, no obvious core-loop P1, truthful evidence/analytics, ownership
isolation, no common navigation dead end, mobile blocker or fake CTA. A milestone tag
records this checkpoint, not satisfaction of the final release gate.

Deferred: Bookmark/SavedResource, Learning Paths/prerequisites, recommendation feedback,
AI/vector ranking, provider ingestion/crawler, resource completion, NBA history/impressions,
new content quotas, generic scoring engines and background orchestration.
