<script setup lang="ts">
import { onMounted, ref } from 'vue'

import { getSystemInfo, type SystemInfo } from '@/api/systemApi'

const systemInfo = ref<SystemInfo | null>(null)
const isLoading = ref(true)
const errorMessage = ref<string | null>(null)

onMounted(async () => {
  try {
    systemInfo.value = await getSystemInfo()
  } catch (error) {
    errorMessage.value = error instanceof Error ? error.message : 'An unexpected error occurred.'
  } finally {
    isLoading.value = false
  }
})
</script>

<template>
  <main>
    <h1>DevRecall</h1>

    <p>
      Personal learning and technical interview preparation system.
    </p>

    <p v-if="isLoading">Connecting to backend...</p>

    <p v-else-if="errorMessage">{{ errorMessage }}</p>

    <dl v-else-if="systemInfo">
      <dt>Application</dt>
      <dd>{{ systemInfo.applicationName }}</dd>

      <dt>Version</dt>
      <dd>{{ systemInfo.version }}</dd>

      <dt>Environment</dt>
      <dd>{{ systemInfo.environment }}</dd>

      <dt>Server time</dt>
      <dd>{{ systemInfo.currentTimeUtc }}</dd>
    </dl>
  </main>
</template>
