export interface StudySessionItem {
  id: string
  resourceType: 'KnowledgeNode' | 'InterviewQuestion' | 'DsaProblem' | string
  resourceId: string
  resourceKey?: string
  contentType?: 'Lesson' | 'ExternalResource'
  sourceName?: string
  resourceKind?: string
  resourceTitle: string
  resourcePreview?: string
  isResourceAvailable: boolean
  hasEvidence?: boolean
  plannedDurationMinutes: number
  position: number
  status: 'Pending' | 'InProgress' | 'Completed' | 'Skipped' | string
  evidence?: {
    attemptId: string
    kind: 'InterviewAttempt' | 'DsaAttempt' | string
    outcome: string
    durationSeconds: number
    timeComplexity?: string
  }
}

export interface StudySessionDetail {
  id: string
  title: string
  status: 'Planned' | 'InProgress' | 'Completed' | 'Cancelled' | string
  plannedDurationMinutes: number
  actualDurationMinutes?: number
  startedAtUtc?: string
  completedAtUtc?: string
  reflection?: string
  version: number
  currentItemId?: string
  remainingPlannedMinutes: number
  progress: {
    totalItems: number
    pendingItems: number
    inProgressItems: number
    completedItems: number
    skippedItems: number
    completionPercentage: number
  }
  items: StudySessionItem[]
}
