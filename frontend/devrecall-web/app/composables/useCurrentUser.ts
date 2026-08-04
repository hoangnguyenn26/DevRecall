export function useCurrentUser() {
  const auth = useAuth()
  return computed(() => auth.user.value)
}
