import type { StudyPlanResourceType, StudyPlanStatus } from './study-plan.types'

export const studyPlanStatusMeta: Record<
  string,
  { label: string; color: 'primary' | 'success' | 'neutral' | 'warning' }
> = {
  Draft: { label: 'Draft', color: 'warning' },
  Ready: { label: 'Ready', color: 'success' },
  Converted: { label: 'Converted', color: 'primary' },
  Cancelled: { label: 'Cancelled', color: 'neutral' },
}

export const studyPlanResourceMeta: Record<
  StudyPlanResourceType,
  { label: string; icon: string; route: string }
> = {
  KnowledgeNode: { label: 'Knowledge', icon: 'i-lucide-book-open', route: '/app/knowledge' },
  InterviewQuestion: {
    label: 'Interview',
    icon: 'i-lucide-messages-square',
    route: '/app/interview',
  },
  DsaProblem: { label: 'DSA', icon: 'i-lucide-code-2', route: '/app/dsa' },
}

export function getStudyPlanStatusMeta(status: StudyPlanStatus) {
  return studyPlanStatusMeta[status] ?? { label: status || 'Unknown', color: 'neutral' as const }
}
