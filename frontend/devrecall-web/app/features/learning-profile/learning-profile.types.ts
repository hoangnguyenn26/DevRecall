export interface LearningProfileTechnology { name: string, isPrimary: boolean }
export interface LearningProfile {
  isConfigured: boolean
  targetRole: string | null
  experienceLevel: string | null
  availableMinutesPerDay: number | null
  technologies: LearningProfileTechnology[]
  goals: string[]
  version: number | null
}
export interface LearningProfileOption { value: string, label: string, description?: string | null }
export interface LearningProfileOptions {
  targetRoles: LearningProfileOption[]
  experienceLevels: LearningProfileOption[]
  technologies: LearningProfileOption[]
  goals: LearningProfileOption[]
  studyTimeOptions: number[]
}
export interface PutLearningProfileRequest {
  targetRole: string
  experienceLevel: string
  availableMinutesPerDay: number
  technologies: LearningProfileTechnology[]
  goals: string[]
  expectedVersion: number | null
}
export interface LearningProfileDraft {
  targetRole: string
  experienceLevel: string
  availableMinutesPerDay: number
  technologies: LearningProfileTechnology[]
  goals: string[]
}
