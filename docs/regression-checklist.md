# DevRecall release regression checklist

This checklist is the reusable Week 24 release gate. Automated tests remain the source of truth for business invariants; browser and operational checks cover behavior that is more valuable to verify manually.

## Identity

- [ ] Register creates an authenticated account with incomplete onboarding.
- [ ] Onboarding persists daily target, weekly target, focus areas, and completion.
- [ ] Login routes completed users to Today and incomplete users to onboarding.
- [ ] Logout invalidates the server cookie and clears all private client state.
- [ ] Anonymous private and focus routes redirect to login without a redirect loop.
- [ ] Valid `/app/**` `returnTo` paths retain query strings; external and encoded URLs are rejected.

## Knowledge

- [ ] Quick Capture creates an owner-scoped Knowledge item visible in the workspace.
- [ ] Topic hierarchy, descendant filtering, Uncategorized, tags, and AND filtering retain their documented semantics.
- [ ] Workspace list, detail, edit, URL selection, and global search stay synchronized.
- [ ] A stale version returns `409 KNOWLEDGE_CONFLICT` without discarding the local edit.
- [ ] Cross-user reads and mutations return 404.
- [ ] User-authored title/content/tag values render as text and never execute markup.

## Practice

- [ ] Interview practice keeps the reference hidden until compare, records a self-rating, and creates immutable newest-first history.
- [ ] Practice Again creates a new Interview attempt; historical attempts cannot be edited.
- [ ] DSA practice records an attempt outcome, code, complexity, reflection, and immutable history without judge/pass-fail claims.
- [ ] Interview and DSA submission retries return the same logical attempt.
- [ ] Numeric enum strings are rejected for difficulty, rating, outcome, and resource type inputs.
- [ ] Cross-question/problem attempt IDs and cross-user attempt reads return 404.

## Review

- [ ] Due queue ordering, reveal, one rating mutation, progress, and completion are correct.
- [ ] Review submission retry creates one immutable history record.
- [ ] A stale `expectedReviewCount` returns the stable schedule conflict and does not double-schedule.
- [ ] Review labels remain Again, Hard, Good, and Easy with “recall outcome” terminology.

## Study

- [ ] Recommendation generation produces one editable Draft plan with source explanations.
- [ ] Draft edit, remove, reorder, duration, no-op, and version increments are correct.
- [ ] A stale plan mutation cannot overwrite or downgrade a Ready plan.
- [ ] Ready conversion retry returns the same Study Session and does not duplicate it.
- [ ] Session items preserve position, type, resource ID, title snapshot, and planned minutes.
- [ ] Opening a resource never completes an item; completion requires explicit action or matching practice evidence.
- [ ] Interview/DSA evidence must belong to the same user and resource as the Session item.
- [ ] If practice succeeds and Session progress fails, retry only Session completion.
- [ ] Completing or skipping the final pending item completes the Session exactly once.
- [ ] Resume, summary, evidence links, missing-resource fallback, reflection versioning, and counts are correct.

## Insights

- [ ] Weak Topics are owner-scoped, ordered by severity, evidence-bounded, and explained without raw-score claims.
- [ ] Dismissing a Recommendation does not remove the underlying Weak Topic.
- [ ] Recommendation actions reuse trusted resource mappings and never accept arbitrary URLs.
- [ ] Analytics ranges 7d/30d/90d use correct current/previous boundaries, zero-fill, and owner isolation.
- [ ] Active Days counts calendar days, not actions.
- [ ] Nested Interview/DSA activity inside a Study Session is not double-counted as study time.
- [ ] Interview uses “self-rating”; DSA uses “attempt outcome”; Review uses “recall outcome”.
- [ ] Learning Insights respect minimum samples and reuse the matching Recommendation action.
- [ ] Today refreshes after Session completion, Recommendation dismissal, and Plan generation.

## Public

- [ ] `/` and `/features` work anonymously, use the public shell, and make no private API calls.
- [ ] Register → onboarding → Today and sign-in → correct private destination work without shell flashes.
- [ ] Public HTML contains no user identity or private learning data.
- [ ] Landing and Features are indexable; auth and `/app/**` are noindex.
- [ ] Canonical, robots, sitemap, Open Graph, JSON-LD, and HTTP 404 behavior are valid in production preview.
- [ ] Public chunks do not eagerly include private workspaces, ECharts, or authenticated stores.

## Security

- [ ] Authentication cookie is Secure and HttpOnly in production, SameSite=Lax, path `/`, and expires server-side on logout.
- [ ] Mutations require the antiforgery request token; production CORS remains same-origin.
- [ ] Login/register rate limiting returns a safe 429 response when the configured boundary is exceeded.
- [ ] All user-owned resources and nested IDs are owner-scoped and cross-user access returns 404.
- [ ] Representative 400, 401, 404, 409, 422, 429, and 500 responses are stable Problem Details without implementation leakage.
- [ ] Public enum values are names rather than ordinals; text, pagination, search, batch, and range inputs are bounded.
- [ ] No auth tokens are stored in localStorage, sessionStorage, or IndexedDB.
- [ ] No unsafe `v-html`, interpolated raw SQL, request-body logging, production OpenAPI, or debug routes exist.
- [ ] Personalized API/auth/app responses are `no-store`; public pages remain independently cacheable.

## Responsive

- [ ] Public, auth, application, and focus layouts pass at 320, 390, 768, 1024, and 1440 px.
- [ ] Dark mode, 200% zoom, reduced motion, visible focus, labels, skip links, and keyboard-only flows pass.
- [ ] Mobile navigation and virtual keyboard do not hide primary form actions.
- [ ] Browser console has no unexpected Vue, hydration, promise, keyboard-handler, or stale-request errors.
- [ ] Offline/timeout checks do not blindly repeat non-idempotent mutations.

## Release

- [ ] `dotnet clean`, restore, format verification, build, and full test suite pass once without rerun-until-green.
- [ ] Frontend typecheck, lint, unit tests, Playwright smoke tests, build, and production preview pass.
- [ ] PostgreSQL migration-from-zero, health checks, and demo-data command pass.
- [ ] Git contains no secrets, debug output, local reports, temporary assets, or unrelated changes.
- [ ] README, API catalog, architecture, security, performance baseline, demo guide, and release notes are current.
- [ ] Known limitations are documented and the release tag is created only after all gates pass.
