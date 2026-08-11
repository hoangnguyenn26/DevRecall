<script setup lang="ts">
import type { LearningProfileTechnologyGroup, LearningProfileTechnologyInput } from '~/features/learning-profile/learning-profile.types'

const props = defineProps<{ groups: LearningProfileTechnologyGroup[], modelValue: LearningProfileTechnologyInput[] }>()
const emit = defineEmits<{ 'update:modelValue': [value: LearningProfileTechnologyInput[]] }>()
const search = ref('')
const filtered = computed(() => props.groups.map(group => ({
  ...group,
  items: group.items.filter(option => option.label.toLowerCase().includes(search.value.toLowerCase())),
})).filter(group => group.items.length > 0))
function selected(value: string) { return props.modelValue.some(item => item.name === value) }
function primary(value: string) { return props.modelValue.find(item => item.name === value)?.isPrimary ?? false }
function toggle(value: string, checked: boolean) {
  emit('update:modelValue', checked
    ? [...props.modelValue, { name: value, isPrimary: false }]
    : props.modelValue.filter(item => item.name !== value))
}
function setPrimary(value: string, checked: boolean) {
  emit('update:modelValue', props.modelValue.map(item => item.name === value ? { ...item, isPrimary: checked } : item))
}
</script>

<template>
  <div class="technology-selector">
    <label class="search-label">Find a technology<input v-model="search" type="search" placeholder="Search technologies"></label>
    <div v-for="group in filtered" :key="group.name" class="technology-group">
      <h3>{{ group.name }}</h3>
      <div class="technology-list">
        <div v-for="option in group.items" :key="option.value" class="technology-row">
          <label><input type="checkbox" :checked="selected(option.value)" @change="toggle(option.value, ($event.target as HTMLInputElement).checked)"> {{ option.label }}</label>
          <label v-if="selected(option.value)" class="primary"><input type="checkbox" :checked="primary(option.value)" :disabled="!primary(option.value) && modelValue.filter(item => item.isPrimary).length >= 5" @change="setPrimary(option.value, ($event.target as HTMLInputElement).checked)"> Focus</label>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.technology-selector,.technology-group,.technology-list{display:grid;gap:.65rem}.technology-group h3{font-size:.8rem;font-weight:700;color:var(--ui-text-muted);text-transform:uppercase;letter-spacing:.05em}.search-label{display:grid;gap:.35rem;color:var(--ui-text-muted);font-size:.875rem}.search-label input{border:1px solid var(--ui-border);border-radius:.55rem;background:var(--ui-bg);padding:.65rem .75rem;color:var(--ui-text)}.technology-list{grid-template-columns:repeat(2,minmax(0,1fr))}.technology-row{display:flex;justify-content:space-between;gap:.75rem;border:1px solid var(--ui-border);border-radius:.55rem;padding:.65rem}.primary{color:var(--ui-text-muted);font-size:.8rem}@media(max-width:640px){.technology-list{grid-template-columns:1fr}}
</style>
