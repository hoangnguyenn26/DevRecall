# V2 hardening log

## 2026-10-09 — Product-flow audit, Days 1–2 (partial)

Scope: real verticals and critical integration boundaries, no new capability. This entry
distinguishes source/API checks from authenticated browser dogfood. The latter is pending
an existing local demo login; no password reset or substitute account was created.

| Flow / boundary | Result and issue | Severity / layer | Resolution or remaining work |
| --- | --- | --- | --- |
| Account change / expired session | Login/register did not clear personalized Nuxt cache; 401 restore only cleared auth state | Security-sensitive cache-isolation risk; client auth boundary, no live cross-user leak observed | Clear Nuxt data/state after successful login/register and on 401 restore; failed login does not erase current identity/cache. Existing logout behavior retained |
| Today → Review → Lesson → Plan → mixed Session → fallback | Existing PostgreSQL API ladder passed real mutations | No defect observed in scoped API check | Browser back-navigation/freshness still pending |
| Complete retry / concurrent completion | One canonical completion evidence after concurrent requests | No defect observed in scoped API check | No new evidence implementation |
| Lesson → Knowledge | API rejects unavailable lesson/foreign organization; existing component smoke preserves edited draft and retry identity | No defect observed in scoped checks | Browser save/error experience still pending |
| Lesson → Review | Batch retry remains deduplicated; normal scheduler/evidence used by daily-loop smoke | No defect observed in scoped API check | Cold recall/readability and real Reveal/Rate UI still pending |
| Profile → Discover | Exact technology signals and refreshed recommendation result passed existing API check | No defect observed in scoped API check | Live Profile → Discover/Today comparison pending |
| History / archived Lesson | Existing snapshot-history API check passed | No defect observed in scoped API check | Browser reopen/Read Again pending |
| Resource → Plan → mixed Session | Resource task excluded from Lesson completion/History; actual Session duration retained | No defect observed in scoped API check | Real outbound tab, return and partial-session resume pending |
| Deep link `/app/learn/history` while signed out | Browser redirects to login with the original internal returnTo | PASS, browser observation | Authenticated reloads remain pending |
| Public Today copy | Landing still says Session/Ready Plan precede due Review, contradicting current priority | P2 stale product copy | Defer copy cleanup; do not confuse this with policy behavior |
| Weak Topic → executable action | Natural dogfood state not inspected | Pending, not classified as a defect | Check only if existing account has attributable evidence; do not manufacture taxonomy mapping |

### Changes and verification

- Small auth-store change only: invalidate personalized cache on confirmed identity boundaries.
  Browser-only clearing is guarded with `typeof window !== 'undefined'`; SSR does not clear
  unrelated requests' state. No new cache manager, backend change or API response change.
- One added focused account-change check; existing auth tests now assert cache clearing for
  login/register/401. Failed login preserves the previously confirmed identity.
- 24 scoped frontend tests passed: auth, post-Lesson retention dialogs, invalidation and
  best-effort refresh. Six existing PostgreSQL API smoke tests passed: daily loop, concurrent
  completion, archived History, Knowledge ownership, Review batch retry and Profile/Discover.
- Touched-file ESLint and Nuxt type-check passed. API and Nuxt production images rebuilt;
  PostgreSQL data volume preserved. No full regression suite or migrations were needed.
- Explicit Development content seed rerun on the local database reported zero new content
  items; emitted SQL was reads only, no delete/reinsert or editorial updates. No demo-user
  password was modified. This is not a before/after checksum audit of every historical table.

### Remaining Day 1–2 gate

Use an existing realistic account to finish authenticated browser verticals A–G, natural
Weak Topic action if present, one account switch, deep links/back navigation and an editable
draft failure/conflict. Do not label API results as real user dogfood or mark these gates
complete without that observation. Credentials/login were requested from the user.

No new P0 data-integrity failure observed in scoped checks. The cache boundary risk is
hardened and checked, but live account-switch validation remains outstanding. No claim of
release readiness, bug-free behavior, retention benefit or subjective product usefulness.
Bookmark, Learning Paths, AI, new ranking signals, curriculum expansion and P2 polish remain
deferred. Fix further P0/P1 only when those remaining flows expose a concrete problem.

