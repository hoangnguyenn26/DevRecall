import type { Router } from 'vue-router'
import type { AppCommand } from './command.types'

export function createQuickActionCommands(router: Router = useRouter()): AppCommand[] {
  async function navigate(to: Parameters<Router['push']>[0]): Promise<void> {
    await router.push(to)
  }

  return [
    {
      id: 'create:knowledge', label: 'Create knowledge', description: 'Capture a concept or note',
      icon: 'i-lucide-file-plus-2', group: 'Create', keywords: ['new', 'knowledge', 'note', 'capture'],
      execute: () => navigate({ path: '/app/knowledge', query: { action: 'create' } }),
    },
    {
      id: 'learning:start-review', label: 'Start review', description: 'Open your due review queue',
      icon: 'i-lucide-refresh-cw', group: 'Learning', keywords: ['review', 'due', 'recall'],
      execute: () => navigate('/app/review'),
    },
    {
      id: 'create:interview-question', label: 'Create interview question', description: 'Add a question to your interview bank',
      icon: 'i-lucide-message-square-plus', group: 'Create', keywords: ['create', 'new', 'interview', 'question'],
      execute: () => navigate({ path: '/app/interview', query: { action: 'create' } }),
    },
    {
      id: 'create:dsa-problem', label: 'Add DSA problem', description: 'Add a problem to your practice bank',
      icon: 'i-lucide-file-code-2', group: 'Create', keywords: ['create', 'new', 'dsa', 'problem', 'algorithm'],
      execute: () => navigate({ path: '/app/dsa', query: { action: 'create' } }),
    },
    {
      id: 'create:recommendations', label: 'Generate recommendations', description: 'Refresh suggested learning actions',
      icon: 'i-lucide-sparkles', group: 'Create', keywords: ['create', 'recommendation', 'weak', 'suggestion'],
      execute: () => navigate({ path: '/app/recommendations', query: { action: 'generate' } }),
    },
    {
      id: 'create:study-plan', label: 'Generate study plan', description: 'Build a plan from active recommendations',
      icon: 'i-lucide-list-plus', group: 'Create', keywords: ['create', 'plan', 'recommendation', 'schedule'],
      execute: () => navigate({ path: '/app/study-plans', query: { action: 'generate' } }),
    },
  ]
}
