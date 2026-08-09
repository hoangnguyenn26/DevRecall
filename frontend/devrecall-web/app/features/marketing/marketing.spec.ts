import { readFileSync } from 'node:fs'
import { resolve } from 'node:path'
import { afterEach, describe, expect, it, vi } from 'vitest'
import { usePageSeo } from '~/composables/usePageSeo'

const readAppFile = (path: string) => readFileSync(resolve(process.cwd(), 'app', path), 'utf8')

describe('public website foundation', () => {
  afterEach(() => vi.unstubAllGlobals())

  it('builds canonical and social metadata from configured site URL', () => {
    const useSeoMeta = vi.fn<(meta: Record<string, unknown>) => void>()
    const useHead = vi.fn<(head: Record<string, unknown>) => void>()
    vi.stubGlobal('useRuntimeConfig', () => ({
      public: { siteUrl: 'https://devrecall.example/base' },
    }))
    vi.stubGlobal('useSeoMeta', useSeoMeta)
    vi.stubGlobal('useHead', useHead)

    usePageSeo({
      title: 'Features',
      description: 'Developer learning features.',
      path: '/features',
    })

    expect(useSeoMeta).toHaveBeenCalledWith(
      expect.objectContaining({
        title: 'Features',
        robots: 'index, follow',
        ogUrl: 'https://devrecall.example/features',
        twitterCard: 'summary_large_image',
      }),
    )
    expect(useHead).toHaveBeenCalledWith({
      link: [{ rel: 'canonical', href: 'https://devrecall.example/features' }],
    })
  })

  it('keeps public and private surfaces separate', () => {
    const landing = readAppFile('pages/(marketing)/index.vue')
    const publicLayout = readAppFile('layouts/public.vue')
    const appLayout = readAppFile('layouts/app.vue')
    const header = readAppFile('features/marketing/components/PublicHeader.vue')

    expect(landing).toContain("definePageMeta({ layout: 'public' })")
    expect(publicLayout).toContain('href="#main-content"')
    expect(publicLayout).toContain('<PublicHeader />')
    expect(publicLayout).toContain('<PublicFooter />')
    expect(appLayout).toContain("content: 'noindex,nofollow'")
    expect(header).toContain("label: 'Open DevRecall'")
    expect(header).not.toMatch(/Today|Knowledge|Review|Analytics/)
  })

  it('keeps auth pages out of the search index', () => {
    expect(readAppFile('pages/(authentication)/login.vue')).toContain("robots: 'noindex, nofollow'")
    expect(readAppFile('pages/(authentication)/register.vue')).toContain(
      "robots: 'noindex, nofollow'",
    )
  })

  it('tells the complete product story without unsupported claims', () => {
    const landing = readAppFile('pages/(marketing)/index.vue')
    const constants = readAppFile('features/marketing/marketing.constants.ts')
    const copy = `${landing}\n${constants}`

    for (const capability of [
      'Knowledge',
      'Review',
      'Interview',
      'DSA',
      'Weak Topics',
      'Recommendations',
      'Study Plans',
      'Study Session',
      'Analytics',
      'Today',
    ]) {
      expect(copy).toContain(capability)
    }
    expect(copy).not.toMatch(
      /AI-powered|automatic answer evaluation|code execution|cloud sync|10,000 developers|unlock your full potential/i,
    )
    expect(landing).toContain('id="how-it-works"')
    expect(landing).toContain('id="features"')
  })

  it('provides a complete feature page with stable unique anchors', () => {
    const features = readAppFile('pages/(marketing)/features.vue')
    const anchors = [...features.matchAll(/<FeaturePageSection\s+id="([^"]+)"/g)].map(
      (match) => match[1],
    )

    expect(features).toContain("definePageMeta({ layout: 'public' })")
    expect(features.match(/<h1/g) ?? []).toHaveLength(1)
    expect(anchors).toEqual(['knowledge', 'practice', 'study', 'insights', 'today'])
    expect(new Set(anchors).size).toBe(anchors.length)
    expect(features).toContain('to="/register"')
  })

  it('keeps illustrative feature previews out of the keyboard flow', () => {
    const previews = readAppFile('features/marketing/components/FeaturePreviews.vue')
    expect(previews).toContain('aria-hidden="true"')
    expect(previews).not.toMatch(/<(button|a|input|select|textarea)\b/)
  })
})