## 2026-10-09 — Recovery and interaction hardening, Days 3–4

The user authorized creating a separate local dogfood account. Normal authenticated APIs
created its Profile, two lesson completions, two due Review cards and a mixed Study Session;
the browser also saved a Knowledge note. Existing accounts/passwords and historical data
were not reset. This is representative interaction smoke, not delayed human retention testing.

| Flow | Finding | Resolution |
| --- | --- | --- |
| Login after cache isolation | P1: clearing every Nuxt state emptied the framework toast array; login succeeded but the UI reported failure | Clear personalized queries, Knowledge state and private open/query controls; retain framework state and reset notifications to an array |
| Today / Discover refresh | P1 recovery risk: cached results could remain authoritative after refresh failed | Gate action/recommendation rendering on initial or refresh errors; retain retry and Learn fallback, without inventing a replacement action |
| Profile conflict reload | P1: clearing the draft before a failed reload lost local work | Replace draft/version only after a successful GET; failed reload preserves local input and conflict, with explicit feedback |
| Knowledge dialog keyboard | Escape was always disabled | Escape uses the existing guarded close path; pending saves remain non-dismissible; dirty draft still requires confirmation |
| Mixed Session summary | P1 semantic copy: Lesson completion evidence was labelled DSA attempt | Explicit evidence-kind labels; Lesson completion has no fabricated attempt duration |

### Observed browser checks

- Mobile 390 × 844: Today primary action, Review queue/focus, Lesson reader,
  Resource detail, Knowledge dialog and empty Plan picker. Review ratings measured
  175 × 56 px, in two columns, with no horizontal page overflow. Code blocks scroll
  internally rather than expanding the lesson viewport. Knowledge dialog measured
  358 × 678 px with visible labelled fields and actions.
- Keyboard Review: Space reveal, Good/Again numeric ratings, Enter next, then completion.
  Answers were hidden before reveal. Good and Again both saved normal next intervals.
  Opening the lesson source used a separate tab; the original Review session continued.
  The source lesson still displayed Completed after Again.
- Knowledge: initial focus on Title, Escape on a clean draft returns to the trigger;
  Escape on edited Title invokes discard confirmation, Cancel preserves the draft;
  normal Save displays Saved to Knowledge. Plan picker distinguishes loading from
  no editable plan; Escape closes it and returns focus.
- Mixed Session: lesson completion attached evidence, returning selected the external
  resource. Opening Microsoft Learn left the task pending. Explicit task completion
  completed the Session. Resource detail never displayed lesson progress controls.
- Production Docker logout/login returned to Today successfully, without the former
  false login failure. This verifies the actual toast/cache repair, not cross-account isolation.
- Tablet 768 × 1024: existing completed lesson and post-lesson actions remained readable.
  Desktop was inspected before mobile checks. These are sampled responsive checks,
  not certification or an exhaustive screen-reader/200%-zoom audit.

### Scoped verification and limits

- 33 frontend checks passed across auth, recovery states, lesson retention, learning
  content and session navigation; the four retention checks also reran after dialog change.
  Only two new recovery checks were added for the identified Today/Profile failures.
- Four existing PostgreSQL API checks passed: archived History snapshots, unavailable
  direct content, external task ownership and stale Profile version/ownership. Archived
  behavior is API evidence, not a claim that production content was archived for dogfood.
- Touched-file ESLint and Nuxt type-check passed. Docker production web rebuilt without
  changing PostgreSQL storage or API semantics. Nuxt/Nitro emitted its existing unused
  H3Error/H3Event import warning; this is not a warning-free build claim.
- Historical rendering/new eligibility, mutation refresh separation and dialog draft
  handling were source-audited. No new domain, scheduler, taxonomy bridge, migration,
  resource progress or automatic retry was introduced.
- Directly created Session items currently display 0 planned minutes; recorded as P2
  presentation debt outside this recovery patch. Plan-generated duration semantics were
  not changed. Full browser account-switch isolation, all archived historical screens,
  exhaustive filter combinations and formal assistive-technology checks remain separate
  release checks. No blanket release-readiness claim is made.
