import type { AppCommand, AppCommandGroup, AppCommandSection } from './command.types'

const defaultGroupOrder: AppCommandGroup[] = ['Create', 'Learning', 'Navigation', 'Appearance']

function relevance(command: AppCommand, query: string): number {
  const label = command.label.toLowerCase()
  if (label.startsWith(query)) return 0
  if (label.includes(query)) return 1
  if ((command.keywords ?? []).some(keyword => keyword.toLowerCase().includes(query))) return 2
  return 3
}

export function searchCommands(commands: AppCommand[], query: string): AppCommand[] {
  const normalized = query.trim().toLowerCase()
  if (!normalized) return [...commands]

  return commands
    .filter(command => [command.label, command.description ?? '', ...(command.keywords ?? [])]
      .join(' ').toLowerCase().includes(normalized))
    .sort((left, right) => relevance(left, normalized) - relevance(right, normalized)
      || left.label.localeCompare(right.label))
}

export function groupCommands(commands: AppCommand[], recentIds: readonly string[], query: string): AppCommandSection[] {
  const filtered = searchCommands(commands, query)
  if (query.trim()) {
    const groups = new Map<AppCommandGroup, AppCommand[]>()
    for (const command of filtered) groups.set(command.group, [...(groups.get(command.group) ?? []), command])
    return defaultGroupOrder.flatMap(label => groups.has(label) ? [{ label, commands: groups.get(label)! }] : [])
  }

  const byId = new Map(commands.map(command => [command.id, command]))
  const recent = recentIds.flatMap(id => byId.has(id) ? [byId.get(id)!] : [])
  const recentSet = new Set(recent.map(command => command.id))
  const sections: AppCommandSection[] = recent.length ? [{ label: 'Recent', commands: recent }] : []
  for (const label of defaultGroupOrder) {
    const grouped = commands.filter(command => command.group === label && !recentSet.has(command.id))
    if (grouped.length) sections.push({ label, commands: grouped })
  }
  return sections
}

export function addRecentCommandId(ids: readonly string[], commandId: string): string[] {
  return [commandId, ...ids.filter(id => id !== commandId)].slice(0, 5)
}
