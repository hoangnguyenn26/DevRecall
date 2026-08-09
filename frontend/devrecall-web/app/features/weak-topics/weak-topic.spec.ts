import { describe, expect, it } from 'vitest'
import { weakTopicAction, weakTopicReason } from './weak-topic.meta'

describe('weak topic presentation', () => {
  it('turns persisted signals into factual explanations', () => {
    expect(weakTopicReason('ReviewAgain', 3)).toBe('3 review outcomes rated Again')
    expect(weakTopicReason('InterviewFair', 2)).toBe('2 Interview attempts rated Fair')
  })

  it('maps resource types to trusted application routes', () => {
    expect(weakTopicAction('InterviewQuestion', 'question-id').to).toBe(
      '/app/interview/question-id/practice',
    )
    expect(weakTopicAction('DsaProblem', 'problem-id').to).toBe('/app/dsa/problem-id/practice')
    expect(weakTopicAction('KnowledgeNode', 'node-id').to).toBe('/app/knowledge/node-id')
  })
})
