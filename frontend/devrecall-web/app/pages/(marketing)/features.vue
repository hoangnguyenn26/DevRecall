<script setup lang="ts">
import FeaturePageSection from '~/features/marketing/components/FeaturePageSection.vue'
import FeaturePreviews from '~/features/marketing/components/FeaturePreviews.vue'
import MarketingContainer from '~/features/marketing/components/MarketingContainer.vue'

definePageMeta({ layout: 'public' })
usePageSeo({
  title: 'Features — Knowledge, Practice, Study & Insights',
  description:
    'Explore DevRecall for developer knowledge management, spaced review, interview and DSA practice, focused study plans, and explainable learning insights.',
  path: '/features',
})

const sections = [
  { id: 'knowledge', label: 'Knowledge' },
  { id: 'practice', label: 'Practice' },
  { id: 'study', label: 'Study' },
  { id: 'insights', label: 'Insights' },
  { id: 'today', label: 'Today' },
] as const
</script>

<template>
  <MarketingContainer>
    <section class="py-20 sm:py-28">
      <p class="text-sm font-semibold text-primary">DevRecall features</p>
      <h1 class="mt-4 max-w-4xl text-4xl font-semibold tracking-tight text-pretty sm:text-6xl">
        A connected workspace for developer learning
      </h1>
      <p class="mt-6 max-w-3xl text-lg leading-8 text-muted">
        Notes, interview preparation, DSA practice, review, planning, and learning insights work
        better when they share the same history.
      </p>
      <UButton to="/register" size="xl" trailing-icon="i-lucide-arrow-right" class="mt-8"
        >Start learning</UButton
      >
      <nav
        class="mt-12 flex flex-wrap gap-x-5 gap-y-3 border-t border-default pt-6"
        aria-label="Feature sections"
      >
        <NuxtLink
          v-for="section in sections"
          :key="section.id"
          :to="`#${section.id}`"
          class="text-sm font-medium text-muted hover:text-primary"
          >{{ section.label }}</NuxtLink
        >
      </nav>
    </section>

    <FeaturePageSection
      id="knowledge"
      number="01"
      eyebrow="Knowledge"
      title="Build a developer knowledge base that stays useful"
      description="Capture concepts as you learn, organize them by topic and tags, and find them again with search instead of letting notes disappear into folders you rarely revisit."
      :points="['Topic hierarchy', 'Tags', 'Global search', 'Related knowledge', 'Quick Capture']"
      ><FeaturePreviews kind="knowledge"
    /></FeaturePageSection>

    <FeaturePageSection
      id="practice"
      number="02"
      eyebrow="Practice"
      title="Practice recall, explanation, and problem solving"
      description="Each practice mode keeps the semantics that matter: recall difficulty for Review, self-reflection for Interview, and attempt outcomes for DSA."
      :points="[
        'Spaced Review when knowledge becomes due',
        'Interview answers in your own words',
        'Reference-answer comparison and self-rating',
        'DSA attempts, outcomes, complexity, and reflections',
      ]"
    >
      <div>
        <FeaturePreviews kind="practice" />
        <div class="mt-6 grid gap-4 sm:grid-cols-3">
          <div>
            <h3 class="font-semibold">Review</h3>
            <p class="mt-2 text-sm leading-6 text-muted">
              Recall knowledge when it becomes due, reveal the answer, and rate how difficult recall
              felt.
            </p>
          </div>
          <div>
            <h3 class="font-semibold">Interview</h3>
            <p class="mt-2 text-sm leading-6 text-muted">
              Explain technical concepts in your own words before comparing and recording a
              self-rating.
            </p>
          </div>
          <div>
            <h3 class="font-semibold">DSA</h3>
            <p class="mt-2 text-sm leading-6 text-muted">
              Keep a history of attempts, solutions, outcomes, complexity notes, and reflections.
            </p>
          </div>
        </div>
      </div>
    </FeaturePageSection>

    <FeaturePageSection
      id="study"
      number="03"
      eyebrow="Study"
      title="Turn scattered learning into a focused session"
      description="Group learning activities into a plan, decide how much time to spend, then work through them one by one in a focused session."
      :points="[
        'Editable Draft plans',
        'Planned duration and ordered items',
        'Resume active sessions',
        'Knowledge, Interview, and DSA handoff',
        'Session summary and reflection',
      ]"
      ><FeaturePreviews kind="study"
    /></FeaturePageSection>

    <FeaturePageSection
      id="insights"
      number="04"
      eyebrow="Insights"
      title="Understand patterns without reducing learning to one score"
      description="DevRecall uses your recorded learning activity to show weak topics, explain why they need attention, and surface relevant next actions."
      :points="[
        'Weak Topics from recurring difficulty',
        'Recommendations with visible reasons',
        'Activity and performance trends',
        'Explainable cross-signal Learning Insights',
      ]"
      ><FeaturePreviews kind="insights"
    /></FeaturePageSection>

    <FeaturePageSection
      id="today"
      number="05"
      eyebrow="Today"
      title="Open DevRecall and know where to continue"
      description="Today brings your active session, reviews, study plans, weak areas, and recommendations together so the first question is always simple: what should I do next?"
      :points="[
        'Continue an active Study Session',
        'Open a ready or draft plan',
        'Start due Review',
        'Act on the highest-priority recommendation',
      ]"
      ><FeaturePreviews kind="today"
    /></FeaturePageSection>

    <section class="border-t border-default py-20 text-center sm:py-28">
      <h2 class="mx-auto max-w-3xl text-3xl font-semibold tracking-tight text-pretty sm:text-5xl">
        Build the learning system you wish you had kept from day one.
      </h2>
      <p class="mx-auto mt-5 max-w-2xl text-lg text-muted">
        Keep knowledge, practice history, learning signals, and the next focused session connected.
      </p>
      <UButton to="/register" size="xl" trailing-icon="i-lucide-arrow-right" class="mt-8"
        >Start learning</UButton
      >
    </section>
  </MarketingContainer>
</template>
