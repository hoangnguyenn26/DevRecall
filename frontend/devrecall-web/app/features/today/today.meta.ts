const todayActionIconMeta: Record<string, string> = {
  timer: 'i-lucide-timer',
  'list-checks': 'i-lucide-list-checks',
  refresh: 'i-lucide-refresh-cw',
  pencil: 'i-lucide-pencil-line',
  sparkles: 'i-lucide-sparkles',
  'list-plus': 'i-lucide-list-plus',
  'book-open': 'i-lucide-book-open',
}

export function getTodayActionIcon(key: string): string {
  return todayActionIconMeta[key] ?? 'i-lucide-circle-arrow-right'
}

export function getTodayResourcePath(type: string, id: string): string {
  if (type === 'KnowledgeNode') return `/app/knowledge?node=${id}`
  if (type === 'InterviewQuestion') return `/app/interview/${id}`
  if (type === 'DsaProblem') return `/app/dsa/${id}`
  return '/app'
}
