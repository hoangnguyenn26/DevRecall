import type { LearningContentReviewCandidate } from './learning-content.types'

export function availableReviewCandidates(candidates: LearningContentReviewCandidate[]) {
  return candidates.filter(candidate => !candidate.isInReview)
}

export function markReviewCandidatesAdded(candidates: LearningContentReviewCandidate[], keys: string[]) {
  const selected = new Set(keys)
  return candidates.map(candidate => selected.has(candidate.key)
    ? { ...candidate, isInReview: true }
    : candidate)
}
