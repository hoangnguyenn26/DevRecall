import type { PracticeModule } from './practice.types'

export const practiceModuleMeta: Record<PracticeModule, { label: string; returnLabel: string }> = {
  Review: { label: 'Review', returnLabel: 'Return to Review' },
  Interview: { label: 'Interview practice', returnLabel: 'Return to Question Bank' },
  Dsa: { label: 'DSA practice', returnLabel: 'Return to Problem Bank' },
  StudySession: { label: 'Study session', returnLabel: 'View Session Summary' },
}
