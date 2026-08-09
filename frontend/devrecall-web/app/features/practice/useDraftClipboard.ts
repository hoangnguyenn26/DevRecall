export function useDraftClipboard() {
  const toast = useToast()

  async function copyDraft(content: string, label: string): Promise<boolean> {
    try {
      await navigator.clipboard.writeText(content)
      toast.add({ title: `${label} copied`, color: 'success' })
      return true
    } catch {
      toast.add({ title: `Unable to copy ${label.toLowerCase()}`, description: 'Your draft is still visible and has not been cleared.', color: 'warning' })
      return false
    }
  }

  return { copyDraft }
}
