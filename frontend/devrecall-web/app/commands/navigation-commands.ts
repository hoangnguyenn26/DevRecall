import type { Router } from 'vue-router'
import { appNavigation } from '~/navigation/app-navigation'
import type { AppCommand } from './command.types'

export function createNavigationCommands(router: Router = useRouter()): AppCommand[] {
  return appNavigation.flatMap(section => section.items.map(item => ({
    id: `navigate:${item.to}`,
    label: item.label,
    description: section.label ? `Open ${section.label}` : 'Open workspace',
    icon: item.icon,
    group: 'Navigation' as const,
    keywords: [section.label ?? '', item.label, item.to],
    shortcuts: item.shortcuts,
    execute: async () => { await router.push(item.to) },
  })))
}
