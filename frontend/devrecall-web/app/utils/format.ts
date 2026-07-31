export function formatMinutes(minutes: number) {
  if (minutes < 60) return `${minutes}m`
  const hours = Math.floor(minutes / 60)
  const remainder = minutes % 60
  return remainder ? `${hours}h ${remainder}m` : `${hours}h`
}

export function formatDateTime(value?: string) {
  if (!value) return '—'
  return new Intl.DateTimeFormat(undefined, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(value))
}

export function resourceRoute(type: string, id: string) {
  const normalized = type.toLowerCase()
  if (normalized.includes('knowledge')) return `/app/knowledge?node=${id}`
  if (normalized.includes('interview')) return `/app/interview/${id}`
  if (normalized.includes('dsa')) return `/app/dsa/${id}`
  if (normalized.includes('review')) return '/app/review'
  return '/app/today'
}
