# Discover foundation

`GET /api/v1/discover` requires authentication and is a read-only projection. It never starts content, records impressions, persists recommendations or changes learning evidence.

Eligibility: Published Lessons with no progress and no canonical completion evidence for the current user. In-progress and completed lessons belong in Learn / Continue Learning / History, not Discover. Difficulty, technology and available time are not eligibility filters.

Content goals use the canonical `LearningProfileGoal` taxonomy, not the legacy Identity preference goals. A lesson has at most three unique goals; existing content can have none. A composite key prevents duplicate tags. All eight seed lessons target backend fundamentals; five target interview preparation, while middleware, cancellation and transactions target practical projects. The explicit development seed upgrades the original broad goal pair idempotently, without replacing custom goal sets. No lesson body or user progress is rewritten.

`recommended` returns the top four ranked lessons. Secondary sections return at most three each and are deduplicated on the backend in priority order: Recommended → Weak → Goals. `basedOnGoals` uses exact canonical goal intersections; secondary section order remains publication date descending then UUID. Missing profiles return `profileConfigured: false`; browsing Learn is always available.

`basedOnWeakTopics` is intentionally empty for now. Existing Weak Topic profiles are resource/user-taxonomy evidence, not the platform Content Topic taxonomy. There is no stable explicit mapping; Discover does not infer one from names, silently create personal topics, or fake weak-area relevance. The UI hides empty sections.

The compact response contains summary metadata and reasons, not lesson sections/body, progress scores, or persisted recommendation IDs. Declared profile goals are preferences, not observed ability. A completed lesson is not mastery.

## Experience and freshness

Discover offers **Open lesson**, which only navigates to the reader with a safe internal `returnTo=/app/discover`. Starting and completing remain explicit reader actions. No impressions or clicks are persisted. Learn remains the full catalog; Discover is not a due-review queue or Continue Learning replacement.

Successful Start, Complete and Learning Profile save clear the `discover` query root without forced background fetches. Logout clears private Nuxt data. Next navigation fetches current eligible lessons. Errors offer Retry and a catalog escape; missing profiles offer optional setup, never a redirect gate. Empty goal matches make no claim that all relevant lessons were completed.

## Deterministic ranking policy

Infrastructure reads compact candidate metadata and the current owner's declared `LearningProfileSignals`, using no tracking. It filters out unpublished/non-lesson content, progress and completion evidence before materialization. Exact goal/technology relevance filtering occurs in SQL; at most 100 candidates are considered, by publication date descending then UUID. This recency-bounded pool is an MVP limitation: older relevant content beyond the pool is not globally ranked. No sections, objectives, candidate answers or user notes are loaded.

Application owns scoring. A semantic match (mapped weak topic OR exact goal OR exact primary/secondary technology) is mandatory. Difficulty and time cannot recommend otherwise unrelated lessons. There is no inferred C#→ASP.NET Core relationship and no role-based magic.

| Signal | Internal points | Meaning |
| --- | --- | --- |
| Explicitly mapped weak topic | 40 max | Observed relevance (mapping currently unavailable) |
| Goal match | 30 max | Declared intent |
| Primary technology | 20 max | Declared focus |
| Secondary technology | 10 max | Explicit interest; not added on top of primary |
| Difficulty | 0–10 | Soft preference based on self-reported experience |
| Time | 5 / 2 / 0 | Within daily preference / at most 1.5× / longer |

Multi-tag content cannot multiply points for a dimension. Long/advanced lessons remain eligible when semantically relevant. Missing experience/time contributes zero, with no guessed defaults. No recency points, popularity, clicks, saved notes, Review-outcome double counting or Study Plan membership are used.

| Experience | Beginner lesson | Intermediate | Advanced |
| --- | --- | --- | --- |
| Beginner | 10 | 3 | 0 |
| Junior | 10 | 8 | 1 |
| Mid-level | 5 | 10 | 6 |
| Senior | 2 | 8 | 10 |

