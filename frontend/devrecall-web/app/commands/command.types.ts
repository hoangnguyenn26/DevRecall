export type AppCommandGroup = 'Navigation' | 'Create' | 'Learning' | 'Appearance'

export interface AppCommand {
  id: string
  label: string
  description?: string
  icon: string
  group: AppCommandGroup
  keywords?: string[]
  shortcuts?: string[]
  disabled?: boolean
  execute: () => void | Promise<void>
}

export interface AppCommandSection {
  label: AppCommandGroup | 'Recent'
  commands: AppCommand[]
}
