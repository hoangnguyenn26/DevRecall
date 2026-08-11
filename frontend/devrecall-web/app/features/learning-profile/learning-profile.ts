import type { LearningProfile, LearningProfileDraft } from './learning-profile.types'

const studyTimeOptions = new Set([15, 30, 45, 60, 90, 120])

export function draftFromProfile(profile: LearningProfile): LearningProfileDraft {
  return {
    targetRole: profile.targetRole?.value ?? '',
    experienceLevel: profile.experienceLevel?.value ?? '',
    availableMinutesPerDay: profile.availableMinutesPerDay ?? 45,
    technologies: profile.technologies.map(item => ({ name: item.value, isPrimary: item.isPrimary })),
    goals: profile.goals.map(item => item.value),
  }
}

export function learningProfileSignature(value: LearningProfileDraft): string {
  return JSON.stringify({
    targetRole: value.targetRole,
    experienceLevel: value.experienceLevel,
    availableMinutesPerDay: value.availableMinutesPerDay,
    technologies: [...value.technologies].sort((a, b) => a.name.localeCompare(b.name)),
    goals: [...value.goals].sort(),
  })
}

export function isLearningProfileDraftValid(value: LearningProfileDraft): boolean {
  return Boolean(value.targetRole && value.experienceLevel
    && studyTimeOptions.has(value.availableMinutesPerDay)
    && value.technologies.length >= 1 && value.technologies.length <= 20
    && value.technologies.filter(item => item.isPrimary).length <= 5
    && value.goals.length >= 1 && value.goals.length <= 10)
}

export function shouldShowLearningProfileSetup(profile: LearningProfile | null | undefined): boolean {
  return profile?.isConfigured === false
}
