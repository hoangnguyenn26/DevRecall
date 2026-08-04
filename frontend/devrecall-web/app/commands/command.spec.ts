import type { Router } from 'vue-router'
import { describe, expect, it, vi } from 'vitest'
import { appNavigation } from '~/navigation/app-navigation'
import { addRecentCommandId, groupCommands, searchCommands } from './command.utils'
import { createNavigationCommands } from './navigation-commands'
import type { AppCommand } from './command.types'

function command(id: string, label: string, group: AppCommand['group'], keywords: string[] = []): AppCommand {
  return { id, label, group, keywords, icon: 'i-lucide-circle', execute: vi.fn<() => void>() }
}

describe('application commands', () => {
  it('builds navigation commands from the shared navigation metadata', () => {
    const router = { push: vi.fn<(to: unknown) => Promise<void>>().mockResolvedValue() } as unknown as Router
    const commands = createNavigationCommands(router)
    expect(commands).toHaveLength(appNavigation.flatMap(section => section.items).length)
    expect(commands.find(item => item.label === 'Knowledge')?.shortcuts).toEqual(['G', 'K'])
  })

  it('navigates to the command route', async () => {
    const push = vi.fn<(to: unknown) => Promise<void>>().mockResolvedValue()
    const commands = createNavigationCommands({ push } as unknown as Router)
    await commands.find(item => item.label === 'Study Plans')!.execute()
    expect(push).toHaveBeenCalledWith('/app/study-plans')
  })

  it('matches labels and keywords with stable relevance', () => {
    const commands = [
      command('knowledge', 'Knowledge', 'Navigation', ['notes']),
      command('capture', 'Capture concept', 'Create', ['knowledge']),
    ]
    expect(searchCommands(commands, 'knowledge').map(item => item.id)).toEqual(['knowledge', 'capture'])
    expect(searchCommands(commands, 'notes').map(item => item.id)).toEqual(['knowledge'])
  })

  it('orders recent commands before the standard groups', () => {
    const commands = [command('nav', 'Today', 'Navigation'), command('create', 'Create knowledge', 'Create')]
    expect(groupCommands(commands, ['nav'], '')[0]?.label).toBe('Recent')
    expect(groupCommands(commands, [], '')[0]?.label).toBe('Create')
  })

  it('deduplicates and bounds recent command IDs', () => {
    expect(addRecentCommandId(['b', 'a', 'c', 'd', 'e'], 'a')).toEqual(['a', 'b', 'c', 'd', 'e'])
    expect(addRecentCommandId(['a', 'b', 'c', 'd', 'e'], 'f')).toEqual(['f', 'a', 'b', 'c', 'd'])
  })
})
