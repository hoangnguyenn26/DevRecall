type RouteQueryValue = string | (string | null)[] | null | undefined

export interface CanonicalKnowledgeLocation {
  path: string
  query: Record<string, string>
}

const guidPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i

function first(value: RouteQueryValue): string | undefined {
  return Array.isArray(value) ? value.find(item => item !== null) ?? undefined : value ?? undefined
}

export function canonicalizeKnowledgeLocation(path: string, rawQuery: Record<string, RouteQueryValue>): CanonicalKnowledgeLocation {
  const selectedMatch = path.match(/^\/app\/knowledge\/([^/]+)$/)
  const selectedId = selectedMatch?.[1]
  const canonicalPath = selectedId && guidPattern.test(selectedId) ? `/app/knowledge/${selectedId.toLowerCase()}` : '/app/knowledge'
  const query: Record<string, string> = {}
  const topicId = first(rawQuery.topicId)?.trim()
  const topicScope = first(rawQuery.topicScope)?.trim().toLowerCase()
  if (topicId && guidPattern.test(topicId)) query.topicId = topicId.toLowerCase()
  else if (topicScope === 'uncategorized') query.topicScope = 'uncategorized'

  const search = first(rawQuery.query)?.trim()
  if (search) query.query = search
  const tags = [...new Set((first(rawQuery.tagIds) ?? '').split(',')
    .map(value => value.trim().toLowerCase()).filter(value => guidPattern.test(value)))].sort()
  if (tags.length) query.tagIds = tags.join(',')
  const sort = first(rawQuery.sort)?.trim().toLowerCase()
  if (sort === 'created' || sort === 'title') query.sort = sort
  const page = Number(first(rawQuery.page))
  if (Number.isInteger(page) && page > 1) query.page = String(page)
  return { path: canonicalPath, query }
}

export function isCanonicalKnowledgeLocation(currentPath: string, currentQuery: Record<string, RouteQueryValue>, canonical: CanonicalKnowledgeLocation): boolean {
  if (currentPath !== canonical.path) return false
  const current = Object.fromEntries(Object.entries(currentQuery).filter(([, value]) => value !== undefined && value !== null)
    .map(([key, value]) => [key, Array.isArray(value) ? value.join(',') : value]))
  return JSON.stringify(current) === JSON.stringify(canonical.query)
}
