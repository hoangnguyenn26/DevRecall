export type StudyPlanStatus = 'Draft' | 'Ready' | 'Converted' | 'Cancelled' | string
export type StudyPlanResourceType = 'KnowledgeNode' | 'InterviewQuestion' | 'DsaProblem' | 'LearningContent'

export interface StudyPlanListItem {
  studyPlanId: string
  title: string
  status: StudyPlanStatus
  itemCount: number
  totalPlannedDurationMinutes: number
  generatedAtUtc: string
  updatedAtUtc: string
  version: number
  convertedStudySessionId?: string
}

export interface StudyPlanItem {
  itemId: string
  sourceRecommendationId?: string
  sourceType: string
  resourceType: StudyPlanResourceType
  resourceId: string
  resourceTitle: string
  resourcePreview?: string
  isResourceAvailable: boolean
  plannedDurationMinutes: number
  position: number
  resourceKey?: string
  contentType?: 'Lesson' | 'ExternalResource'
  sourceName?: string
  resourceKind?: string
}

export interface StudyPlanDetail extends StudyPlanListItem {
  createdAtUtc: string
  items: StudyPlanItem[]
}

export type StudyPlanEditItem = StudyPlanItem
export interface StudyPlanEditState {
  title: string
  items: StudyPlanEditItem[]
  expectedVersion: number
}
