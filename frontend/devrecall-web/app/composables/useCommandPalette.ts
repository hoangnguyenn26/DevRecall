export function useCommandPalette() {
  const commandPaletteOpen = useState('command-palette:open', () => false)
  const commandPaletteQuerySeed = useState('command-palette:query-seed', () => '')

  function openCommandPalette(initialQuery = ''): void {
    commandPaletteQuerySeed.value = initialQuery
    commandPaletteOpen.value = true
  }

  function closeCommandPalette(): void {
    commandPaletteOpen.value = false
  }

  return { commandPaletteOpen, commandPaletteQuerySeed, openCommandPalette, closeCommandPalette }
}
