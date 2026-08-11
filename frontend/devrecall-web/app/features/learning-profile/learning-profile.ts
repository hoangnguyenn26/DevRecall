import type { LearningProfile, LearningProfileDraft } from './learning-profile.types'

export function draftFromProfile(profile: LearningProfile): LearningProfileDraft {
  return {
    targetRole: profile.targetRole ?? '',
    experienceLevel: profile.experienceLevel ?? '',
    availableMinutesPerDay: profile.availableMinutesPerDay ?? 45,
    technologies: profile.technologies.map(item => ({ ...item })),
    goals: [...profile.goals],
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
    && value.availableMinutesPerDay >= 5 && value.availableMinutesPerDay <= 480
    && value.technologies.length >= 1 && value.goals.length >= 1)
}
