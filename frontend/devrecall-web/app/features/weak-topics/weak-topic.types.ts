export interface WeakTopicSummary {
  profileId: string
  resourceType: string
  resourceId: string
  resourceTitle: string
  resourcePreview?: string
  isResourceAvailable: boolean
  level: string
  signalCount: number
  calculatedAtUtc: string
}
export interface WeakTopicDetail extends WeakTopicSummary {
  reasons: { type: string; count: number }[]
  contributions: { signalType: string; occurredAtUtc: string }[]
}
