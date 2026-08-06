export type GlobalSearchResourceType = 'Knowledge' | 'InterviewQuestion' | 'DsaProblem' | string

export interface GlobalSearchHighlight {
  field: string
  text: string
}

export interface GlobalSearchResult {
  resourceId: string
  resourceType: GlobalSearchResourceType
  title: string
  summary?: string | null
  targetPath: string
  rank: number
  updatedAtUtc: string
  highlights: GlobalSearchHighlight[]
}

export interface GlobalSearchResponse {
  query: string
  results: GlobalSearchResult[]
  hasMore: boolean
}

export interface GlobalSearchGroup {
  type: GlobalSearchResourceType
  label: string
  icon: string
  results: GlobalSearchResult[]
  known: boolean
}
