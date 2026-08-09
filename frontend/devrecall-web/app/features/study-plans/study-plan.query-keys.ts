export const studyPlanQueryKeys = {
  list: (status = 'all') => `study-plans:list:${status.toLowerCase()}`,
  detail: (id: string) => `study-plans:detail:${id}`,
}
