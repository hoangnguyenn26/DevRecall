<script setup lang="ts">
const { state, confirm, cancel } = useConfirmDialog()

function updateOpen(open: boolean): void {
  if (!open && !state.value.pending) cancel()
}
</script>

<template>
  <UModal
    :open="state.open"
    :dismissible="!state.pending"
    :title="state.title"
    :description="state.description"
    @update:open="updateOpen"
  >
    <template #footer>
      <div class="flex w-full justify-end gap-2">
        <UButton autofocus color="neutral" variant="ghost" :disabled="state.pending" @click="cancel">{{ state.cancelLabel }}</UButton>
        <UButton
          :color="state.tone === 'danger' ? 'error' : 'primary'"
          :loading="state.pending"
          :disabled="state.pending"
          @click="confirm"
        >
          {{ state.confirmLabel }}
        </UButton>
      </div>
    </template>
  </UModal>
</template>
