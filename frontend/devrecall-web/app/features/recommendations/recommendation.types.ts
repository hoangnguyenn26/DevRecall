export interface RecommendationSummary {
  recommendationId: string
  resourceType: string
  resourceId: string
  resourceTitle: string
  resourcePreview?: string
  isResourceAvailable: boolean
  type: string
  priority: string
  status: string
  weaknessLevel: string
  signalCount: number
  generatedAtUtc: string
  version: number
}
export interface RecommendationDetail extends RecommendationSummary {
  reasons: { type: string; count?: number; level?: string; calculatedAtUtc?: string }[]
}
