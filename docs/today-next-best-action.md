# Today / Next Best Action — V2 policy

Week 10 is closed as a functional milestone; see the
[product checkpoint and hardening direction](v2-week10-validation.md).
Final presentation budget: one primary and at most one useful secondary. The current
primary-only implementation intentionally leaves that secondary slot absent.

## Audit and pipeline

`TodayEndpoints.MapTodayEndpoints` authenticates `GET /api/v1/today` and maps the
application result to `GetTodayDashboardResponse`. `GetTodayDashboardHandler` derives
the owner from authentication and reads the clock once. `TodayDashboardReader` composes
bounded, no-tracking projections; `TodayRecentActivityReader` reads one resumable recent
activity. `TodayNextActionPolicy.SelectAction` selects one primary action through explicit
ordered branches, not enum integers or a global numeric score. `NextActionCard.vue`
renders backend title/description/label/path; `Dashboard.vue` now presents only primary
learning work with optional setup below it.

Code pointers (relative to repository root):

- `src/DevRecall.Api/Endpoints/Today/TodayEndpoints.cs`
- `src/DevRecall.Application/Today/TodayDashboard.cs`
- `src/DevRecall.Application/Today/TodayNextActionPolicy.cs`
- `src/DevRecall.Infrastructure/Today/TodayDashboardReader.cs`
- `src/DevRecall.Infrastructure/Today/TodayRecentActivityReader.cs`
- `src/DevRecall.Infrastructure/LearningContent/LearningContentReader.cs` — `GetInProgressAsync`
- `src/DevRecall.Infrastructure/Discover/DiscoverReader.cs` — `GetLessonInputsAsync`
- `src/DevRecall.Application/Discover/LearningRecommendationPolicy.cs`
- `frontend/devrecall-web/app/components/today/NextActionCard.vue`

The prior order was Session → Ready Plan → Review → Draft Plan → Critical/High
recommendation → Generate Plan → Capture. V1 has no distinct scheduled Interview/DSA
NBA candidate or direct Weak Topic action. Their mature practice flows participate
through Review, Session/Plan and persisted evidence-based Study Recommendations.
Recent Interview/DSA activity remains supporting navigation, not an urgency signal.

## Candidate inventory and official order

| Order / class | Wire type | Eligibility and factual reason | Destination |
| --- | --- | --- | --- |
| 1 / Due | StartReview | Owned Active reviews, DueAtUtc ≤ request instant; scheduled recall is ready | `/app/review` |
| 2 / Ongoing | ContinueStudySession | Owned InProgress Session; user already started it | existing Session detail |
| 3 / Ongoing | ContinueLearning | Published Lesson, owner InProgress, no canonical completion evidence; user started it | lesson reader with internal returnTo |
| 4 / Planned | StartStudyPlan | Owned Ready plan with an actionable source; user planned study | existing Plan detail |
| 4 / Planned | ContinueStudyPlan | Owned Draft plan with an actionable source; continue preparing the plan | existing Plan detail |
| 5 / Suggested practice | OpenRecommendation | Available Critical/High V1 Study Recommendation, active and unexpired | Recommendation detail |
| 5 / Suggested planning | GenerateStudyPlan | Existing active V1 Study Recommendations; preserve V1 planning fallback | Plan generation entry |
| 6 / Suggested learning | LearnRecommendedContent | Top eligible semantically relevant Discover Lesson, only without stronger work | lesson reader with internal returnTo |
| Empty | BrowseLearning | No actionable candidate; no fabricated recommendation | `/app/discover` |

Backbone: **Due → Ongoing → Planned → Observed practice suggestion → Recommended lesson**.
Due Review now intentionally wins over Session/Ready Plan; scheduler and Review-start
behavior are unchanged. Draft preparation is not Session execution. Empty/all-unavailable
plans do not produce a planned-work primary. Existing `CreateKnowledge` enum value is
retained for compatibility but no longer selected as an empty-user learning obligation.

## Ordering, dominance and scope

- Resume uses existing latest StartedAtUtc descending, then Session ID ordering.
- Continue lesson reuses StartedAtUtc descending, then progress ID, bounded to one.
- Plans retain Ready-before-Draft, latest Updated/Generated and ID tie-breaks; source
  eligibility is checked before selecting a plan, including owner checks for V1 resources.
- Published external resources are eligible planned sources; completed/archived Lessons
  are not actionable new content. Historical active Sessions remain resumable after source
  archive, preserving Week 9 rules. Today does not reconstruct or execute those tasks.
- Session dominates its contained lesson/resource. Only one primary is emitted, so no
  competing per-item candidate appears while a Session is active. Continue lesson dominates
  its planned/recommended duplicate; actionable Plan dominates Discover recommendations.
