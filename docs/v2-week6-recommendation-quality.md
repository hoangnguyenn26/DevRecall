# Week 6 — Recommendation quality checkpoint (Days 5–6)

## What was inspected

The existing PostgreSQL Discover regression test now prints persona top-four results from the actual eight-lesson development seed through `LearningRecommendationPolicy.Build`. No production account preferences, progress or evidence were changed. These are deterministic data-level inspections, not browser QA or a claim that a human found the recommendations useful.

| Persona | Top results, in order |
| --- | --- |
| Backend Junior: C#, .NET, ASP.NET Core; backend fundamentals; 30 minutes | async/await Fundamentals; Dependency Injection Fundamentals; Optimistic Concurrency in EF Core; EF Core Transactions |
| EF broad focus: C#, .NET, EF Core, PostgreSQL; backend fundamentals; Junior; 30 minutes | async/await Fundamentals; Optimistic Concurrency in EF Core; EF Core Transactions; ASP.NET Core Cancellation Tokens |
| EF specific focus: EF Core; backend fundamentals; Mid-level; 30 minutes | Optimistic Concurrency in EF Core; EF Core Transactions; EF Core Tracking vs No Tracking; ASP.NET Core Cancellation Tokens |
| Interview: C#, .NET; interview preparation; Junior; 30 minutes | async/await Fundamentals; Optimistic Concurrency in EF Core; EF Core Tracking vs No Tracking; ASP.NET Core Service Lifetimes |
| Sparse: .NET only; no goals, experience or time | Optimistic Concurrency in EF Core; EF Core Transactions; ASP.NET Core Middleware Pipeline; async/await Fundamentals |
| No useful signals | No recommendations |

Backend Junior was inspected **without** a mapped weak signal, matching production today. A user-owned weak Dependency Injection topic is not automatically equivalent to the global Content Topic. Existing policy tests exercise explicit canonical mappings separately; there is no live weak-topic recommendation claim.

## Metadata audit and findings

- Interview tags are selective: five of eight lessons, not the entire catalog. Middleware, cancellation and transactions target practical projects instead. These distinctions were already implemented in Days 3–4.
- EF lessons teach EF Core/.NET, not PostgreSQL-specific behavior; no PostgreSQL or ASP.NET tag was added just to improve ranking. Global topics describe the actual lesson concepts.
- DI fundamentals and service lifetimes share a topic but answer different learning needs: dependency composition versus lifetime/state sharing. They are not interchangeable duplicates.
- Two introductory lessons are Beginner; lifecycle, tracking, middleware, cancellation, transaction and conflict-resolution lessons are Intermediate. There is no claim that this is a calibrated skill assessment.
- Estimates are editorial learning/practice allowances, not measured reading duration. Current values are 12, 14 and 15 minutes, concentrated in a small introductory catalog. Human timing remains needed before replacing them with broader 10/15/20/30-minute buckets. No timestamps, learning snapshots or user-edited metadata were rewritten to manufacture a better rank.

The broad EF persona does **not** put all three EF lessons first. This is an explicit current limitation, not a passing quality claim: .NET and C# are also primary signals, every matching technology dimension is capped at the same 20 points, and backend goals are shared. Beginner fit and stable publication tie-breaks can put async or cancellation above tracking. The focused EF persona puts all three EF lessons first. Editing declared focus can clarify intent, but is not required to browse or a substitute for future human quality feedback. Do not delete true .NET tags, multiply tag points or change weights based on this one finding.

Stable ties remain publication date descending, UUID ascending, with no recency points or random rotation. Technology-only sparse recommendations require exact .NET metadata. Difficulty/time alone cannot fill the list. Start/Complete exclusion, profile-save invalidation and owner isolation reuse existing scoped regression coverage.

## Decisions and UX changes

**No evidence of a diversity problem in the inspected seed objectives.** Human dogfood is still pending. No diversity rule was added: Topics are an unordered set, with no semantic PrimaryTopic or ContentSeries. A same-topic cap would suppress potentially useful complementary lessons.

Weights, eligibility, candidate bounds and ranking order are unchanged. Difficulty still adjusts rank but no longer occupies a visible reason slot. Semantic reasons remain strongest; exact time fit may fill a remaining slot. Maximum two reasons, no confidence percentage or measured-mastery wording.

Recommendations appear before optional setup for partial/observed signals. A configured empty result says “No matching lessons right now” and invites Browse rather than suggesting the profile is wrong. Missing-profile setup, partial counts, Retry and Browse remain explicit and do not require new backend diagnosis queries. Card hierarchy is title → summary → metadata → compact reasons → Open.

No feedback buttons, tracking, persistence, caching layer, scheduled jobs, inferred taxonomy mapping or new recommendation algorithm.

## Real account and Week 7 handoff

A read-only authenticated API smoke against the seeded demo account returned `profileConfigured: false`, zero recommendations and empty secondary sections. This is the expected no-signal fallback. Its preferences were intentionally not changed for a synthetic demonstration. Actual browser dogfood was deferred per the implementation-day instruction.

Human exit questions remain open:

- With your own technologies/goals, would you choose any of the top three lessons?
- Does the broad .NET focus produce repeated unwanted ASP.NET/EF cross-matches, or useful adjacent material?
- Does intentionally changing focus resolve unwanted suggestions?
- Would “Not interested” solve a repeated problem, rather than compensate for the small catalog?
- Are the time estimates useful after actually completing these lessons?

Day 7 should validate Discover → Open → Start → Continue and Complete exclusion in the browser if requested. No subjective usefulness, real cold-learning outcome or need for Week 7 feedback/adaptation is claimed yet.
