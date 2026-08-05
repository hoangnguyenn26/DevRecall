import { describe, expect, it } from 'vitest'
import { capturedResourcePath, quickCaptureTypeMeta } from './quick-capture.meta'

describe('quick capture metadata', () => {
  it('supports only the three foundation resources', () => {
    expect(Object.keys(quickCaptureTypeMeta)).toEqual([
      'KnowledgeNode', 'InterviewQuestion', 'DsaProblem',
    ])
  })

  it.each([
    ['KnowledgeNode', '/app/knowledge/resource-id'],
    ['InterviewQuestion', '/app/interview/resource-id'],
    ['DsaProblem', '/app/dsa/resource-id'],
  ] as const)('maps %s to a trusted resource path', (type, expected) => {
    expect(capturedResourcePath(type, 'resource-id')).toBe(expected)
  })

  it('never exposes numeric enum values to forms', () => {
    expect(Object.keys(quickCaptureTypeMeta).every(value => Number.isNaN(Number(value)))).toBe(true)
  })
})