- The recent-activity DTO is omitted when it targets the same resource as primary. The UI
  does not render recent activity or other competing preview lists; no secondary-candidate
  list or mini planner is added.
- External resources never enter NBA from Trusted Resources. They enter through declared
  Plan/Session intent only. No role hardcoding, minutes-based suppression or percentage mastery.
- V1 recommendation score ordering is retained inside its own tier; Discover ranking is
  reused without rescoring or comparing its numeric values against Review urgency.
- Discover weak-topic mapping remains unavailable in production; no implicit taxonomy bridge.

## Read-only composition and freshness

Queries are sequential on the scoped DbContext. The existing dashboard reads one Session,
one eligible Plan (five preview items), three recommendations/weakness previews and seven
activity points. If due Review or Session exists, no lesson lookup is needed. Otherwise
Continue Learning reads top one; Discover is queried only at the fallback tier. Its existing
100-candidate compact bound and shared ranking are reused through `GetTopRecommendedLesson`,
without mapping secondary Discover sections; the lesson-only path skips the external resource
query. There are no internal HTTP calls, source-site requests or full catalog loads.

One UTC instant and existing UTC day/week boundaries are preserved. Today GET has no
SaveChanges, Start, conversion, evidence, impression or recommendation persistence.
CTA generation only constructs trusted internal routes; opening a Plan never auto-converts it.
The existing `nextAction` DTO remains; title/description explain why now, without a tier/score.
Legacy preview scores are unchanged by this additive integration.

Start/Complete lesson, add content to Plan and Profile save invalidate Today lazily, alongside
existing targeted Plan/Session/Review refresh behavior. No polling/global cache clear is added.
Mutation success is not reversed by a later failed read; Today uses its existing error/retry flow.

## Validation and deferrals

Scoped policy checks cover Due dominance, Session → Lesson → Plan → recommendation fallback,
empty state and trusted destinations. A PostgreSQL-backed Today API smoke covers resource
planning, owner isolation, ongoing lesson/Session dominance, Discover fallback and no added
completion evidence. Existing Today API/frontend tests are rerun, not unrelated full suites.

Day 3–4 owns visual priority dogfood and whether supporting actions need refinement. No browser
check, dashboard rewrite, secondary list, new persistence, AI, scheduling change or new resource
interaction is introduced in Days 1–2.

Verification: 13 Today policy tests, 7 Today API tests and 8 frontend Today tests passed.
Frontend type-check and ESLint on changed frontend files passed. Backend builds performed
by the scoped test commands produced no new warnings. No production Docker rebuild or
database mutation was performed; local deployed containers still require a later rebuild
to serve these source changes.

## Days 3–4 — calm action presentation and conflict review

Decision: **one primary, zero supporting learning actions** for now. The previous grid
made metrics, plans, weakness, recommendations and activity equally visible. Removing
it protects the decision-first surface; setup is secondary and modules remain in navigation.
One/two alternatives might help choice, but there is no repeated user feedback establishing
that value. Do not construct an Up next list or infer semantic dedupe in the browser.

The card shows a human-readable action label, work title, factual why-now explanation,
small existing context and an explicit navigation CTA. Session uses its name and remaining
items; plan uses its name and **Estimated plan time**; lesson time is an estimate, never
percentage progress. V1 practice uses its evidence-backed reason summary. Discover reasons
use actual goal/technology labels, without priority scores or confidence claims. Open study
plan navigates; it does not start/convert anything. Empty state offers Explore Discover.

Conflict log (synthetic/API/component checks, **not human/browser dogfood**):

| Conflict | Decision / observed smoke result |
| --- | --- |
| 5 due cards + active Session containing InProgress Lesson | Review primary; scheduler-first baseline retained |
| Session + its lesson | Session primary, one CTA, no individual duplicate |
| InProgress Lesson + Plan | Continue lesson wins |
| Planned external source + Discover Lesson | Plan wins even when its time estimate exceeds profile preference |
| No stronger intent + relevant lesson | Top Discover lesson; no Start mutation on navigation |
| No profile/no learning work | Explore Discover; setup is optional below primary |

No reliable current-interaction signal distinguishes resumable Session from work actively
being performed moments ago. Therefore Review > Session remains; no LastViewedAt heuristic,
priority retuning or scheduler urgency model was introduced. The daily-loop subjective
question (does Review interrupt natural continuity?) remains for real user dogfood.

Scoped validation: existing Today API smoke extended for due-vs-contained-Session conflict;
component checks cover action labels/context/trusted destinations and the absence of the old
grid. Shared Discover policy checks guard eligibility/order. Type-check and changed-file lint
are run; no full suites or browser check during implementation days. Live visual/mobile
perception and measured latency are not claimed. Production Docker is not rebuilt here.

