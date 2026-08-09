export function resolveSafeRedirect(value: unknown, fallback = '/app'): string {
  if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//') || value.includes('\\')) return fallback

  try {
    const base = 'http://devrecall.local'
    const target = new URL(value, base)
    const isPrivateAppPath = target.pathname === '/app' || target.pathname.startsWith('/app/')
    if (target.origin !== base || !isPrivateAppPath || target.username || target.password) return fallback
    return `${target.pathname}${target.search}${target.hash}`
  } catch {
    return fallback
  }
}
