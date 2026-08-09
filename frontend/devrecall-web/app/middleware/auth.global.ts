export default defineNuxtRouteMiddleware(async (to) => {
  if (!to.path.startsWith('/app')) return
  const auth = useAuth()

  try {
    await auth.restoreSession()
  } catch {
    throw createError({ statusCode: 503, statusMessage: 'Unable to verify your session.' })
  }

  if (!auth.isAuthenticated.value) {
    return navigateTo({ path: '/login', query: { returnTo: to.fullPath } })
  }
})
