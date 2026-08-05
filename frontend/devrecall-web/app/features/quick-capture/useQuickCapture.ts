import type { QuickCaptureType } from './quick-capture.types'

let restoreFocusTo: HTMLElement | null = null

export function useQuickCapture() {
  const open = useState('quick-capture:open', () => false)
  const type = useState<QuickCaptureType | null>('quick-capture:type', () => null)

  function start(selectedType?: QuickCaptureType): void {
    if (import.meta.client) restoreFocusTo = document.activeElement as HTMLElement | null
    type.value = selectedType ?? null
    open.value = true
  }
  function close(): void {
    open.value = false
    type.value = null
    nextTick(() => restoreFocusTo?.focus())
  }

  return { open, type, start, close, selectType: (value: QuickCaptureType) => { type.value = value }, back: () => { type.value = null } }
}
