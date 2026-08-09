export function useGlobalShortcuts() {
  const { commandPaletteOpen, openCommandPalette, closeCommandPalette } = useCommandPalette()
  const { shortcutScope } = useShortcutScope()

  function handleKeydown(event: KeyboardEvent): void {
    if (shortcutScope.value === 'global' && (event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault()
      if (commandPaletteOpen.value) closeCommandPalette()
      else openCommandPalette()
    }
  }

  onMounted(() => window.addEventListener('keydown', handleKeydown))
  onBeforeUnmount(() => window.removeEventListener('keydown', handleKeydown))
  return { commandPaletteOpen }
}
