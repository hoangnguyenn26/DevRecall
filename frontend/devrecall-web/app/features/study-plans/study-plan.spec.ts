import { describe, expect, it } from 'vitest'
import { ref } from 'vue'
import {
  createStudyPlanEditState,
  normalizePositions,
  useStudyPlanEditor,
} from './useStudyPlanEditor'
import type { StudyPlanDetail } from './study-plan.types'

const detail: StudyPlanDetail = {
  studyPlanId: 'plan',
  title: 'Plan',
  status: 'Draft',
  itemCount: 2,
  totalPlannedDurationMinutes: 30,
  generatedAtUtc: '',
  updatedAtUtc: '',
  createdAtUtc: '',
  version: 4,
  items: [
    {
      itemId: 'a',
      sourceType: 'Manual',
      resourceType: 'KnowledgeNode',
      resourceId: 'ka',
      resourceTitle: 'A',
      isResourceAvailable: true,
      plannedDurationMinutes: 15,
      position: 1,
    },
    {
      itemId: 'b',
      sourceType: 'Manual',
      resourceType: 'DsaProblem',
      resourceId: 'db',
      resourceTitle: 'B',
      isResourceAvailable: true,
      plannedDurationMinutes: 15,
      position: 2,
    },
  ],
}

describe('Study Plan editor', () => {
  it('clones query data and normalizes positions', () => {
    const state = createStudyPlanEditState(detail)
    state.items[0]!.plannedDurationMinutes = 30
    expect(detail.items[0]!.plannedDurationMinutes).toBe(15)
    expect(normalizePositions([...state.items].reverse()).map((item) => item.position)).toEqual([
      1, 2,
    ])
  })

  it('becomes clean when an item is moved back', () => {
    const editor = useStudyPlanEditor(ref(detail))
    editor.move(0, 1)
    expect(editor.dirty.value).toBe(true)
    editor.move(1, -1)
    expect(editor.dirty.value).toBe(false)
  })

  it('restores removed items on reset and accepts the latest version', () => {
    const editor = useStudyPlanEditor(ref(detail))
    editor.remove('a')
    editor.reset()
    expect(editor.form.value.items).toHaveLength(2)
    editor.acceptSaved({ ...detail, version: 5, title: 'Saved' })
    expect(editor.form.value.expectedVersion).toBe(5)
    expect(editor.dirty.value).toBe(false)
  })
})
