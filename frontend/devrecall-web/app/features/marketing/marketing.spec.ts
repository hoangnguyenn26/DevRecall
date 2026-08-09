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
})
