import type { Ref } from 'vue'
import { isEditableTarget } from '~/utils/keyboard'
import type { PracticeShortcut } from './practice.types'

export function matchesPracticeShortcut(event: KeyboardEvent, keys: string[]): boolean {
  const normalized = keys.map(key => key.toLowerCase())
  const key = event.key.toLowerCase()
  const requiresModifier = normalized.includes('ctrl') || normalized.includes('meta')
  return normalized.includes(key)
    && (!requiresModifier || event.ctrlKey || event.metaKey)
    && (requiresModifier || (!event.ctrlKey && !event.metaKey && !event.altKey))
}

export function usePracticeShortcuts(shortcuts: Ref<PracticeShortcut[]>) {
  function onKeydown(event: KeyboardEvent): void {
    if (isEditableTarget(event.target) && !(event.ctrlKey || event.metaKey)) return
    const matched = shortcuts.value.find(shortcut => shortcut.enabled() && matchesPracticeShortcut(event, shortcut.keys))
    if (!matched) return
    event.preventDefault()
    void matched.execute()
  }

  onMounted(() => window.addEventListener('keydown', onKeydown))
  onBeforeUnmount(() => window.removeEventListener('keydown', onKeydown))
  return { onKeydown }
}
