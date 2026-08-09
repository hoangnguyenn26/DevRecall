import { runBestEffortRefresh } from '~/utils/best-effort-refresh'

export function useBestEffortRefresh() {
  const toast = useToast()

  async function refreshAfterMutation(
    refresh: () => Promise<unknown>,
    description = 'The saved change is safe. Related views will refresh when you open them again.',
  ): Promise<boolean> {
    const refreshed = await runBestEffortRefresh(refresh)
    if (!refreshed) {
      toast.add({
        title: 'Change saved, refresh delayed',
        description,
        color: 'warning',
      })
    }
    return refreshed
  }

  return { refreshAfterMutation }
}
