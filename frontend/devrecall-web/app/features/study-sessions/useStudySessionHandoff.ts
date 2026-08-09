import { useStudySessionApi } from './study-session.api'

const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i

export function useStudySessionHandoff() {
  const route = useRoute()
  const api = useStudySessionApi()
  const sessionId = computed(() =>
    typeof route.query.studySession === 'string' && guid.test(route.query.studySession)
      ? route.query.studySession
      : undefined,
  )
  const itemId = computed(() =>
    typeof route.query.studyItem === 'string' && guid.test(route.query.studyItem)
      ? route.query.studyItem
      : undefined,
  )
  const hasContext = computed(() => !!sessionId.value && !!itemId.value)
  const pending = ref(false)
  const error = ref<unknown>()
  const submissionId = ref(crypto.randomUUID())

  async function complete(evidenceId?: string): Promise<void> {
    if (!sessionId.value || !itemId.value || pending.value) return
    pending.value = true
    error.value = undefined
    try {
      const session = await api.detail(sessionId.value)
      await api.completeItem(
        sessionId.value,
        itemId.value,
        session.version,
        submissionId.value,
        evidenceId,
      )
      await navigateTo(`/app/study-sessions/${sessionId.value}`)
    } catch (cause) {
      error.value = cause
    } finally {
      pending.value = false
    }
  }
  return { sessionId, itemId, hasContext, pending, error, complete }
}
