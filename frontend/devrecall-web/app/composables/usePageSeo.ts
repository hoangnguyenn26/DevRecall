interface PageSeoOptions {
  title: string
  description: string
  path: string
  robots?: string
}

export function usePageSeo(options: PageSeoOptions): void {
  const config = useRuntimeConfig()
  const canonical = new URL(options.path, `${config.public.siteUrl.replace(/\/$/, '')}/`).toString()

  useSeoMeta({
    title: options.title,
    description: options.description,
    robots: options.robots ?? 'index, follow',
    ogTitle: options.title,
    ogDescription: options.description,
    ogUrl: canonical,
    ogType: 'website',
    twitterCard: 'summary_large_image',
    twitterTitle: options.title,
    twitterDescription: options.description,
  })
  useHead({ link: [{ rel: 'canonical', href: canonical }] })
}
