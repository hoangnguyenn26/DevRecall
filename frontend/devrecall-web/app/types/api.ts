export interface ProblemDetails {
  type?: string
  title: string
  status: number
  detail?: string
  code?: string
  traceId?: string
  errors?: Record<string, string[]>
}

export interface PagedResponse<T> {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}

export type QueryValue = string | number | boolean | null | undefined
export type Query = Record<string, QueryValue | QueryValue[]>

export class ApiError extends Error {
  constructor(public readonly problem: ProblemDetails) {
    super(problem.detail ?? problem.title)
    this.name = 'ApiError'
  }
}
