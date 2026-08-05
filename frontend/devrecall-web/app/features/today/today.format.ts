export type DayPeriod = 'morning' | 'afternoon' | 'evening'

export function getDayPeriod(date: Date): DayPeriod {
  const hour = date.getHours()
  if (hour < 12) return 'morning'
  if (hour < 18) return 'afternoon'
  return 'evening'
}

export function formatTodayMinutes(minutes: number): string {
  if (minutes < 60) return `${minutes} min`
  const hours = Math.floor(minutes / 60)
  const remainder = minutes % 60
  return remainder === 0 ? `${hours}h` : `${hours}h ${remainder}m`
}

export function buildWeeklyActivityDescription(activity: { date: string; studyMinutes: number }[]): string {
  const total = activity.reduce((sum, point) => sum + point.studyMinutes, 0)
  const activeDays = activity.filter(point => point.studyMinutes > 0).length
  return `Weekly activity: ${formatTodayMinutes(total)} across ${activeDays} active ${activeDays === 1 ? 'day' : 'days'}.`
}

export function buildWeeklyActivityInsight(activity: { date: string; studyMinutes: number }[], targetDays: number): string {
  const active = activity.filter(point => point.studyMinutes > 0)
  if (active.length === 0) return 'Your first focused activity will start this week’s progress.'
  const mostActive = active.reduce((best, point) => point.studyMinutes > best.studyMinutes ? point : best)
  const day = new Intl.DateTimeFormat(undefined, { weekday: 'long', timeZone: 'UTC' }).format(new Date(`${mostActive.date}T00:00:00Z`))
  if (active.length < targetDays) return `You studied on ${active.length} of ${targetDays} target days this week.`
  return `Your most active day was ${day} with ${formatTodayMinutes(mostActive.studyMinutes)}.`
}
