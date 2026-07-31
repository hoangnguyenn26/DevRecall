export default defineNuxtRouteMiddleware(async (to) => {
  if (!to.path.startsWith('/app') || import.meta.server) return
  const auth = useAuthStore()
  await auth.restore()
  if (!auth.isAuthenticated) return navigateTo({ path: '/login', query: { redirect: to.fullPath } })
})
