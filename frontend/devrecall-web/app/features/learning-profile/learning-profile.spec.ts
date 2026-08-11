import { describe, expect, it } from 'vitest'
import { draftFromProfile, isLearningProfileDraftValid, learningProfileSignature, shouldShowLearningProfileSetup } from './learning-profile'
import { learningProfileKeys } from './learning-profile.query-keys'

describe('learning profile form', () => {
  it('creates an editable empty draft for a missing profile', () => {
    expect(draftFromProfile({ isConfigured: false, targetRole: null, experienceLevel: null,
      availableMinutesPerDay: null, technologies: [], goals: [], version: null, updatedAtUtc: null })).toEqual({
      targetRole: '', experienceLevel: '', availableMinutesPerDay: 45, technologies: [], goals: [],
    })
  })

  it('compares technology and goal sets without order sensitivity', () => {
    const first = { targetRole: 'BackendDeveloper', experienceLevel: 'Junior', availableMinutesPerDay: 45,
      technologies: [{ name: 'CSharp', isPrimary: true }, { name: 'DotNet', isPrimary: false }],
      goals: ['PrepareForInterviews', 'ImproveBackendFundamentals'] }
    const reordered = { ...first, technologies: [...first.technologies].reverse(), goals: [...first.goals].reverse() }
    expect(learningProfileSignature(first)).toBe(learningProfileSignature(reordered))
    expect(learningProfileSignature(first)).not.toBe(learningProfileSignature({
      ...first, technologies: [{ name: 'CSharp', isPrimary: false }, { name: 'DotNet', isPrimary: false }],
    }))
  })

  it('requires every recommendation signal', () => {
    expect(isLearningProfileDraftValid({ targetRole: 'BackendDeveloper', experienceLevel: 'Junior',
      availableMinutesPerDay: 45, technologies: [{ name: 'CSharp', isPrimary: true }],
      goals: ['PrepareForInterviews'] })).toBe(true)
    expect(isLearningProfileDraftValid({ targetRole: '', experienceLevel: 'Junior',
      availableMinutesPerDay: 45, technologies: [], goals: [] })).toBe(false)
    expect(isLearningProfileDraftValid({ targetRole: 'BackendDeveloper', experienceLevel: 'Junior',
      availableMinutesPerDay: 47, technologies: [{ name: 'CSharp', isPrimary: true }],
      goals: ['PrepareForInterviews'] })).toBe(false)
  })

  it('shows setup only for a loaded missing profile', () => {
    const missing = { isConfigured: false, targetRole: null, experienceLevel: null,
      availableMinutesPerDay: null, technologies: [], goals: [], version: null, updatedAtUtc: null }
    expect(shouldShowLearningProfileSetup(undefined)).toBe(false)
    expect(shouldShowLearningProfileSetup(missing)).toBe(true)
    expect(shouldShowLearningProfileSetup({ ...missing, isConfigured: true })).toBe(false)
  })

  it('uses canonical shared query keys', () => {
    expect(learningProfileKeys).toEqual({ current: 'learning-profile', options: 'learning-profile-options' })
  })
})
