import type { Ref } from 'vue'

export function useUnsavedChangesGuard(dirty: Ref<boolean>) {
  const confirmDialog = useConfirmDialog()

  onBeforeRouteLeave(async () => {
    if (!dirty.value) return true
    return await confirmDialog.open({
      title: 'Discard unsaved changes?',
      description: 'Your changes have not been saved.',
      confirmLabel: 'Discard changes',
      tone: 'danger',
    })
  })

  function beforeUnload(event: BeforeUnloadEvent): void {
    if (!dirty.value) return
    event.preventDefault()
    event.returnValue = ''
  }

  onMounted(() => window.addEventListener('beforeunload', beforeUnload))
  onBeforeUnmount(() => window.removeEventListener('beforeunload', beforeUnload))
}
