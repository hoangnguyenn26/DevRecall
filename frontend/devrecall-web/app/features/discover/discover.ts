import type { LearningContentTechnology, LearningContentTopic } from '../learning-content/learning-content.types'

export const discoverKeys = { root: 'discover', current: 'discover:current' } as const
export interface DiscoverLesson {
  slug: string
  title: string
  summary: string
  difficulty: string
  estimatedMinutes: number
  technologies: LearningContentTechnology[]
  topics: LearningContentTopic[]
  reasons: DiscoverReason[]
}
export interface DiscoverReason {
  type: 'GoalMatch' | 'WeakTopicMatch' | 'PrimaryTechnologyMatch' | 'SecondaryTechnologyMatch' | 'DifficultyFit' | 'TimeFit'
  goal?: { value: string; label: string } | null
  value?: string | null
  label?: string | null
  availableMinutes?: number | null
}
export interface DiscoverResult {
  profileConfigured: boolean
  recommended: DiscoverLesson[]
  basedOnGoals: DiscoverLesson[]
  basedOnWeakTopics: DiscoverLesson[]
}
export function invalidateDiscover(): void {
  clearNuxtData(key => key === discoverKeys.root || key.startsWith(`${discoverKeys.root}:`))
}
export function discoverReasons(item: DiscoverLesson): string[] {
  return item.reasons.slice(0, 2).map(reason => {
    switch (reason.type) {
      case 'WeakTopicMatch': return `Strengthens: ${reason.label}`
      case 'GoalMatch': return `Matches your goal: ${reason.goal?.label}`
      case 'PrimaryTechnologyMatch': return `Matches your focus: ${reason.label}`
      case 'SecondaryTechnologyMatch': return `Related to: ${reason.label}`
      case 'DifficultyFit': return 'Difficulty matches your learning profile'
      case 'TimeFit': return `Fits your ${reason.availableMinutes} min/day preference`
    }
  })
}
