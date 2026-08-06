<script setup lang="ts">
import type { AppCommand } from '~/commands/command.types'
import { groupCommands } from '~/commands/command.utils'
import { groupGlobalSearchResults } from '~/features/global-search/global-search.meta'
import type { GlobalSearchResult } from '~/features/global-search/global-search.types'
import { useGlobalSearch } from '~/features/global-search/useGlobalSearch'

const open = defineModel<boolean>('open', { default: false })
const query = ref('')
const activeIndex = ref(0)
const paletteBody = useTemplateRef<HTMLElement>('paletteBody')
const route = useRoute()
const { commands } = useAppCommands()
const { recentIds, record } = useRecentCommands()
const { commandPaletteQuerySeed } = useCommandPalette()
const globalSearch = useGlobalSearch()
const { shortcutScope, setShortcutScope } = useShortcutScope()
let previousScope: typeof shortcutScope.value = 'global'
let previousFocus: HTMLElement | null = null

const sections = computed(() => groupCommands(commands.value, recentIds.value, query.value))
const visibleCommands = computed(() => sections.value.flatMap(section => section.commands).filter(command => !command.disabled))
const resourceGroups = computed(() => groupGlobalSearchResults(globalSearch.response.value?.results.map(result => ({
  ...result,
  highlights: result.highlights.map(highlight => ({ ...highlight })),
})) ?? []))
const visibleResources = computed(() => resourceGroups.value.filter(group => group.known).flatMap(group => group.results))
const selectableItems = computed(() => [
  ...visibleCommands.value.map(command => ({ kind: 'command' as const, id: command.id, command })),
  ...visibleResources.value.map(result => ({ kind: 'resource' as const, id: `${result.resourceType}-${result.resourceId}`, result })),
])
const activeItem = computed(() => selectableItems.value[activeIndex.value])
const activeId = computed(() => activeItem.value ? `palette-${activeItem.value.kind}-${activeItem.value.id}` : undefined)

async function execute(command: AppCommand): Promise<void> {
  if (command.disabled) return
  record(command.id)
  open.value = false
  await command.execute()
}

async function openResource(result: GlobalSearchResult): Promise<void> {
  open.value = false
  await navigateTo(result.targetPath)
}

function handleKeydown(event: KeyboardEvent): void {
  if (event.key === 'ArrowDown') {
    event.preventDefault()
    activeIndex.value = selectableItems.value.length ? (activeIndex.value + 1) % selectableItems.value.length : 0
  } else if (event.key === 'ArrowUp') {
    event.preventDefault()
    activeIndex.value = selectableItems.value.length
      ? (activeIndex.value - 1 + selectableItems.value.length) % selectableItems.value.length
      : 0
  } else if (event.key === 'Enter' && activeItem.value) {
    event.preventDefault()
    if (activeItem.value.kind === 'command') void execute(activeItem.value.command)
    else void openResource(activeItem.value.result)
  } else if (event.key === 'Escape') {
    open.value = false
  }
}

watch(query, value => { activeIndex.value = 0; globalSearch.search(value) })
watch(open, async (value) => {
  if (value) {
    previousScope = shortcutScope.value
    setShortcutScope('modal')
    previousFocus = document.activeElement as HTMLElement | null
    query.value = commandPaletteQuerySeed.value
    activeIndex.value = 0
    await nextTick()
    paletteBody.value?.querySelector('input')?.focus()
  } else {
    if (shortcutScope.value === 'modal') setShortcutScope(previousScope)
    query.value = ''
    globalSearch.clear()
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
          placeholder="Search DevRecall..."
          size="lg"
          autocomplete="off"
          role="combobox"
          aria-label="Search DevRecall"
          aria-controls="app-command-list"
          :aria-expanded="open"
          :aria-activedescendant="activeId"
        />

        <div id="app-command-list" class="max-h-[60dvh] overflow-y-auto" role="listbox" aria-label="Available commands">
          <section v-for="section in sections" :key="section.label" class="mb-5 last:mb-0">
            <p class="mb-2 px-2 text-xs font-semibold uppercase tracking-wider text-muted">{{ section.label }}</p>
            <button
              v-for="command in section.commands"
              :id="`palette-command-${command.id}`"
              :key="command.id"
              type="button"
              role="option"
              class="flex w-full items-center gap-3 rounded-lg px-3 py-2 text-left hover:bg-elevated focus-visible:bg-elevated focus-visible:outline-none disabled:opacity-50"
              :class="{ 'bg-elevated': activeItem?.kind === 'command' && activeItem.id === command.id }"
              :aria-selected="activeItem?.kind === 'command' && activeItem.id === command.id"
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

          <section v-for="group in resourceGroups.filter(item => item.known)" :key="group.type" class="mb-5 last:mb-0">
            <p class="mb-2 flex items-center gap-2 px-2 text-xs font-semibold uppercase tracking-wider text-muted"><UIcon :name="group.icon" />{{ group.label }}</p>
            <button v-for="result in group.results" :id="`palette-resource-${result.resourceType}-${result.resourceId}`" :key="result.resourceId" type="button" role="option" class="flex w-full items-start gap-3 rounded-lg px-3 py-2 text-left hover:bg-elevated focus-visible:bg-elevated focus-visible:outline-none" :class="{ 'bg-elevated': activeItem?.kind === 'resource' && activeItem.id === `${result.resourceType}-${result.resourceId}` }" :aria-selected="activeItem?.kind === 'resource' && activeItem.id === `${result.resourceType}-${result.resourceId}`" @mouseenter="activeIndex = selectableItems.findIndex(item => item.kind === 'resource' && item.id === `${result.resourceType}-${result.resourceId}`)" @click="openResource(result)">
              <UIcon :name="group.icon" class="mt-0.5 size-4 shrink-0" />
              <span class="min-w-0"><span class="block truncate text-sm font-medium">{{ result.title }}</span><span v-if="result.summary" class="block line-clamp-2 text-xs text-muted">{{ result.summary }}</span></span>
            </button>
          </section>

          <NuxtLink v-if="globalSearch.response.value?.hasMore && resourceGroups.some(group => group.type === 'Knowledge')" :to="{ path: '/app/knowledge', query: { query: globalSearch.response.value.query } }" class="mb-4 flex items-center justify-between rounded-lg px-3 py-2 text-sm text-primary hover:bg-primary/5" @click="open = false"><span>View all Knowledge results</span><UIcon name="i-lucide-arrow-right" /></NuxtLink>

          <p class="sr-only" aria-live="polite">{{ globalSearch.pending.value ? 'Searching learning resources' : `${visibleResources.length} resource results` }}</p>
          <p v-if="globalSearch.error.value" class="px-3 py-2 text-xs text-error">Resource search is unavailable. Commands remain available.</p>

          <FeedbackAppEmptyState
            v-if="!sections.length && !visibleResources.length && !globalSearch.pending.value"
            icon="i-lucide-search-x"
            title="No results found"
            description="Try another page, action, or learning resource."
          />
        </div>
      </div>
    </template>
  </UModal>
</template>
