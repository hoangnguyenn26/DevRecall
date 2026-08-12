import type { LearningContentFilters } from './learning-content.types'

export const learningContentKeys = {
  list: (filters: LearningContentFilters) => `learning-content:list:${filters.technology ?? 'all'}:${filters.difficulty ?? 'all'}:${filters.page}`,
  detail: (slug: string) => `learning-content:detail:${slug}`,
}
