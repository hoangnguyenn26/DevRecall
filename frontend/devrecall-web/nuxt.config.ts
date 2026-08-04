export default defineNuxtConfig({
  compatibilityDate: '2026-08-01',
  modules: ['@nuxt/ui', '@pinia/nuxt', '@nuxt/eslint'],
  ui: { fonts: false },
  icon: { serverBundle: { collections: ['lucide'] } },
  devtools: { enabled: true },
  css: ['~/assets/css/main.css'],
  runtimeConfig: {
    apiInternalBaseUrl: 'https://localhost:7081',
    public: {
      apiBaseUrl: 'https://localhost:7081/api/v1',
      siteUrl: 'http://localhost:3000',
    },
  },
  app: {
    head: {
      htmlAttrs: { lang: 'en' },
      titleTemplate: '%s · DevRecall',
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
