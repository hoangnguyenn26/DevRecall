import type { AppNavigationItem } from './navigation.types'

export function isNavigationItemActive(item: AppNavigationItem, path: string): boolean {
  if (item.exact) return path === item.to
  return path === item.to || path.startsWith(`${item.to}/`)
}
