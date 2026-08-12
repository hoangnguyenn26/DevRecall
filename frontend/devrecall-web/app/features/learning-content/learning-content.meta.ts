export function difficultyColor(value: string): 'success' | 'warning' | 'error' | 'neutral' {
  if (value === 'Beginner') return 'success'
  if (value === 'Intermediate') return 'warning'
  if (value === 'Advanced') return 'error'
  return 'neutral'
}

export function visibleTags<T>(values: T[], maximum = 3): { visible: T[]; hiddenCount: number } {
  return { visible: values.slice(0, maximum), hiddenCount: Math.max(0, values.length - maximum) }
}
