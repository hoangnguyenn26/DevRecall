import { buildPublicStructuredData } from '~/features/marketing/public-seo'

export function usePublicStructuredData(): void {
  const config = useRuntimeConfig()
  useHead({
    script: buildPublicStructuredData(config.public.siteUrl).map((schema) => ({
      type: 'application/ld+json',
      innerHTML: JSON.stringify(schema),
    })),
  })
}
