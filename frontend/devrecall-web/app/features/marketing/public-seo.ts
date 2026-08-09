import { publicRoutes } from './marketing.constants'

export const indexablePublicRoutes = [publicRoutes.home, publicRoutes.features] as const

export function normalizeSiteUrl(siteUrl: string): string {
  const url = new URL(siteUrl)
  if (!['http:', 'https:'].includes(url.protocol))
    throw new Error('Public site URL must use HTTP or HTTPS.')
  url.pathname = ''
  url.search = ''
  url.hash = ''
  return url.toString().replace(/\/$/, '')
}

export function buildRobotsTxt(siteUrl: string, indexable: boolean): string {
  if (!indexable) return 'User-agent: *\nDisallow: /\n'
  const origin = normalizeSiteUrl(siteUrl)
  return `User-agent: *\nAllow: /\nDisallow: /app\nDisallow: /app/\nDisallow: /login\nDisallow: /register\nSitemap: ${origin}/sitemap.xml\n`
}

export function buildSitemapXml(siteUrl: string): string {
  const origin = escapeXml(normalizeSiteUrl(siteUrl))
  const urls = indexablePublicRoutes
    .map((path) => `  <url><loc>${origin}${path}</loc></url>`)
    .join('\n')
  return `<?xml version="1.0" encoding="UTF-8"?>\n<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">\n${urls}\n</urlset>`
}

export function buildPublicStructuredData(siteUrl: string): Record<string, unknown>[] {
  const origin = normalizeSiteUrl(siteUrl)
  return [
    { '@context': 'https://schema.org', '@type': 'WebSite', name: 'DevRecall', url: `${origin}/` },
    {
      '@context': 'https://schema.org',
      '@type': 'SoftwareApplication',
      name: 'DevRecall',
      applicationCategory: 'EducationalApplication',
      operatingSystem: 'Web',
      url: `${origin}/`,
      description:
        'A learning workspace for developers to capture knowledge, practice deliberately, review what they learn, and plan what to study next.',
    },
  ]
}

function escapeXml(value: string): string {
  return value
    .replaceAll('&', '&amp;')
    .replaceAll('<', '&lt;')
    .replaceAll('>', '&gt;')
    .replaceAll('"', '&quot;')
    .replaceAll("'", '&apos;')
}
