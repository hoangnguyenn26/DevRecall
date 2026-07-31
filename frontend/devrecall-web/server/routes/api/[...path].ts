export default defineEventHandler(async (event) => {
  const config = useRuntimeConfig(event)
  const path = getRouterParam(event, 'path') ?? ''
  const search = getRequestURL(event).search
  const target = `${config.apiInternalBaseUrl.replace(/\/$/, '')}/api/${path}${search}`
  return proxyRequest(event, target)
})
