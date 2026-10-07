import type { LearningContentFilters } from './learning-content.types'

export const learningContentKeys = {
  listBase: 'learning-content:list:',
  list: (filters: LearningContentFilters) => `learning-content:list:${filters.technology ?? 'all'}:${filters.difficulty ?? 'all'}:${filters.page}:${filters.contentType ?? 'all'}`,
  detail: (slug: string) => `learning-content:detail:${slug}`,
  inProgress: 'learning-content:in-progress',
  history: (page: number) => `learning-content:history:${page}`,
}
