<script setup lang="ts">
import type { AppCommand } from '~/commands/command.types'
import { groupCommands } from '~/commands/command.utils'

const open = defineModel<boolean>('open', { default: false })
const query = ref('')
const activeIndex = ref(0)
const paletteBody = useTemplateRef<HTMLElement>('paletteBody')
const route = useRoute()
const { commands } = useAppCommands()
const { recentIds, record } = useRecentCommands()
const { commandPaletteQuerySeed } = useCommandPalette()
let previousFocus: HTMLElement | null = null

const sections = computed(() => groupCommands(commands.value, recentIds.value, query.value))
const visibleCommands = computed(() => sections.value.flatMap(section => section.commands).filter(command => !command.disabled))
const activeCommand = computed(() => visibleCommands.value[activeIndex.value])

async function execute(command: AppCommand): Promise<void> {
  if (command.disabled) return
  record(command.id)
  open.value = false
  await command.execute()
}

function handleKeydown(event: KeyboardEvent): void {
  if (event.key === 'ArrowDown') {
    event.preventDefault()
    activeIndex.value = visibleCommands.value.length ? (activeIndex.value + 1) % visibleCommands.value.length : 0
  } else if (event.key === 'ArrowUp') {
    event.preventDefault()
    activeIndex.value = visibleCommands.value.length
      ? (activeIndex.value - 1 + visibleCommands.value.length) % visibleCommands.value.length
      : 0
  } else if (event.key === 'Enter' && activeCommand.value) {
    event.preventDefault()
    void execute(activeCommand.value)
  } else if (event.key === 'Escape') {
    open.value = false
  }
}

watch(query, () => { activeIndex.value = 0 })
watch(open, async (value) => {
  if (value) {
    previousFocus = document.activeElement as HTMLElement | null
    query.value = commandPaletteQuerySeed.value
    activeIndex.value = 0
    await nextTick()
    paletteBody.value?.querySelector('input')?.focus()
  } else {
    query.value = ''
    commandPaletteQuerySeed.value = ''
    await nextTick()
    previousFocus?.focus()
    previousFocus = null
  }
})
watch(() => route.fullPath, () => { open.value = false })
</script>

<template>
  <UModal v-model:open="open" title="Search and commands" description="Navigate or run an action.">
    <template #body>
      <div ref="paletteBody" class="space-y-3" @keydown="handleKeydown">
        <UInput
          v-model="query"
          icon="i-lucide-search"
          placeholder="Search commands..."
          size="lg"
          autocomplete="off"
          role="combobox"
          aria-label="Search commands"
          aria-controls="app-command-list"
          :aria-expanded="open"
          :aria-activedescendant="activeCommand ? `command-${activeCommand.id}` : undefined"
        />

        <div id="app-command-list" class="max-h-[60dvh] overflow-y-auto" role="listbox" aria-label="Available commands">
          <section v-for="section in sections" :key="section.label" class="mb-5 last:mb-0">
            <p class="mb-2 px-2 text-xs font-semibold uppercase tracking-wider text-muted">{{ section.label }}</p>
            <button
              v-for="command in section.commands"
              :id="`command-${command.id}`"
              :key="command.id"
              type="button"
              role="option"
              class="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-left hover:bg-elevated focus-visible:bg-elevated focus-visible:outline-none disabled:opacity-50"
              :class="{ 'bg-elevated': activeCommand?.id === command.id }"
              :aria-selected="activeCommand?.id === command.id"
              :disabled="command.disabled"
              @mouseenter="activeIndex = visibleCommands.findIndex(item => item.id === command.id)"
              @click="execute(command)"
            >
              <UIcon :name="command.icon" class="size-4 shrink-0" />
              <span class="min-w-0 flex-1">
                <span class="block truncate text-sm font-medium">{{ command.label }}</span>
                <span v-if="command.description" class="block truncate text-xs text-muted">{{ command.description }}</span>
              </span>
              <span v-if="command.shortcuts?.length" class="flex gap-1" aria-label="Keyboard shortcut">
                <UKbd v-for="shortcut in command.shortcuts" :key="shortcut">{{ shortcut }}</UKbd>
              </span>
            </button>
          </section>

          <FeedbackAppEmptyState
            v-if="!sections.length"
            icon="i-lucide-search-x"
            title="No commands found"
            description="Try searching for a page or action."
          />
        </div>
      </div>
    </template>
  </UModal>
</template>
