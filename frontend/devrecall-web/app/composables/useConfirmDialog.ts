export interface ConfirmDialogOptions {
  title: string
  description: string
  confirmLabel?: string
  cancelLabel?: string
  tone?: 'neutral' | 'danger'
}

interface ConfirmDialogState extends ConfirmDialogOptions {
  open: boolean
  pending: boolean
}

type ConfirmContext = ReturnType<typeof useNuxtApp> & { _devrecallConfirmResolver?: (accepted: boolean) => void }

export function useConfirmDialog() {
  const state = useState<ConfirmDialogState>('confirm-dialog', () => ({
    open: false,
    pending: false,
    title: '',
    description: '',
    confirmLabel: 'Confirm',
    cancelLabel: 'Cancel',
    tone: 'neutral',
  }))
  const context = useNuxtApp() as ConfirmContext

  function settle(accepted: boolean): void {
    const resolver = context._devrecallConfirmResolver
    if (!resolver) return
    context._devrecallConfirmResolver = undefined
    state.value = { ...state.value, open: false, pending: false }
    resolver(accepted)
  }

  function open(options: ConfirmDialogOptions): Promise<boolean> {
    context._devrecallConfirmResolver?.(false)
    state.value = {
      ...options,
      open: true,
      pending: false,
      confirmLabel: options.confirmLabel ?? 'Confirm',
      cancelLabel: options.cancelLabel ?? 'Cancel',
      tone: options.tone ?? 'neutral',
    }
    return new Promise(resolve => { context._devrecallConfirmResolver = resolve })
  }

  function setPending(pending: boolean): void {
    state.value = { ...state.value, pending }
  }

  return { state: readonly(state), open, confirm: () => settle(true), cancel: () => settle(false), setPending }
}
