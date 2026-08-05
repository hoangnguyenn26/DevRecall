# Today Dashboard

## Purpose

Today is DevRecall's action-first entry point. It answers what the learner should do next, shows a small amount of supporting context, and makes it easy to continue or capture learning. It is not an admin dashboard and does not manage the complete lifecycle of every module.

## Composition endpoint

`GET /api/v1/today` is an authenticated, owner-scoped, read-only composition query. A single UTC instant is shared across due-review boundaries, recommendation expiry, the current week, recent-activity cutoff, and `generatedAtUtc`.

The response is deliberately bounded:

- one active Study Session candidate;
- one Ready or Draft Study Plan with at most five ordered items;
- at most three active, unexpired recommendations;
- at most three current Weak Topic profiles;
- exactly seven daily activity points;
- one resumable activity from the previous 14 days.

Readers use no-tracking projections and never execute lifecycle mutations or `SaveChanges`.

## Next Best Action

The application policy selects exactly one action in this order:

1. Continue an active Study Session.
2. Start a Ready Study Plan.
3. Start due reviews.
4. Continue a Draft Study Plan.
5. Open a Critical or High recommendation.
6. Generate a Study Plan from active recommendations.
7. Capture Knowledge as the empty-workspace fallback.

Every target path is constructed in application code. Database text and user-provided URLs are never used as navigation targets.

## Supporting sections

Weekly metrics reuse Review, Analytics, and Study definitions. Study Plan, recommendation, and Weak Topic previews provide navigation only; Today does not convert plans, dismiss recommendations, or recalculate weakness profiles. Continue Learning is visually secondary to Next Best Action and omits itself when no valid resource is available.

## Onboarding

Users without a learning-preference row receive backend defaults and an onboarding callout. Complete and Skip persist an explicit preference. Repeating the same normalized completion payload is a no-op that preserves its version and timestamps.

## Quick Capture

The application shell owns one Quick Capture panel shared by the top bar and command palette. It reuses the existing Knowledge, Interview, and DSA create APIs. It does not create Review Items, Attempts, Recommendations, Study Plans, or Study Sessions. Success keeps the current route and offers an explicit link to the created resource.

## Navigation indicators

`GET /api/v1/navigation-indicators` returns due reviews, whether a Draft or Ready Study Plan exists, the number of Critical Weak Topics, and onboarding completion. Failure is non-blocking: navigation remains usable and badges are omitted.

## Query invalidation

Frontend invalidation is targeted:

- onboarding, review completion, Study Plan generation/lifecycle, and Weak Topic recalculation refresh Today and navigation indicators;
- recommendation lifecycle refreshes Today, while recommendation generation also refreshes navigation;
- Quick Capture refreshes its module list and Today;
- background Today refresh retains the previous response if the new request fails.

The frontend does not refresh every query after a mutation and does not poll indicators continuously.

## Runtime and security notes

Server-rendered authenticated routes call the API through `apiInternalBaseUrl`; browser requests continue through the same-origin `/api/v1` proxy. The in-memory antiforgery token is cleared whenever login or logout changes the cookie identity, so a token minted for the previous claims principal is never reused.

The Week 18 dependency review found one moderate PostCSS advisory in the current Nuxt dependency tree. DevRecall does not process user-controlled CSS or source maps at runtime, so the practical exposure is limited to the trusted build pipeline. The package will be upgraded through the regular Nuxt dependency update rather than applying an unreviewed lockfile-wide audit fix.
