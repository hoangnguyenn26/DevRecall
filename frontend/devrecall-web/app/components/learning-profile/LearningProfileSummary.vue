<script setup lang="ts">
import type { LearningProfile } from '~/features/learning-profile/learning-profile.types'

const props = defineProps<{ profile: LearningProfile }>()
const primary = computed(() => props.profile.technologies.filter(item => item.isPrimary))
const other = computed(() => props.profile.technologies.filter(item => !item.isPrimary))
const updated = computed(() => props.profile.updatedAtUtc
  ? new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(props.profile.updatedAtUtc))
  : null)
</script>

<template>
  <section class="summary" aria-labelledby="learning-profile-summary-title">
    <header><div><span class="status">Configured</span><h2 id="learning-profile-summary-title">{{ profile.targetRole?.label }}</h2><p>{{ profile.experienceLevel?.label }}</p></div><span v-if="updated" class="updated">Updated {{ updated }}</span></header>
    <dl>
      <div><dt>Primary focus</dt><dd>{{ primary.length ? primary.map(item => item.label).join(' · ') : 'No primary technologies selected' }}</dd></div>
      <div v-if="other.length"><dt>Other interests</dt><dd>{{ other.map(item => item.label).join(' · ') }}</dd></div>
      <div><dt>Goals</dt><dd>{{ profile.goals.map(item => item.label).join(' · ') }}</dd></div>
      <div><dt>Study availability</dt><dd>{{ profile.availableMinutesPerDay }} min/day</dd></div>
    </dl>
  </section>
</template>

<style scoped>
.summary{border:1px solid var(--ui-border);border-radius:.85rem;background:linear-gradient(135deg,var(--ui-bg-elevated),var(--ui-bg));padding:1.2rem}.summary header{display:flex;justify-content:space-between;gap:1rem;margin-bottom:1rem}.summary h2{font-size:1.25rem;font-weight:750}.summary p,.updated,dt{color:var(--ui-text-muted)}.status{display:inline-block;margin-bottom:.35rem;border-radius:99px;background:var(--ui-color-success-100);padding:.18rem .5rem;color:var(--ui-color-success-700);font-size:.75rem;font-weight:700}.updated{font-size:.8rem}dl{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:.85rem}dt{font-size:.78rem}dd{margin-top:.15rem;font-weight:600}@media(max-width:640px){dl{grid-template-columns:1fr}.summary header{display:block}.updated{display:block;margin-top:.5rem}}
</style>
