import type { LearningFocusArea, LearningGoal, OnboardingFormState } from './onboarding.types'

export const learningGoals: { value: LearningGoal; label: string; description: string; icon: string }[] = [
  { value: 'PrepareForInterviews', label: 'Prepare for interviews', description: 'Practice explanations, technical questions and recall.', icon: 'i-lucide-messages-square' },
  { value: 'StrengthenDotNetSkills', label: 'Strengthen .NET skills', description: 'Build durable C# and backend engineering knowledge.', icon: 'i-lucide-braces' },
  { value: 'PracticeAlgorithms', label: 'Practice algorithms', description: 'Improve patterns, attempts and problem-solving recall.', icon: 'i-lucide-binary' },
  { value: 'BuildConsistentStudyHabit', label: 'Build a consistent study habit', description: 'Create a realistic rhythm you can sustain.', icon: 'i-lucide-calendar-check' },
]

export const focusAreas: { value: LearningFocusArea; label: string }[] = [
  { value: 'CSharp', label: 'C#' },
  { value: 'DotNet', label: '.NET' },
  { value: 'SystemDesign', label: 'System Design' },
  { value: 'Databases', label: 'Databases' },
  { value: 'AlgorithmsAndDataStructures', label: 'Algorithms & DSA' },
  { value: 'InterviewCommunication', label: 'Interview Skills' },
]

export const onboardingDefaults = (): OnboardingFormState => ({
  goal: null,
  focusAreas: [],
  dailyCommitmentMinutes: 30,
  weeklyTargetDays: 5,
})

export const canContinueOnboarding = (step: number, state: OnboardingFormState): boolean => {
  if (step === 2) return state.goal !== null
  if (step === 3) return state.focusAreas.length >= 1 && state.focusAreas.length <= 4
  if (step === 4) return state.dailyCommitmentMinutes >= 10 && state.dailyCommitmentMinutes <= 180
    && state.weeklyTargetDays >= 1 && state.weeklyTargetDays <= 7
  return true
}

export function toggleFocusArea(current: LearningFocusArea[], area: LearningFocusArea): LearningFocusArea[] {
  if (current.includes(area)) return current.filter(value => value !== area)
  return current.length >= 4 ? current : [...current, area]
}
