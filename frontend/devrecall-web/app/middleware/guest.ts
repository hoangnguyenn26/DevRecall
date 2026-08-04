export default defineNuxtRouteMiddleware(async () => {
  if (import.meta.server) return
  const auth = useAuth()
  await auth.restoreSession()
  if (auth.isAuthenticated.value) return navigateTo('/app')
})
