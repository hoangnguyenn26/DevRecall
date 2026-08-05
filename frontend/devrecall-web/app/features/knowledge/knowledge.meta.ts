export const knowledgeSortOptions = [
  { label: 'Recently updated', value: 'updated' },
  { label: 'Recently created', value: 'created' },
  { label: 'Title A–Z', value: 'title' },
]

export function formatRelativeKnowledgeDate(value: string, now = Date.now()): string {
  const deltaDays = Math.round((new Date(value).getTime() - now) / 86_400_000)
  if (Math.abs(deltaDays) < 1) return 'Updated today'
  return new Intl.RelativeTimeFormat('en', { numeric: 'auto' }).format(deltaDays, 'day')
}
