# DevRecall security model

DevRecall is a local-first, same-origin application. Its primary v1 trust boundaries are authentication, user ownership, mutation integrity, safe rendering, bounded inputs, and non-disclosing errors.

## Authentication and cookies

ASP.NET Core cookie authentication uses `devrecall.auth`. The cookie is HttpOnly, essential, scoped to `/`, SameSite=Lax, and valid for eight sliding hours. `Authentication:RequireHttpsCookies` defaults to `true`; Development explicitly disables it for local HTTP only. Production therefore emits Secure cookies and must run behind HTTPS.

Logout calls the server endpoint and expires the authentication ticket. The Nuxt auth store then clears current-user state and all private query state; setting the frontend user to null without the server logout is not sufficient.

No access token, refresh token, or copy of the authentication cookie may be stored in localStorage, sessionStorage, or IndexedDB. UI-only preferences may use browser storage when they contain no authentication or learning data.

## CSRF and CORS

Cookie-authenticated mutations under `/api/v1` require ASP.NET Core antiforgery validation outside the Testing environment. The client first obtains `/api/v1/auth/csrf-token`, then sends its request token in `X-CSRF-TOKEN`. It may refresh the token once after an antiforgery failure, but it must not automatically retry a business mutation.

SameSite=Lax provides an additional browser boundary but is not treated as the only CSRF defense. Development CORS permits only `http://localhost:3000` with credentials. Production registers no cross-origin policy because the deployed Nuxt server proxies same-origin `/api` traffic. `AllowAnyOrigin` with credentials is prohibited.

Login and registration use a fixed-window, client-address rate-limit policy. The default production boundary is 10 requests per minute and can be changed with `RateLimiting:Authentication:PermitLimit`. Testing uses a high explicit value so the shared integration-test address does not distort the suite.

## Authorization and ownership

Every user-owned query and mutation is scoped by the authenticated user ID. UUIDs are identifiers, not authorization. Nested resources must also match their route context—for example, an attempt must belong to the requested question/problem, and a Session item must belong to the requested Session.

Requests for resources outside the authenticated user's scope return 404 rather than 403. This deliberately avoids disclosing that another user's resource exists. Submitted Interview or DSA evidence is accepted for Study Session completion only when the user, resource ID, and evidence type all match.

## Input and persistence boundaries

The backend owns all validation. Text lengths, pagination, search, analytics ranges, batch sizes, and lifecycle versions remain bounded even when the frontend also validates them for usability.

Public enum-like values use stable string names such as `Good`, `Strong`, `Solved`, and `High`. Numeric strings are rejected before `Enum.TryParse`, because .NET otherwise accepts enum ordinals that are not part of the public contract.

EF Core LINQ is parameterized. Raw SQL must use parameterized APIs, and arbitrary sort fields must map through an allowlist. Application code must not reference EF Core provider exceptions. Infrastructure maps named PostgreSQL constraints and concurrency failures into persistence-neutral exceptions that Application translates to stable business errors.

## Rendering and XSS

User-authored Knowledge, Interview, DSA, Study reflection, title, tag, and search text is rendered through Vue interpolation or form controls, which escape text. Raw user content must never be passed directly to `v-html`. A future Markdown renderer must use an established sanitization library and reject scripts, event-handler attributes, and `javascript:` URLs.

Search highlighting must be represented as escaped text segments rather than concatenated HTML. Recommendation and Insight actions are built from allowlisted resource types and internal route builders; stored arbitrary URLs are not trusted navigation targets.

## Errors, logging, and privacy

Production exceptions are handled centrally. Unexpected failures return generic Problem Details with `INTERNAL_SERVER_ERROR`, a trace ID, and no stack trace, SQL, constraint message, connection string, or filesystem path. Detailed exceptions remain server-side logs. Request logging records method, path, status, duration, correlation ID context, and stable error code where useful; it does not log passwords, cookies, request bodies, answers, solutions, Knowledge content, or reflections.

List and analytics endpoints project only the fields required by their UI. Analytics never returns answer bodies, solutions, Knowledge content, or Session reflections. Weak Topic evidence and Recommendation details contain bounded resource metadata rather than internal algorithm diagnostics.

All API responses receive `Cache-Control: no-store`; private Nuxt auth/app routes are also `no-store` and noindex. Public landing and feature pages may be prerendered and cached because they do not bootstrap authentication or private learning data.

## Production hosting

OpenAPI is mapped only in Development. Production enables HTTPS redirection and HSTS. API and Nuxt responses set `X-Content-Type-Options: nosniff`, `Referrer-Policy: strict-origin-when-cross-origin`, and `X-Frame-Options: DENY`.

A strict Content Security Policy is intentionally deferred until the exact deployment's Nuxt scripts, styles, fonts, images, and API proxy have been inventoried and tested. Adding an unverified CSP shortly before release could break hydration while providing misleading assurance.

Secrets belong in environment variables or user secrets. Development examples must contain obvious placeholders only. Demo data is created by an explicit development-only CLI, never by an unauthenticated production endpoint or automatic API startup hook.

## Release audit

Use [regression-checklist.md](./regression-checklist.md) for the complete release matrix. High-value static review includes `v-html`, raw SQL APIs, EF exceptions in Application, permissive CORS, browser storage, redirect handling, enum parsing, exception messages, OpenAPI exposure, and debug endpoints.
