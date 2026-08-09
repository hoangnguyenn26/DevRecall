export default defineNuxtConfig({
  compatibilityDate: '2026-08-01',
  modules: ['@nuxt/ui', '@pinia/nuxt', '@nuxt/eslint'],
  ui: { fonts: false },
  icon: {
    serverBundle: { collections: ['lucide'] },
    clientBundle: {
      icons: [
        'lucide:library',
        'lucide:refresh-cw',
        'lucide:triangle-alert',
        'lucide:sparkles',
        'lucide:list-checks',
        'lucide:chart-no-axes-combined',
        'lucide:brain-circuit',
        'lucide:arrow-up-right',
      ],
    },
  },
  devtools: { enabled: true },
  css: ['~/assets/css/main.css'],
  runtimeConfig: {
    apiInternalBaseUrl: 'http://localhost:5012',
    public: {
      apiBaseUrl: '/api/v1',
      siteUrl: process.env.NUXT_PUBLIC_SITE_URL ?? 'http://localhost:3000',
      siteIndexable: process.env.NUXT_PUBLIC_SITE_INDEXABLE !== 'false',
    },
  },
  routeRules: {
    '/': { prerender: true },
    '/features': { prerender: true },
    '/app/**': { ssr: false, headers: { 'X-Robots-Tag': 'noindex, nofollow' } },
    '/login': { headers: { 'X-Robots-Tag': 'noindex, nofollow' } },
    '/register': { headers: { 'X-Robots-Tag': 'noindex, nofollow' } },
  },
  app: {
    head: {
      htmlAttrs: { lang: 'en' },
      titleTemplate: '%s — DevRecall',
      meta: [
        { name: 'theme-color', content: '#4f46e5' },
        { name: 'color-scheme', content: 'light dark' },
      ],
    },
  },
  typescript: { strict: true, typeCheck: true },
  experimental: { prefetchPreloadTags: false },
  hooks: {
    'build:manifest': (manifest) => {
      for (const chunk of Object.values(manifest)) chunk.dynamicImports = []
    },
  },
  eslint: { config: { stylistic: false } },
  vite: {
    build: {
      chunkSizeWarningLimit: 600,
      rolldownOptions: { checks: { pluginTimings: false } },
    },
  },
})
