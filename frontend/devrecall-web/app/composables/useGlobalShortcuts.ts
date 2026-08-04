export function useGlobalShortcuts() {
  const { commandPaletteOpen, openCommandPalette, closeCommandPalette } = useCommandPalette()

  function handleKeydown(event: KeyboardEvent): void {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') {
      event.preventDefault()
      if (commandPaletteOpen.value) closeCommandPalette()
      else openCommandPalette()
    }
  }

  onMounted(() => window.addEventListener('keydown', handleKeydown))
  onBeforeUnmount(() => window.removeEventListener('keydown', handleKeydown))
  return { commandPaletteOpen }
}
