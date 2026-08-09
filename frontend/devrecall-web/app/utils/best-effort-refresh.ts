export async function runBestEffortRefresh(refresh: () => Promise<unknown>): Promise<boolean> {
  try {
    await refresh()
    return true
  } catch {
    return false
  }
}
