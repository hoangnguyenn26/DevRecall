import type { QuickCaptureType } from './quick-capture.types'

export const quickCaptureTypeMeta: Record<QuickCaptureType, { label: string; description: string; icon: string }> = {
  KnowledgeNode: { label: 'Knowledge', description: 'Capture a concept, note or explanation.', icon: 'i-lucide-book-open' },
  InterviewQuestion: { label: 'Interview question', description: 'Add a question to your practice bank.', icon: 'i-lucide-messages-square' },
  DsaProblem: { label: 'DSA problem', description: 'Track an algorithm problem for future practice.', icon: 'i-lucide-code-2' },
}

export function capturedResourcePath(type: QuickCaptureType, id: string): string {
  if (type === 'KnowledgeNode') return `/app/knowledge/${id}`
  if (type === 'InterviewQuestion') return `/app/interview/${id}`
  return `/app/dsa/${id}`
}
