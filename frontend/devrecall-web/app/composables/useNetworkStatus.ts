export function useNetworkStatus() {
  const online = ref(true)
  function update(): void {
    online.value = navigator.onLine
  }
  onMounted(() => {
    update()
    window.addEventListener('online', update)
    window.addEventListener('offline', update)
  })
  onBeforeUnmount(() => {
    window.removeEventListener('online', update)
    window.removeEventListener('offline', update)
  })
  return { online: readonly(online) }
}
