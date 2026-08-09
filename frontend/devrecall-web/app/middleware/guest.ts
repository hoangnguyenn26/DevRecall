export default defineNuxtRouteMiddleware(async (to) => {
  const auth = useAuth()
  const { resolveAuthenticatedDestination } = useAuthNavigation()
  try {
    await auth.restoreSession()
  } catch {
    throw createError({ statusCode: 503, statusMessage: 'Unable to verify your session.' })
  }
  if (auth.isAuthenticated.value) {
    return navigateTo(await resolveAuthenticatedDestination(to.query.returnTo))
  }
})
