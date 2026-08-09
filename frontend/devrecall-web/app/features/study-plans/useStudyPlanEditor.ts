import type { StudyPlanDetail, StudyPlanEditItem, StudyPlanEditState } from './study-plan.types'
import { computed, ref, type Ref } from 'vue'

export function createStudyPlanEditState(detail: StudyPlanDetail): StudyPlanEditState {
  return {
    title: detail.title,
    expectedVersion: detail.version,
    items: detail.items.map((item) => ({ ...item })),
  }
}

export function normalizePositions(items: StudyPlanEditItem[]): StudyPlanEditItem[] {
  return items.map((item, index) => ({ ...item, position: index + 1 }))
}

function cloneState(state: StudyPlanEditState): StudyPlanEditState {
  return {
    title: state.title,
    expectedVersion: state.expectedVersion,
    items: state.items.map((item) => ({ ...item })),
  }
}

function comparable(state: StudyPlanEditState) {
  return {
    title: state.title.trim(),
    items: state.items.map((item, index) => ({
      itemId: item.itemId,
      resourceType: item.resourceType,
      resourceId: item.resourceId,
      position: index + 1,
      plannedDurationMinutes: item.plannedDurationMinutes,
    })),
  }
}

export function useStudyPlanEditor(detail: Ref<StudyPlanDetail>) {
  const form = ref(createStudyPlanEditState(detail.value))
  const initial = ref(cloneState(form.value))
  const dirty = computed(
    () => JSON.stringify(comparable(form.value)) !== JSON.stringify(comparable(initial.value)),
  )
  const totalMinutes = computed(() =>
    form.value.items.reduce((sum, item) => sum + item.plannedDurationMinutes, 0),
  )
  const valid = computed(
    () =>
      !!form.value.title.trim() &&
      form.value.items.every(
        (item) => item.plannedDurationMinutes >= 5 && item.plannedDurationMinutes <= 180,
      ) &&
      totalMinutes.value <= 480,
  )

  function move(index: number, offset: number): void {
    const target = index + offset
    if (target < 0 || target >= form.value.items.length) return
    const items = [...form.value.items]
    ;[items[index], items[target]] = [items[target]!, items[index]!]
    form.value.items = normalizePositions(items)
  }
  function remove(itemId: string): void {
    form.value.items = normalizePositions(form.value.items.filter((item) => item.itemId !== itemId))
  }
  function add(item: StudyPlanEditItem): void {
    if (
      form.value.items.some(
        (current) =>
          current.resourceType === item.resourceType && current.resourceId === item.resourceId,
      )
    )
      return
    form.value.items = normalizePositions([...form.value.items, item])
  }
  function reset(): void {
    form.value = cloneState(initial.value)
  }
  function acceptSaved(saved: StudyPlanDetail): void {
    form.value = createStudyPlanEditState(saved)
    initial.value = cloneState(form.value)
  }

  return { form, dirty, valid, totalMinutes, move, remove, add, reset, acceptSaved }
}
