export interface AppNavigationBadge {
  label: string
  tone?: 'neutral' | 'primary' | 'warning' | 'error'
}

export interface AppNavigationItem {
  label: string
  to: string
  icon: string
  exact?: boolean
  badgeKey?: string
  shortcuts?: string[]
}

export interface AppNavigationSection {
  label?: string
  items: AppNavigationItem[]
}
