import { describe, expect, it } from 'vitest'
import { draftFromProfile, isLearningProfileDraftValid, learningProfileSignature } from './learning-profile'

describe('learning profile form', () => {
  it('creates an editable empty draft for a missing profile', () => {
    expect(draftFromProfile({ isConfigured: false, targetRole: null, experienceLevel: null,
      availableMinutesPerDay: null, technologies: [], goals: [], version: null })).toEqual({
      targetRole: '', experienceLevel: '', availableMinutesPerDay: 45, technologies: [], goals: [],
    })
  })

  it('compares technology and goal sets without order sensitivity', () => {
    const first = { targetRole: 'BackendDeveloper', experienceLevel: 'Junior', availableMinutesPerDay: 45,
      technologies: [{ name: 'CSharp', isPrimary: true }, { name: 'DotNet', isPrimary: false }],
      goals: ['PrepareForInterviews', 'ImproveBackendFundamentals'] }
    const reordered = { ...first, technologies: [...first.technologies].reverse(), goals: [...first.goals].reverse() }
    expect(learningProfileSignature(first)).toBe(learningProfileSignature(reordered))
  })

  it('requires every recommendation signal', () => {
    expect(isLearningProfileDraftValid({ targetRole: 'BackendDeveloper', experienceLevel: 'Junior',
      availableMinutesPerDay: 45, technologies: [{ name: 'CSharp', isPrimary: true }],
      goals: ['PrepareForInterviews'] })).toBe(true)
    expect(isLearningProfileDraftValid({ targetRole: '', experienceLevel: 'Junior',
      availableMinutesPerDay: 45, technologies: [], goals: [] })).toBe(false)
  })
})
