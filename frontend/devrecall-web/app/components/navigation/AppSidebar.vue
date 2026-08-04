<script setup lang="ts">
import { appNavigation } from '~/navigation/app-navigation'
import { isNavigationItemActive } from '~/navigation/navigation.utils'

withDefaults(defineProps<{ collapsed: boolean; showToggle?: boolean }>(), { showToggle: true })
const emit = defineEmits<{ toggle: []; navigate: [] }>()
const route = useRoute()
</script>

<template>
  <aside
    data-testid="app-sidebar"
    class="flex h-dvh shrink-0 flex-col border-r border-default bg-default transition-[width] duration-200"
    :class="collapsed ? 'w-[var(--devrecall-sidebar-collapsed)]' : 'w-[var(--devrecall-sidebar-expanded)]'"
  >
    <div class="flex h-[var(--devrecall-topbar-height)] shrink-0 items-center justify-between px-3">
      <NavigationAppLogo :compact="collapsed" />
      <UButton
        v-if="showToggle && !collapsed"
        color="neutral"
        variant="ghost"
        icon="i-lucide-panel-left-close"
        aria-label="Collapse sidebar"
        @click="emit('toggle')"
      />
    </div>

    <nav class="min-h-0 flex-1 overflow-y-auto px-2 py-3" aria-label="Application navigation">
      <div v-for="(section, sectionIndex) in appNavigation" :key="section.label ?? sectionIndex" class="mb-5">
        <p v-if="section.label && !collapsed" class="mb-2 px-2 text-[0.6875rem] font-semibold uppercase tracking-wider text-muted">
          {{ section.label }}
        </p>

        <ul class="space-y-1">
          <li v-for="item in section.items" :key="item.to">
            <UTooltip :text="item.label" :disabled="!collapsed" :content="{ side: 'right' }">
              <NuxtLink
                :to="item.to"
                class="group flex h-9 items-center rounded-lg text-sm transition-colors"
                :class="[
                  collapsed ? 'justify-center px-2' : 'gap-3 px-2.5',
                  isNavigationItemActive(item, route.path)
                    ? 'bg-elevated font-medium text-highlighted ring-1 ring-inset ring-default'
                    : 'text-muted hover:bg-elevated/70 hover:text-highlighted',
                ]"
                :aria-label="collapsed ? item.label : undefined"
                :aria-current="isNavigationItemActive(item, route.path) ? 'page' : undefined"
                @click="emit('navigate')"
              >
                <UIcon :name="item.icon" class="size-4 shrink-0" />
                <span v-if="!collapsed" class="min-w-0 flex-1 truncate">{{ item.label }}</span>
                <NavigationAppNavigationBadge v-if="!collapsed && item.badgeKey" :badge-key="item.badgeKey" />
              </NuxtLink>
            </UTooltip>
          </li>
        </ul>
      </div>
    </nav>

    <div v-if="showToggle" class="border-t border-default p-2">
      <UButton
        color="neutral"
        variant="ghost"
        block
        :icon="collapsed ? 'i-lucide-panel-left-open' : 'i-lucide-panel-left-close'"
        :label="collapsed ? undefined : 'Collapse'"
        :aria-label="collapsed ? 'Expand sidebar' : 'Collapse sidebar'"
        @click="emit('toggle')"
      />
    </div>
  </aside>
</template>
