export const weakTopicLevels = {
  Critical: { description: 'Needs immediate attention' },
  High: { description: 'Needs focused practice' },
  Medium: { description: 'Worth revisiting' },
  Low: { description: 'Minor weakness signal' },
} as const

const reasonLabels: Record<string, (count: number) => string> = {
  ReviewAgain: (count) => `${count} review outcomes rated Again`,
  ReviewHard: (count) => `${count} difficult review outcomes`,
  InterviewNeedsWork: (count) => `${count} Interview attempts need work`,
  InterviewFair: (count) => `${count} Interview attempts rated Fair`,
  DsaFailed: (count) => `${count} unsuccessful DSA attempts`,
  DsaPartiallySolved: (count) => `${count} partially solved DSA attempts`,
}
export function weakTopicReason(type: string, count: number): string {
  return (
    reasonLabels[type]?.(count) ??
    `${count} recent ${type
      .replace(/([A-Z])/g, ' $1')
      .trim()
      .toLowerCase()} signals`
  )
}
export function weakTopicAction(type: string, id: string) {
  if (type === 'InterviewQuestion')
    return { label: 'Practice question', to: `/app/interview/${id}/practice` }
  if (type === 'DsaProblem') return { label: 'Practice problem', to: `/app/dsa/${id}/practice` }
  return { label: 'Open knowledge', to: `/app/knowledge/${id}` }
}
