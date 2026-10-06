import type { AppNavigationSection } from './navigation.types'

export const appNavigation: AppNavigationSection[] = [
  {
    items: [
      { label: 'Today', to: '/app', icon: 'i-lucide-house', exact: true, shortcuts: ['G', 'T'] },
    ],
  },
  {
    label: 'Learn',
    items: [
      { label: 'Discover', to: '/app/discover', icon: 'i-lucide-compass' },
      { label: 'Learn', to: '/app/learn', icon: 'i-lucide-book-open', shortcuts: ['G', 'L'] },
      { label: 'Knowledge', to: '/app/knowledge', icon: 'i-lucide-library', shortcuts: ['G', 'K'] },
    ],
  },
  {
    label: 'Practice',
    items: [
      { label: 'Review', to: '/app/review', icon: 'i-lucide-refresh-cw', badgeKey: 'reviewsDue', shortcuts: ['G', 'R'] },
      { label: 'Interview', to: '/app/interview', icon: 'i-lucide-messages-square', shortcuts: ['G', 'I'] },
      { label: 'DSA', to: '/app/dsa', icon: 'i-lucide-code-2', shortcuts: ['G', 'D'] },
    ],
  },
  {
    label: 'Plan',
    items: [
      { label: 'Recommendations', to: '/app/recommendations', icon: 'i-lucide-sparkles' },
      { label: 'Study Plans', to: '/app/study-plans', icon: 'i-lucide-list-checks', badgeKey: 'activeStudyPlan' },
      { label: 'Study Sessions', to: '/app/study-sessions', icon: 'i-lucide-timer' },
    ],
  },
  {
    label: 'Insights',
    items: [
      { label: 'Weak Topics', to: '/app/weak-topics', icon: 'i-lucide-triangle-alert', badgeKey: 'criticalWeakTopics' },
      { label: 'Analytics', to: '/app/analytics', icon: 'i-lucide-chart-no-axes-combined' },
    ],
  },
  {
    label: 'System',
    items: [
      { label: 'Settings', to: '/app/settings', icon: 'i-lucide-settings' },
    ],
  },
]
