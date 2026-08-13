export interface LearningContentTechnology { value: string; label: string }
export interface LearningContentTopic { slug: string; name: string }
export interface LearningContentObjective { position: number; text: string }
export interface LearningContentSection { position: number; type: 'Explanation' | 'CodeExample' | 'KeyTakeaway'; heading: string | null; bodyMarkdown: string }
export interface LearningContentReviewCandidate { key: string; prompt: string; answer: string; isInReview: boolean }
export interface LearningContentSource { type: 'Internal' | 'External'; name: string; url: string | null }
export type LearningProgressStatus = 'NotStarted' | 'InProgress' | 'Completed'
export interface LearningContentProgress { status: LearningProgressStatus; startedAtUtc: string | null; completedAtUtc: string | null; version: number | null; completionEvidenceId?: string | null }
export interface LearningContentListItem {
  slug: string; title: string; summary: string; contentType: 'Lesson' | 'ExternalResource'
  difficulty: 'Beginner' | 'Intermediate' | 'Advanced'; estimatedMinutes: number
  technologies: LearningContentTechnology[]; topics: LearningContentTopic[]; progressStatus: LearningProgressStatus
}
export interface LearningContentDetail extends LearningContentListItem {
  id?: string
  objectives: LearningContentObjective[]; sections: LearningContentSection[]
  reviewCandidates: LearningContentReviewCandidate[]
  source: LearningContentSource; publishedAtUtc: string; progress: LearningContentProgress
}
export interface LearningContentPage {
  items: LearningContentListItem[]; page: number; pageSize: number; totalCount: number; totalPages: number
}
export interface LearningContentFilters { technology?: string; difficulty?: string; page: number }
export interface SaveLessonToKnowledgeInput {
  title: string; content: string; topicId: string | null; tagIds: string[]; submissionId: string
}
export interface SavedLessonKnowledge { id: string; title: string; alreadyExisted: boolean }
export interface LearningContentReviewBatchItem { candidateKey: string; reviewItemId: string; wasCreated: boolean }
export interface LearningContentReviewBatch { createdCount: number; existingCount: number; items: LearningContentReviewBatchItem[] }
