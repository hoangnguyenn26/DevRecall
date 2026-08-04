import { addRecentCommandId } from '~/commands/command.utils'

export function useRecentCommands() {
  const recentIds = useCookie<string[]>('devrecall-recent-commands', { default: () => [], sameSite: 'lax' })
  function record(commandId: string): void {
    recentIds.value = addRecentCommandId(recentIds.value, commandId)
  }
  return { recentIds: readonly(recentIds), record }
}
