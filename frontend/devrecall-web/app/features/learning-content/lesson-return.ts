export function lessonReturnTo(value: unknown): string {
  return typeof value === 'string' && (value === '/app/discover' || value === '/app/learn' || value.startsWith('/app/learn?'))
    ? value : '/app/learn'
}
export function lessonReturnLabel(value: string): string {
  return value === '/app/discover' ? 'Back to Discover' : 'Back to Learn'
}
