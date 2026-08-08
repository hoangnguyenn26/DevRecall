import type { Ref } from 'vue'

export function usePracticeExit(options: { exitTo: string; hasUnsubmittedWork: Ref<boolean>; busy: Ref<boolean> }) {
  const confirm = useConfirmDialog()
  const approvedNavigation = ref(false)

  async function confirmLeave(): Promise<boolean> {
    if (!options.hasUnsubmittedWork.value) return true
    return await confirm.open({
      title: 'Leave this practice session?',
      description: 'Completed progress is saved, but the current unfinished item will not be submitted.',
      confirmLabel: 'Leave session',
      cancelLabel: 'Keep practicing',
      tone: 'neutral',
    })
  }

  async function exit(): Promise<void> {
    if (options.busy.value) return
    if (!await confirmLeave()) return
    approvedNavigation.value = true
    await navigateTo(options.exitTo)
  }

  onBeforeRouteLeave(async () => {
    if (approvedNavigation.value) return true
    if (options.busy.value) return false
    return await confirmLeave()
  })

  function beforeUnload(event: BeforeUnloadEvent): void {
    if (!options.hasUnsubmittedWork.value || options.busy.value) return
    event.preventDefault()
    event.returnValue = ''
  }

  onMounted(() => window.addEventListener('beforeunload', beforeUnload))
  onBeforeUnmount(() => window.removeEventListener('beforeunload', beforeUnload))

  return { exit }
}
