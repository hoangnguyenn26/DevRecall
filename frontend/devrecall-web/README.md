# DevRecall Web

Nuxt 4 presentation layer for DevRecall. It contains a small server-rendered public site and a client-heavy authenticated Learning OS under `/app`.

## Stack

- Nuxt 4, Vue 3, and TypeScript
- Nuxt UI and Tailwind design tokens
- Pinia authentication state
- PrimeVue unstyled Knowledge Tree
- Apache ECharts analytics
- Vitest and Playwright

## Development

```powershell
Copy-Item .env.example .env.local
npm install
npm run dev
```

The default API target is `https://localhost:7081/api/v1`; the web application listens on `http://localhost:3000`.

## Quality gates

```powershell
npm run type-check
npm run lint
npm run test:unit
npm run build
```

Playwright smoke tests require the API for authenticated journeys:

```powershell
npm run test:e2e
```

## Runtime configuration

| Variable | Purpose |
| --- | --- |
| `NUXT_PUBLIC_API_BASE_URL` | Browser-visible API base URL |
| `NUXT_API_INTERNAL_BASE_URL` | ASP.NET Core origin used by the server-side `/api/**` proxy |
| `NUXT_PUBLIC_SITE_URL` | Canonical public URL for sitemap and metadata |
