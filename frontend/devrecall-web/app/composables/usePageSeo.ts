interface PageSeoOptions {
  title: string
  description: string
  path: string
  robots?: string
  ogTitle?: string
}

export function usePageSeo(options: PageSeoOptions): void {
  const config = useRuntimeConfig()
  const canonical = new URL(options.path, `${config.public.siteUrl.replace(/\/$/, '')}/`).toString()
  const robots =
    config.public.siteIndexable === false
      ? 'noindex, nofollow'
      : (options.robots ?? 'index, follow')

  useSeoMeta({
    title: options.title,
    description: options.description,
    robots,
    ogTitle: options.ogTitle ?? options.title,
    ogDescription: options.description,
    ogUrl: canonical,
    ogType: 'website',
    twitterCard: 'summary_large_image',
    twitterTitle: options.ogTitle ?? options.title,
    twitterDescription: options.description,
  })
  useHead({ link: [{ rel: 'canonical', href: canonical }] })
}
