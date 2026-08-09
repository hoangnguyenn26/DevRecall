export const interviewSelfRatingRank = { NeedsWork: 1, Fair: 2, Good: 3, Strong: 4 } as const

export const interviewSelfRatingLabel: Record<string, string> = {
  NeedsWork: 'Needs work', Fair: 'Fair', Good: 'Good', Strong: 'Strong',
}

export const dsaAttemptOutcomeLabel: Record<string, string> = {
  Solved: 'Solved', Skipped: 'Solved with help', PartiallySolved: 'Partial', Failed: 'Could not solve',
}

export function formatPracticeDuration(seconds: number): string {
  const minutes = Math.floor(seconds / 60); const remainder = seconds % 60
  return minutes ? `${minutes}m ${String(remainder).padStart(2, '0')}s` : `${remainder}s`
}

export function interviewComparison(current: { selfRating: string; followUpsAnswered: number; durationSeconds: number }, previous: { selfRating: string; followUpsAnswered: number; durationSeconds: number }): string[] {
  const currentRank = interviewSelfRatingRank[current.selfRating as keyof typeof interviewSelfRatingRank]
  const previousRank = interviewSelfRatingRank[previous.selfRating as keyof typeof interviewSelfRatingRank]
  const rating = currentRank > previousRank ? `Self-rating changed from ${interviewSelfRatingLabel[previous.selfRating]} to ${interviewSelfRatingLabel[current.selfRating]}.` : currentRank < previousRank ? `Self-rating changed from ${interviewSelfRatingLabel[previous.selfRating]} to ${interviewSelfRatingLabel[current.selfRating]}.` : 'Self-rating stayed the same.'
  const followUps = current.followUpsAnswered === previous.followUpsAnswered ? 'You answered the same number of follow-ups.' : `You answered ${Math.abs(current.followUpsAnswered - previous.followUpsAnswered)} ${current.followUpsAnswered > previous.followUpsAnswered ? 'more' : 'fewer'} follow-up${Math.abs(current.followUpsAnswered - previous.followUpsAnswered) === 1 ? '' : 's'}.`
  const difference = Math.abs(current.durationSeconds - previous.durationSeconds)
  const duration = difference === 0 ? 'Both attempts took the same time.' : `This attempt took ${formatPracticeDuration(difference)} ${current.durationSeconds < previous.durationSeconds ? 'less' : 'more'}.`
  return [rating, followUps, duration]
}
