import type { UiStatusMeta } from '~/types/ui-meta'

export const studyPlanStatusMeta = {
  Draft: { label: 'Draft', tone: 'neutral', icon: 'i-lucide-pencil-line' },
  Ready: { label: 'Ready', tone: 'info', icon: 'i-lucide-list-checks' },
  Converted: { label: 'Converted', tone: 'success', icon: 'i-lucide-circle-check' },
  Cancelled: { label: 'Cancelled', tone: 'error', icon: 'i-lucide-circle-x' },
} satisfies Record<string, UiStatusMeta>

export const recommendationPriorityMeta = {
  Low: { label: 'Low', tone: 'neutral' },
  Medium: { label: 'Medium', tone: 'info' },
  High: { label: 'High', tone: 'warning' },
  Critical: { label: 'Critical', tone: 'error' },
} satisfies Record<string, UiStatusMeta>

export const moduleAccentMeta = {
  Knowledge: 'indigo',
  Review: 'amber',
  Interview: 'violet',
  DSA: 'cyan',
  StudyPlan: 'emerald',
  WeakTopic: 'rose',
  Analytics: 'blue',
} as const
