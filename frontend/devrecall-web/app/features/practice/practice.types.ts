export type PracticeModule = 'Review' | 'Interview' | 'Dsa' | 'StudySession'
export type PracticePhase = 'loading' | 'ready' | 'active' | 'submitting' | 'completed' | 'error'

export interface PracticeProgress {
  current: number
  total: number
  completed: number
  percent: number
}

export interface PracticeShellContext {
  module: PracticeModule
  title: string
  progress?: PracticeProgress
  startedAtUtc?: string
}

export interface PracticeShortcut {
  id: string
  keys: string[]
  label: string
  enabled: () => boolean
  execute: () => void | Promise<void>
}

export interface PracticeCompletionSummary {
  title: string
  completedCount: number
  durationMinutes?: number
  primaryMetricLabel?: string
  primaryMetricValue?: string
  secondaryMetrics?: { label: string; value: string }[]
}
