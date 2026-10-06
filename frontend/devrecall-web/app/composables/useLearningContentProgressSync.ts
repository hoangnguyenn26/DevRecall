import { learningContentKeys } from '~/features/learning-content/learning-content.query-keys'
import { invalidateDiscover } from '~/features/discover/discover'

export function useLearningContentProgressSync() {
  const learningInvalidation = useLearningDataInvalidation()

  function afterProgressChanged(action: 'start' | 'complete'): void {
    invalidateDiscover()
    clearNuxtData(key => key.startsWith(learningContentKeys.listBase))
    clearNuxtData(learningContentKeys.inProgress)
    if (action !== 'complete') return
    clearNuxtData(key => key.startsWith('learning-content:history:'))
    learningInvalidation.afterLessonCompleted()
  }

  return { afterProgressChanged }
}
