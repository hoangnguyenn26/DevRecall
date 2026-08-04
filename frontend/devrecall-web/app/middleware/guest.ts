export default defineNuxtRouteMiddleware(async () => {
  const auth = useAuth()
  try {
    await auth.restoreSession()
  } catch {
    throw createError({ statusCode: 503, statusMessage: 'Unable to verify your session.' })
  }
  if (auth.isAuthenticated.value) return navigateTo('/app')
})
