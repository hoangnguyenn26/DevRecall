export type ShortcutScope = 'global' | 'knowledge-workspace' | 'editor' | 'modal'

export function useShortcutScope() {
  const scope = useState<ShortcutScope>('shortcuts:scope', () => 'global')
  function setShortcutScope(value: ShortcutScope): void { scope.value = value }
  return { shortcutScope: readonly(scope), setShortcutScope }
}
