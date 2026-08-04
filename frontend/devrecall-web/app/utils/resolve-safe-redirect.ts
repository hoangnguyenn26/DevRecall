export function resolveSafeRedirect(value: unknown, fallback = '/app'): string {
  if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//') || value.includes('\\')) return fallback

  try {
    const base = 'http://devrecall.local'
    const target = new URL(value, base)
    if (target.origin !== base || target.pathname.startsWith('/login') || target.pathname.startsWith('/register')) return fallback
    return `${target.pathname}${target.search}${target.hash}`
  } catch {
    return fallback
  }
}
