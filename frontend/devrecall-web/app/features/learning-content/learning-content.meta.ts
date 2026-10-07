export function difficultyColor(value: string): 'success' | 'warning' | 'error' | 'neutral' {
  if (value === 'Beginner') return 'success'
  if (value === 'Intermediate') return 'warning'
  if (value === 'Advanced') return 'error'
  return 'neutral'
}

export function resourceKindLabel(value: string | null | undefined): string {
  const labels: Record<string, string> = { Documentation: 'Documentation', Guide: 'Guide', Tutorial: 'Tutorial', Reference: 'Reference' }
  return value ? labels[value] ?? 'Resource' : 'Resource'
}

export function safeResourceUrl(value: string | null): string | undefined {
  if (!value) return undefined
  try {
    const url = new URL(value)
    return ['https:', 'http:'].includes(url.protocol) && !url.username && !url.password ? url.href : undefined
  } catch { return undefined }
}

export function visibleTags<T>(values: T[], maximum = 3): { visible: T[]; hiddenCount: number } {
  return { visible: values.slice(0, maximum), hiddenCount: Math.max(0, values.length - maximum) }
}
