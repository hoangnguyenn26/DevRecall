import { readFileSync } from 'node:fs'
import { fileURLToPath } from 'node:url'
import { describe, expect, it } from 'vitest'

const routes = [
  '../../pages/app/review/session.vue',
  '../../pages/app/interview/[id]/practice.vue',
  '../../pages/app/dsa/[id]/practice.vue',
]

describe('focus practice routes', () => {
  it.each(routes)('%s uses the authenticated Focus layout', (route) => {
    const source = readFileSync(fileURLToPath(new URL(route, import.meta.url)), 'utf8')
    expect(source).toContain("definePageMeta({ layout: 'focus', middleware: 'auth' })")
  })

  it.each(routes.slice(1))('%s imports the shared Focus components', (route) => {
    const source = readFileSync(fileURLToPath(new URL(route, import.meta.url)), 'utf8')
    expect(source).toContain("import PracticeShell from '~/features/practice/components/PracticeShell.vue'")
    expect(source).toContain("import PracticeCompletion from '~/features/practice/components/PracticeCompletion.vue'")
  })

  it.each([
    '../../pages/app/interview/[id]/index.vue',
    '../../pages/app/dsa/[id]/index.vue',
  ])('%s is an explicit sibling detail route', (route) => {
    const source = readFileSync(fileURLToPath(new URL(route, import.meta.url)), 'utf8')
    expect(source).toContain("definePageMeta({ layout: 'app' })")
  })
})
