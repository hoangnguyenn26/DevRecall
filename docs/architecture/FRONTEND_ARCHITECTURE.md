# Frontend Architecture

DevRecall uses Nuxt 4 as two presentation layers over the existing ASP.NET Core API:

```text
Public SSR routes                     Authenticated Learning OS
/, /login, /register                 /app/** (client-heavy, noindex)
         \                              /
          Nuxt UI + domain components
                    |
             typed API composable
                    |
       ASP.NET Core /api/v1 + Problem Details
```

Nuxt UI supplies the common visual primitives. The Knowledge Tree uses lightweight application components, while ECharts is loaded only by client-side Analytics views. Pinia holds authenticated session state; business state remains server-owned.

Cookie authentication and antiforgery tokens are transported through the typed API composable. Mutating requests are never retried automatically. `409 Conflict` responses require a visible reload decision, especially for versioned Study Plans and Study Sessions.

## Data loading and reliability

Private routes request bounded projections instead of downloading complete modules.
Management lists and histories use server pagination; command-palette search is
debounced and cancels its previous request. Analytics accepts only 7, 30 or 90 day
ranges and aborts stale requests when the range changes or the view unmounts.
DevRecall does not poll Today or navigation indicators.

Nuxt query keys are module-specific. Mutations refresh only affected entry points;
analytics and cross-signal projections are cleared lazily when immediate display is
not required. A successful business mutation and a failed follow-up refresh are two
different outcomes: the UI keeps the saved state, shows a refresh-delayed warning,
and never asks the user to repeat the mutation. Review, Interview and DSA submission
identifiers remain stable across a retry and rotate only for the next logical action.

Logout clears the authentication store, CSRF state, Nuxt data and Nuxt state. Global
keyboard listeners, network listeners, route guards, timers and abort controllers
are removed on unmount or scope disposal. Private learning content is not persisted
to local storage; the only browser-persisted workspace preference is non-sensitive
navigation state.

## Bundle boundaries

Public pages are prerendered and do not import authenticated stores or workspaces.
`/app/**` is client-heavy and route-split. Analytics is a private route chunk;
`ChartContainer.client.vue` imports the ECharts core with only Bar, Line, Pie, Grid,
Tooltip, ARIA and Canvas modules. ECharts is therefore absent from the landing-page
entry path and loaded only when the analytics route is requested.

Inspect a release bundle with `npm run build`; compare emitted chunk sizes with
`docs/PERFORMANCE_BASELINE.md`. A large private analytics chunk does not by itself
indicate a public-site regression, so verify its importer and route boundary before
optimizing by size alone.

The production Nuxt server proxies `/api/**` to ASP.NET Core so the browser has a same-origin surface. Direct development calls use `NUXT_PUBLIC_API_BASE_URL=https://localhost:7081/api/v1`.
