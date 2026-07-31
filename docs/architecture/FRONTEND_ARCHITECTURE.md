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

Nuxt UI supplies the common visual primitives. PrimeVue is restricted to the unstyled Knowledge Tree, and ECharts is loaded only by client-side Analytics views. Pinia holds authenticated session state; business state remains server-owned.

Cookie authentication and antiforgery tokens are transported through the typed API composable. Mutating requests are never retried automatically. `409 Conflict` responses require a visible reload decision, especially for versioned Study Plans and Study Sessions.

The production Nuxt server proxies `/api/**` to ASP.NET Core so the browser has a same-origin surface. Direct development calls use `NUXT_PUBLIC_API_BASE_URL=https://localhost:7081/api/v1`.
