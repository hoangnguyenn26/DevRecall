# Today / Next Best Action — V2 policy

## Audit and pipeline

`TodayEndpoints.MapTodayEndpoints` authenticates `GET /api/v1/today` and maps the
application result to `GetTodayDashboardResponse`. `GetTodayDashboardHandler` derives
the owner from authentication and reads the clock once. `TodayDashboardReader` composes
bounded, no-tracking projections; `TodayRecentActivityReader` reads one resumable recent
activity. `TodayNextActionPolicy.SelectAction` selects one primary action through explicit
ordered branches, not enum integers or a global numeric score. `NextActionCard.vue`
renders backend title/description/label/path; `Dashboard.vue` keeps supporting previews.

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
| Empty | BrowseLearning | No actionable candidate; no fabricated recommendation | `/app/learn` |

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
- The recent-activity CTA is omitted when it targets the same resource as primary. Other
  existing contextual previews remain; no secondary-candidate list or mini planner is added.
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
100-candidate compact bound and scoring are reused; the lesson-only path skips the external
resource query. There are no internal HTTP calls, source-site requests or full catalog loads.

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