Ranking is score descending, publication date descending, UUID ascending. Public DTOs omit score and breakdown. At most two visible reasons follow priority Weak → Goal → Primary/Secondary Technology → exact Time fit. Difficulty remains an internal ranking adjustment, not a user-facing ability claim. Goal/technology labels reuse Learning Profile metadata. A near time fit never claims to fit the user's budget. Every recommendation has a genuine semantic reason; experience is not presented as measured skill.

Observed mapped topics are separate from declared signals in the policy input. Tests cover their weighting and missing-profile behavior, but production deliberately supplies no weak-topic matches until a trustworthy taxonomy bridge exists. No profile, history or weakness data is sent to an external provider. Learn catalog ordering remains unpersonalized.

Tests simulate Backend Junior, interview-goal, EF-focused and missing-profile personas; validate exact relevance, stable ties, cross-section deduplication, Start/Complete removal and profile-change refresh. These are synthetic quality checks, not a claim of human cold-review dogfooding or calibrated recommendation accuracy.

Deferred: weak-topic taxonomy bridge, feedback/impressions, AI/ML/embeddings, diversity/exploration algorithms, recommendation history and integration with persisted Study Recommendations.

## Week 6 Days 5–6 quality checkpoint

See [recommendation quality audit](v2-week6-recommendation-quality.md) for seeded persona results and remaining human dogfood questions. Weights and tie-breaks are unchanged. Recommendations can contain fewer than four cards and are never padded. `IsConfigured` is setup UX state, not a policy gate: valid partial signals still rank content, with optional profile setup below the results. The profile editor still enforces its existing save invariants; no partial-save API was introduced.

A configured empty result says **No matching lessons right now** and offers catalog browsing, without claiming all lessons are completed or requiring profile changes. Missing profiles offer optional setup plus Browse; errors offer Retry plus Browse. Browse remains available in every state. Cards prioritize title and summary before metadata, a compact explanation and Open lesson.

## Week 6 Day 7 — stable contract and checkpoint

The stages are intentionally separate:

1. **Eligibility:** Published Lesson, no owner progress and no canonical completion evidence. Open does not change eligibility.
2. **Relevance:** at least one exact mapped weak-topic, goal or technology match. This is an invariant, not an optional score threshold. SQL applies the available goal/technology relevance predicate early to bound the compact candidate read.
3. **Fit:** self-reported experience and available time only adjust already-relevant candidates. Missing values add nothing.
4. **Ranking:** capped code-based dimensions, then stable score/publication/UUID ordering, top four without padding.
5. **Reasons:** truthful semantic reasons first, optional exact time fit, maximum two; no public score, confidence or AI explanation.
6. **Display deduplication:** Recommended → Weak → Goals; hide empty secondary sections.

State ownership remains **NotStarted → Discover/Browse; InProgress → Continue; Completed → History/Review**. Browse remains universal. Discover is a read projection, not the persisted Study Recommendations module: no acceptance mutation, status, tracking or background generation. Reading Discover or opening a lesson does not create learning activity. Explicit lesson completion records the historical learning action, not mastery or automatic weakness reduction.

See [Day 7 validation](v2-week6-validation.md). Technical lifecycle and account isolation were checked in the local production browser and existing scoped tests. No demonstrated diversity problem or repeated rejection pattern justifies a new algorithm or feedback system. The next step is **dogfood Discover longer**, then decide with the user's feedback whether catalog quality/volume or a narrowly defined exclusion preference is the actual bottleneck. This is a checkpoint recommendation, not an automatic rewrite or implementation of Week 7.

Known limits: heuristic, not learned; eight seeded lessons; editorial time estimates; no feedback collection, fuzzy/semantic similarity or relearn recommendation; broad primary technologies can produce broad matches; canonical weak-topic attribution is not connected in production. Partial and weak-only signals are supported by the policy, not a new partial-save profile API or an inferred taxonomy bridge.
