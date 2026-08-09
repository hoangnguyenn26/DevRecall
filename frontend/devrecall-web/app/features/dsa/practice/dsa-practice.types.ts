export type DsaAttemptOutcome = 'Solved' | 'PartiallySolved' | 'Failed' | 'Skipped'
export type DsaPracticePhase = 'solving' | 'reflection' | 'submitting' | 'completed'
export interface DsaPractice { problemId: string; title: string; description?: string; externalUrl?: string; difficulty: string; topics: string[]; problemVersion: number }
export interface DsaPracticeResult { id: string; dsaProblemId: string; attemptNumber: number; result: DsaAttemptOutcome; language?: string; solutionCode?: string; approach?: string; timeComplexity?: string; spaceComplexity?: string; durationMinutes: number; notes?: string; attemptedAtUtc: string; createdAtUtc: string }
