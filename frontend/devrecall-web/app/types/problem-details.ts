export interface ApiProblemDetails {
  type?: string
  title?: string
  status?: number
  detail?: string
  instance?: string
  code?: string
  errorCode?: string
  errors?: Record<string, string[]>
  traceId?: string
}
