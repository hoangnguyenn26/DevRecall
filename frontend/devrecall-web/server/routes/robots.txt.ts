import { buildRobotsTxt } from '../../app/features/marketing/public-seo'

export default defineEventHandler((event) => {
  setHeader(event, 'content-type', 'text/plain; charset=utf-8')
  const config = useRuntimeConfig(event)
  return buildRobotsTxt(config.public.siteUrl, config.public.siteIndexable)
})
