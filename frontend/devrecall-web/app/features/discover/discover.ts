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
export interface DiscoverResource extends DiscoverLesson {
  resourceKind: string
  sourceName: string
}
export interface DiscoverResult {
  profileConfigured: boolean
  recommended: DiscoverLesson[]
  basedOnGoals: DiscoverLesson[]
  basedOnWeakTopics: DiscoverLesson[]
  trustedResources: DiscoverResource[]
}
export function invalidateDiscover(): void {
  clearNuxtData(key => key === discoverKeys.root || key.startsWith(`${discoverKeys.root}:`))
}
export function discoverReasons(item: DiscoverLesson, resource = false): string[] {
  return item.reasons.filter(reason => !resource || ['WeakTopicMatch', 'GoalMatch', 'PrimaryTechnologyMatch', 'SecondaryTechnologyMatch'].includes(reason.type)).slice(0, 2).map(reason => {
    switch (reason.type) {
      case 'WeakTopicMatch': return resource ? `Related to a weak topic: ${reason.label}` : `Strengthens: ${reason.label}`
      case 'GoalMatch': return `Matches your goal: ${reason.goal?.label}`
      case 'PrimaryTechnologyMatch': return `Matches your focus: ${reason.label}`
      case 'SecondaryTechnologyMatch': return `Related to: ${reason.label}`
      case 'DifficultyFit': return 'Difficulty matches your learning profile'
      case 'TimeFit': return `Fits your ${reason.availableMinutes} min/day preference`
    }
  })
}
