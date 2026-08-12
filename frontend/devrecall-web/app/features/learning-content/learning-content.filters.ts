import type { LocationQuery, LocationQueryRaw } from 'vue-router'
import type { LearningContentFilters } from './learning-content.types'

export const learningContentTechnologies = [
  ['CSharp', 'C#'], ['DotNet', '.NET'], ['AspNetCore', 'ASP.NET Core'],
  ['EfCore', 'EF Core'], ['PostgreSql', 'PostgreSQL'],
] as const
export const learningContentDifficulties = ['Beginner', 'Intermediate', 'Advanced'] as const

export function filtersFromQuery(query: LocationQuery): LearningContentFilters {
  const technology = typeof query.technology === 'string'
    && learningContentTechnologies.some(([value]) => value === query.technology) ? query.technology : undefined
  const difficulty = typeof query.difficulty === 'string'
    && learningContentDifficulties.includes(query.difficulty as typeof learningContentDifficulties[number])
    ? query.difficulty : undefined
  const rawPage = typeof query.page === 'string' ? Number.parseInt(query.page, 10) : 1
  return { technology, difficulty, page: Number.isSafeInteger(rawPage) && rawPage > 0 ? rawPage : 1 }
}

export function queryFromFilters(filters: LearningContentFilters): LocationQueryRaw {
  return {
    ...(filters.technology ? { technology: filters.technology } : {}),
    ...(filters.difficulty ? { difficulty: filters.difficulty } : {}),
    ...(filters.page > 1 ? { page: String(filters.page) } : {}),
  }
}

export function withFilter(filters: LearningContentFilters,
  field: 'technology' | 'difficulty', value: string): LearningContentFilters {
  return { ...filters, [field]: value || undefined, page: 1 }
}
