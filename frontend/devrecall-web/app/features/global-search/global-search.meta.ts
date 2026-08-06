import type { GlobalSearchGroup, GlobalSearchResourceType, GlobalSearchResult } from './global-search.types'

const metadata: Record<string, { label: string; icon: string }> = {
  Knowledge: { label: 'Knowledge', icon: 'i-lucide-book-open' },
  InterviewQuestion: { label: 'Interview questions', icon: 'i-lucide-messages-square' },
  DsaProblem: { label: 'DSA problems', icon: 'i-lucide-code-2' },
}

export function getGlobalSearchMetadata(type: GlobalSearchResourceType) {
  const value = metadata[type]
  return value ? { ...value, known: true } : { label: 'Other results', icon: 'i-lucide-file-question', known: false }
}

export function groupGlobalSearchResults(results: GlobalSearchResult[]): GlobalSearchGroup[] {
  const groups = new Map<string, GlobalSearchResult[]>()
  for (const result of results) groups.set(result.resourceType, [...(groups.get(result.resourceType) ?? []), result])
  return [...groups].map(([type, items]) => ({ type, results: items, ...getGlobalSearchMetadata(type) }))
}
