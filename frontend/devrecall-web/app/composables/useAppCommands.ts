import type { AppCommand } from '~/commands/command.types'
import { createNavigationCommands } from '~/commands/navigation-commands'
import { createQuickActionCommands } from '~/commands/quick-action-commands'

export function useAppCommands() {
  const colorMode = useColorMode()
  const navigationCommands = createNavigationCommands()
  const quickActionCommands = createQuickActionCommands()
  const appearanceCommands: AppCommand[] = [
    { id: 'appearance:light', label: 'Use light theme', icon: 'i-lucide-sun', group: 'Appearance', keywords: ['color'], execute: () => { colorMode.preference = 'light' } },
    { id: 'appearance:dark', label: 'Use dark theme', icon: 'i-lucide-moon', group: 'Appearance', keywords: ['color'], execute: () => { colorMode.preference = 'dark' } },
    { id: 'appearance:system', label: 'Use system theme', icon: 'i-lucide-monitor', group: 'Appearance', keywords: ['color', 'automatic'], execute: () => { colorMode.preference = 'system' } },
  ]
  const commands = computed(() => [...navigationCommands, ...quickActionCommands, ...appearanceCommands])
  return { commands }
}
