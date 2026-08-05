export type LearningGoal =
  | 'PrepareForInterviews'
  | 'StrengthenDotNetSkills'
  | 'PracticeAlgorithms'
  | 'BuildConsistentStudyHabit'

export type LearningFocusArea =
  | 'CSharp'
  | 'DotNet'
  | 'SystemDesign'
  | 'Databases'
  | 'AlgorithmsAndDataStructures'
  | 'InterviewCommunication'

export interface OnboardingStatus {
  hasCompleted: boolean
  completionType: 'Completed' | 'Skipped' | null
  goal: LearningGoal | null
  dailyCommitmentMinutes: number | null
  weeklyTargetDays: number | null
  focusAreas: LearningFocusArea[]
}

export interface CompleteOnboardingRequest {
  goal: LearningGoal
  dailyCommitmentMinutes: number
  weeklyTargetDays: number
  focusAreas: LearningFocusArea[]
}

export interface OnboardingFormState {
  goal: LearningGoal | null
  focusAreas: LearningFocusArea[]
  dailyCommitmentMinutes: number
  weeklyTargetDays: number
}
