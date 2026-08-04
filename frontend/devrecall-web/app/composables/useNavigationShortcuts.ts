const navigationChordTimeoutMs = 1200
const routesByKey: Record<string, string> = {
  t: '/app', k: '/app/knowledge', r: '/app/review', i: '/app/interview', d: '/app/dsa',
}

export function useNavigationShortcuts() {
  const router = useRouter()
  const pendingPrefix = ref<string | null>(null)
  let timeoutId: ReturnType<typeof setTimeout> | undefined

  function clearPending(): void {
    pendingPrefix.value = null
    if (timeoutId) clearTimeout(timeoutId)
    timeoutId = undefined
  }

  async function handleKeydown(event: KeyboardEvent): Promise<void> {
    const element = event.target as HTMLElement | null
    if (element?.matches('input, textarea, select, [contenteditable="true"]')) return
    const key = event.key.toLowerCase()

    if (pendingPrefix.value === 'g') {
      clearPending()
      const target = routesByKey[key]
      if (target) {
        event.preventDefault()
        await router.push(target)
      }
      return
    }

    if (key === 'g' && !event.ctrlKey && !event.metaKey && !event.altKey) {
      pendingPrefix.value = 'g'
      timeoutId = setTimeout(clearPending, navigationChordTimeoutMs)
    }
  }

  onMounted(() => window.addEventListener('keydown', handleKeydown))
  onBeforeUnmount(() => {
    clearPending()
    window.removeEventListener('keydown', handleKeydown)
  })
  return { pendingPrefix: readonly(pendingPrefix) }
}
