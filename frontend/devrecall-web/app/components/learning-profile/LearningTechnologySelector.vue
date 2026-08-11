<script setup lang="ts">
import type { LearningProfileOption, LearningProfileTechnology } from '~/features/learning-profile/learning-profile.types'

const props = defineProps<{ options: LearningProfileOption[], modelValue: LearningProfileTechnology[] }>()
const emit = defineEmits<{ 'update:modelValue': [value: LearningProfileTechnology[]] }>()
const search = ref('')
const filtered = computed(() => props.options.filter(option => option.label.toLowerCase().includes(search.value.toLowerCase())))
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
    <div class="technology-list">
      <div v-for="option in filtered" :key="option.value" class="technology-row">
        <label><input type="checkbox" :checked="selected(option.value)" @change="toggle(option.value, ($event.target as HTMLInputElement).checked)"> {{ option.label }}</label>
        <label v-if="selected(option.value)" class="primary"><input type="checkbox" :checked="primary(option.value)" @change="setPrimary(option.value, ($event.target as HTMLInputElement).checked)"> Primary</label>
      </div>
    </div>
  </div>
</template>

<style scoped>
.technology-selector,.technology-list{display:grid;gap:.65rem}.search-label{display:grid;gap:.35rem;color:var(--ui-text-muted);font-size:.875rem}.search-label input{border:1px solid var(--ui-border);border-radius:.55rem;background:var(--ui-bg);padding:.65rem .75rem;color:var(--ui-text)}.technology-list{grid-template-columns:repeat(2,minmax(0,1fr));max-height:18rem;overflow:auto}.technology-row{display:flex;justify-content:space-between;gap:.75rem;border:1px solid var(--ui-border);border-radius:.55rem;padding:.65rem}.primary{color:var(--ui-text-muted);font-size:.8rem}@media(max-width:640px){.technology-list{grid-template-columns:1fr}}
</style>
