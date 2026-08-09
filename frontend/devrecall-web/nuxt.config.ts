export default defineNuxtConfig({
  compatibilityDate: '2026-08-01',
  modules: ['@nuxt/ui', '@pinia/nuxt', '@nuxt/eslint'],
  ui: { fonts: false },
  icon: { serverBundle: { collections: ['lucide'] } },
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
  eslint: { config: { stylistic: false } },
  vite: {
    build: {
      chunkSizeWarningLimit: 600,
      rolldownOptions: { checks: { pluginTimings: false } },
    },
  },
})
