export interface LearningProfileValue { value: string, label: string }
export interface LearningProfileTechnology extends LearningProfileValue { isPrimary: boolean }
export interface LearningProfileTechnologyInput { name: string, isPrimary: boolean }
export interface LearningProfile {
  isConfigured: boolean
  targetRole: LearningProfileValue | null
  experienceLevel: LearningProfileValue | null
  availableMinutesPerDay: number | null
  technologies: LearningProfileTechnology[]
  goals: LearningProfileValue[]
  version: number | null
  updatedAtUtc: string | null
}
export interface LearningProfileOption { value: string, label: string, description?: string | null }
export interface LearningProfileTechnologyGroup { name: string, items: LearningProfileOption[] }
export interface LearningProfileOptions {
  targetRoles: LearningProfileOption[]
  experienceLevels: LearningProfileOption[]
  technologyGroups: LearningProfileTechnologyGroup[]
  goals: LearningProfileOption[]
  studyTimeOptions: number[]
}
export interface PutLearningProfileRequest {
  targetRole: string
  experienceLevel: string
  availableMinutesPerDay: number
  technologies: LearningProfileTechnologyInput[]
  goals: string[]
  expectedVersion: number | null
}
export interface LearningProfileDraft {
  targetRole: string
  experienceLevel: string
  availableMinutesPerDay: number
  technologies: LearningProfileTechnologyInput[]
  goals: string[]
}
