# Public Web

DevRecall keeps its public product website separate from the authenticated Learning OS.

## Route and indexing policy

| Route | Rendering | Indexing |
| --- | --- | --- |
| `/` | prerendered | index, follow |
| `/features` | prerendered | index, follow |
| `/login` and `/register` | SSR | noindex, nofollow |
| `/app/**` | client-heavy | noindex, nofollow |
| `/robots.txt` and `/sitemap.xml` | server-generated | public discovery infrastructure |

Public canonicals, Open Graph metadata and JSON-LD use `NUXT_PUBLIC_SITE_URL`. Production must set that value to the canonical HTTPS origin. `NUXT_PUBLIC_SITE_INDEXABLE=false` produces a deny-all robots policy for preview environments. The sitemap intentionally contains only `/` and `/features`.

## Conversion and authentication

```text
New user:       Landing or Features -> Register -> Onboarding -> Today
Existing user:  Landing -> Login -> Onboarding (if incomplete) -> Today
Protected link: Private deep link -> Login -> validated returnTo
```

Registration creates the account and authentication cookie in one response. Login and registration then read the existing onboarding status; the frontend never skips onboarding on the user's behalf.

`returnTo` accepts only a local `/app` path (including its query and fragment). Absolute, protocol-relative, encoded external, backslash-containing and non-app destinations fall back to `/app`. Resource existence is checked by the destination feature after authentication, not by the auth flow.

Logout clears the authentication store, Nuxt query payloads and Nuxt client state before replacing the current history entry with `/login`. Protected-route middleware still rechecks the server session, so browser history cannot expose private content after logout.

## Delivery boundary

Public marketing routes must not depend on private learning APIs or eagerly import private workspace features. Their header and calls to action are deterministic anonymous content, which makes public prerendering cache-safe and avoids a global `/auth/me` request.

Public pages remain primarily server-rendered/static. Interactive JavaScript is reserved for navigation, theme and lightweight presentation. Core headings, copy and normal links are present in HTML before hydration. The hero preview declares intrinsic dimensions and an aspect ratio to reserve layout space.

The PostgreSQL API, private stores, ECharts analytics and workspace editors belong to `/app/**` route chunks. Build review should confirm that marketing entry scripts do not eagerly reference those feature chunks.
