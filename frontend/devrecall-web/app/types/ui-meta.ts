export type UiTone = 'neutral' | 'primary' | 'info' | 'success' | 'warning' | 'error'

export interface UiStatusMeta {
  label: string
  tone: UiTone
  icon?: string
}