Results: 10 Today component/unit tests, 26 scoped Today/Discover policy tests and the
extended PostgreSQL Today conflict smoke passed. The existing 7 Today API tests also
passed before that fixture extension. Frontend type-check and changed-file ESLint passed.

Deferred: supporting actions pending demonstrated value, dismiss, snooze, pin, manual
priority, recommendation history, NBA analytics, time budgeting, calendar integration,
multiple daily goals, AI scheduling and background/cache layers.

## Days 5–6 — daily-loop validation and readiness direction

**Direction A, technically qualified:** move toward deeper dogfood, hardening and release
readiness rather than new orchestration features. This is not a release approval or a
claim that users trust Today. Live browser/mobile perception, natural decision burden and
subjective usefulness remain Day 7/user checkpoint questions. No production data was
modified and deployed Docker was not rebuilt during this source/test-only checkpoint.

The existing mixed-session API scenario was extended, not a broad new matrix. Its isolated
PostgreSQL fixture starts with two due lesson-sourced cards, an InProgress lesson, a mixed
Draft plan and a relevant profile. It uses actual authenticated API mutations:

| Transition | Recomputed Today action |
| --- | --- |
| Due cards → normal Good ratings | ContinueLearning |
| InProgress lesson → canonical Complete | ContinueStudyPlan |
| Draft → Ready | StartStudyPlan (Open study plan, navigation only) |
| Ready → explicit Convert/start | ContinueStudySession |
| Partial mixed Session, including explicit external task finish | ContinueStudySession |
| All Session items completed with valid evidence | LearnRecommendedContent |
| Open recommended lesson + repeated Today GETs | Same recommendation; no Start |
| Add recommended lesson to Draft plan | ContinueStudyPlan |
| Explicit Start of that lesson | ContinueLearning |
| Canonical Complete | Previous candidate removed; another relevant fallback |

The loop keeps five lesson completion facts (two setup lessons, two mixed-session lessons,
one fallback lesson) and two normal Review outcomes; the external resource adds neither
lesson completion nor History. Existing stale Session attachment returns 409 while keeping
the successful lesson completion; submission replay remains safe. The resource's actual
Session time follows existing semantics, never reading estimates.

### Invalidation boundaries

| Successful mutation | Today behavior |
| --- | --- |
| Review rating / adding Review cards | Clear Today cache; refresh only Review/navigation entry points |
| Lesson Start / Complete | Clear Today cache through progress sync; completion also clears analytics/history |
| Plan item add/remove/edit, Ready, conversion | Clear Today cache; refresh local Plan view independently |
| Session start/item finish/session finish/cancel | Clear Today cache; local Session refresh remains separate |
| Learning Profile save | Clear Today and Discover caches |
| Weak Topic recalculation / recommendation lifecycle | Clear Today; navigation/module refresh independent |
| Unrelated capture/edit or external source Open | No Today refresh merely for visiting or creating unrelated content |

Clear invalidates the existing Nuxt query; the next Today mount uses its normal immediate
fetch. Manual Today refresh stays available. Review/Session flows do not fetch Today after
every item. Onboarding's existing explicit entry-point refresh is retained. No custom stale
state manager, polling, event bus or generic NBA evidence is added. Logout clears private
Nuxt data/state; previous owner-isolation API checks remain applicable. UTC due boundaries
are unchanged; no new timezone handling or artificial midnight test was introduced.

### Compact friction log

- Daily loop: technical smoke smooth; real daily use/visual dogfood not measured.
- Wrong primary transitions: none in the scoped API scenario; not a universal correctness claim.
- Stale candidate issues: eager Review/Plan refresh coupling found and removed; cache invalidation
  is tested, live back-navigation remains to be checked in browser at the requested checkpoint.
- Secondary action value: unmeasured. Final current policy is **Option A: primary-only**;
  no useful alternative is invented just to fill the screen.
- Reason/CTA mismatches: no new mismatch observed in source/API; existing domain-specific copy retained.
- Navigation friction: Session has Return to Today, lesson uses safe returnTo; Review exits to its
  queue and Today remains in normal navigation. End-to-end browser ergonomics still unvalidated.
- P0/P1/P2: no P0 observed in this smoke; P1 eager refresh coupling fixed; no extra visual work
  justified without evidence. Production rebuild and real dogfood are outstanding readiness work.

Validation: the extended API daily-loop smoke passed; 19 scoped frontend invalidation,
best-effort refresh and auth tests passed. A failed auxiliary refresh cannot prevent Today
being marked stale; best-effort refresh preserves saved mutation success. Frontend type-check
and changed-file lint passed. No full suite, browser check, performance baseline or final
release gate was claimed. Snooze/dismiss/pin/AI/action analytics remain deferred.
