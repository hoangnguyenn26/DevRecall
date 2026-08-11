<script setup lang="ts">
import type { LearningProfile, LearningProfileDraft, LearningProfileOptions, PutLearningProfileRequest } from '~/features/learning-profile/learning-profile.types'
import { draftFromProfile, isLearningProfileDraftValid, learningProfileSignature } from '~/features/learning-profile/learning-profile'
import { useLearningProfileApi } from '~/features/learning-profile/learning-profile.api'

definePageMeta({ layout: 'app' })
useSeoMeta({ title: 'Learning Profile' })
const api = useLearningProfileApi()
const profileQuery = useApiQuery<LearningProfile>('learning-profile', api.get)
const optionsQuery = useApiQuery<LearningProfileOptions>('learning-profile-options', api.getOptions)
const draft = ref<LearningProfileDraft | null>(null)
const savedSignature = ref('')
const saved = ref(false)

watch(profileQuery.data, (profile) => {
  if (!profile || draft.value) return
  draft.value = draftFromProfile(profile)
  savedSignature.value = learningProfileSignature(draft.value)
}, { immediate: true })

const isDirty = computed(() => Boolean(draft.value && learningProfileSignature(draft.value) !== savedSignature.value))
const valid = computed(() => Boolean(draft.value && isLearningProfileDraftValid(draft.value)))
useUnsavedChangesGuard(isDirty)

const saveMutation = useApiMutation<PutLearningProfileRequest, Awaited<ReturnType<typeof api.put>>>(api.put, {
  showSuccessToast: false,
  onSuccess(response) {
    profileQuery.data.value = response
    draft.value = draftFromProfile(response)
    savedSignature.value = learningProfileSignature(draft.value)
    saved.value = true
  },
})

async function save() {
  if (!draft.value || !valid.value || !isDirty.value) return
  saved.value = false
  await saveMutation.execute({ ...draft.value, expectedVersion: profileQuery.data.value?.version ?? null }).catch(() => undefined)
}

function toggleGoal(value: string, checked: boolean) {
  if (!draft.value) return
  draft.value.goals = checked ? [...draft.value.goals, value] : draft.value.goals.filter(goal => goal !== value)
}
async function reloadLatest() {
  draft.value = null
  await profileQuery.refresh()
}
</script>

<template>
  <div class="profile-page">
    <CorePageHeader title="Learning Profile" description="Tell DevRecall what you're learning toward. This helps personalize future learning suggestions." />
    <CoreLoadingState v-if="profileQuery.isPending.value" label="Loading your learning profile" />
    <CoreErrorState v-else-if="profileQuery.error.value" :error="profileQuery.error.value" @retry="profileQuery.refresh" />
    <CoreLoadingState v-else-if="optionsQuery.isPending.value" label="Loading learning profile options" />
    <CoreErrorState v-else-if="optionsQuery.error.value" :error="optionsQuery.error.value" @retry="optionsQuery.refresh" />
    <form v-else-if="draft && optionsQuery.data.value" class="profile-form" @submit.prevent="save">
      <LearningProfileLearningProfileSummary v-if="profileQuery.data.value?.isConfigured" :profile="profileQuery.data.value" />
      <section>
        <h2>Direction</h2><p>These are self-provided signals, not an assessment of your skill.</p>
        <div class="field-grid">
          <label>Target role<select v-model="draft.targetRole"><option value="" disabled>Select a role</option><option v-for="item in optionsQuery.data.value.targetRoles" :key="item.value" :value="item.value">{{ item.label }}</option></select></label>
          <label>Current experience level<select v-model="draft.experienceLevel"><option value="" disabled>Select your level</option><option v-for="item in optionsQuery.data.value.experienceLevels" :key="item.value" :value="item.value">{{ item.label }}<template v-if="item.description"> — {{ item.description }}</template></option></select></label>
        </div>
      </section>
      <section>
        <h2>Technology focus</h2><p>Choose up to 5 technologies you're focusing on most. Other selected technologies remain secondary interests.</p>
        <LearningProfileLearningTechnologySelector v-model="draft.technologies" :groups="optionsQuery.data.value.technologyGroups" />
      </section>
      <section>
        <h2>Your learning goals</h2>
        <div class="choice-grid"><label v-for="item in optionsQuery.data.value.goals" :key="item.value"><input type="checkbox" :checked="draft.goals.includes(item.value)" @change="toggleGoal(item.value, ($event.target as HTMLInputElement).checked)"> {{ item.label }}</label></div>
      </section>
      <section>
        <h2>Daily study time</h2>
        <div class="time-options"><label v-for="minutes in optionsQuery.data.value.studyTimeOptions" :key="minutes"><input v-model="draft.availableMinutesPerDay" type="radio" :value="minutes"> {{ minutes }} min</label></div>
      </section>
      <div v-if="saveMutation.error.value" class="save-error" role="alert">
        <strong>{{ saveMutation.error.value.status === 409 ? 'Your learning profile changed in another tab.' : saveMutation.error.value.title }}</strong>
        <p>{{ saveMutation.error.value.status === 409 ? 'Reload the latest version before saving again. Your local changes are still here.' : saveMutation.error.value.detail }}</p>
        <button v-if="saveMutation.error.value.status === 409" type="button" class="text-button" @click="reloadLatest">Reload profile</button>
      </div>
      <footer><span aria-live="polite">{{ saved ? 'Saved' : isDirty ? 'Unsaved changes' : 'No changes' }}</span><UButton type="submit" :loading="saveMutation.pending.value" :disabled="!isDirty || !valid || saveMutation.pending.value">Save changes</UButton></footer>
    </form>
  </div>
</template>

<style scoped>
.profile-page{max-width:62rem}.profile-form{display:grid;gap:1rem}.profile-form section{border:1px solid var(--ui-border);border-radius:.8rem;background:var(--ui-bg-elevated);padding:1.15rem}.profile-form h2{font-weight:700;margin-bottom:.25rem}.profile-form p{color:var(--ui-text-muted);margin-bottom:1rem}.field-grid,.choice-grid{display:grid;grid-template-columns:repeat(2,minmax(0,1fr));gap:.8rem}.field-grid label{display:grid;gap:.4rem;font-weight:600}.field-grid select{border:1px solid var(--ui-border);border-radius:.55rem;background:var(--ui-bg);padding:.7rem;color:var(--ui-text)}.choice-grid label,.time-options label{border:1px solid var(--ui-border);border-radius:.55rem;padding:.7rem}.time-options{display:flex;flex-wrap:wrap;gap:.65rem}.save-error{border:1px solid var(--ui-color-error-400);border-radius:.65rem;padding:.85rem;color:var(--ui-text)}.save-error p{margin:.25rem 0}.text-button{text-decoration:underline}footer{position:sticky;bottom:1rem;display:flex;justify-content:space-between;align-items:center;border:1px solid var(--ui-border);border-radius:.7rem;background:var(--ui-bg);padding:.8rem 1rem;box-shadow:var(--ui-shadow)}@media(max-width:640px){.field-grid,.choice-grid{grid-template-columns:1fr}}
</style>
