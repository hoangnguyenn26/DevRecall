<script setup lang="ts">
import FeatureStory from '~/features/marketing/components/FeatureStory.vue'
import HeroSystemPreview from '~/features/marketing/components/HeroSystemPreview.vue'
import MarketingContainer from '~/features/marketing/components/MarketingContainer.vue'
import MarketingCta from '~/features/marketing/components/MarketingCta.vue'
import MarketingSection from '~/features/marketing/components/MarketingSection.vue'
import ProductPreview from '~/features/marketing/components/ProductPreview.vue'
import { learningLoopSteps, productFeatures } from '~/features/marketing/marketing.constants'

definePageMeta({ layout: 'public' })
usePublicStructuredData()
usePageSeo({
  title: 'Developer Learning Workspace',
  ogTitle: 'DevRecall — Developer Learning Workspace',
  description:
    'Build your developer knowledge base, practice interview questions and DSA, review what you learn, and know what to study next.',
  path: '/',
})
</script>

<template>
  <div class="overflow-x-clip">
    <MarketingContainer>
      <section class="hero-grid grid items-center gap-14 py-20 sm:py-28 lg:grid-cols-[1.08fr_0.92fr] lg:gap-16 lg:py-32">
        <div class="max-w-3xl">
          <UBadge variant="subtle">A learning workspace for developers</UBadge>
          <h1
            class="mt-6 max-w-3xl text-5xl font-semibold leading-[1.02] tracking-[-0.055em] text-pretty sm:text-7xl"
          >
            Remember what you learn. Know what to study next.
          </h1>
          <p class="mt-7 max-w-2xl text-lg leading-8 text-muted">
            Build your knowledge base, practice interview questions and DSA, review what you learn,
            and turn weak areas into focused study plans.
          </p>
          <div class="mt-9"><MarketingCta /></div>
        </div>
        <HeroSystemPreview />
      </section>
      <div class="relative z-10"><ProductPreview /></div>

      <MarketingSection
        eyebrow="The problem"
        title="Developer learning gets fragmented."
        description="Notes live in one place. Interview preparation in another. DSA history somewhere else. Deciding what deserves attention next becomes another task."
      >
        <div class="rounded-2xl border border-default bg-elevated p-6 sm:p-10">
          <p class="max-w-3xl text-xl font-medium leading-8 sm:text-2xl">
            DevRecall connects those pieces into one learning loop—so the evidence from yesterday
            can guide the next useful action today.
          </p>
        </div>
      </MarketingSection>

      <MarketingSection
        id="how-it-works"
        eyebrow="How it works"
        title="One loop for developer learning"
        description="Capture, practice, understand, and focus. Each step leaves useful context for the next one."
      >
        <ol
          class="grid gap-px overflow-hidden rounded-2xl border border-default bg-default sm:grid-cols-2 lg:grid-cols-4"
        >
          <li v-for="step in learningLoopSteps" :key="step.number" class="bg-elevated p-6 sm:p-8">
            <span class="font-mono text-xs font-semibold text-primary">{{ step.number }}</span>
            <h3 class="mt-8 text-xl font-semibold">{{ step.label }}</h3>
            <p class="mt-3 text-sm leading-6 text-muted">{{ step.description }}</p>
          </li>
        </ol>
      </MarketingSection>

      <MarketingSection
        id="features"
        eyebrow="Product capabilities"
        title="The whole learning loop, without the hype"
        description="Every capability below reflects a workflow that exists in DevRecall today."
      >
        <div class="grid gap-4 lg:grid-cols-2">
          <FeatureStory
            v-for="feature in productFeatures"
            :key="feature.eyebrow"
            v-bind="feature"
          />
        </div>
      </MarketingSection>

      <MarketingSection
        eyebrow="Today"
        title="Open the app and know what to do next"
        description="Today brings your active Study Session, reviews, plans, weak topics, and recommendations into one action-first view."
      >
        <div
          class="grid gap-6 rounded-2xl border border-default bg-elevated p-6 sm:p-10 lg:grid-cols-[1.2fr_0.8fr] lg:items-center"
        >
          <div>
            <p class="text-xs font-semibold tracking-[0.18em] text-primary uppercase">
              Next best action
            </p>
            <h3 class="mt-4 text-2xl font-semibold sm:text-3xl">
              Continue the work already in motion.
            </h3>
            <p class="mt-4 max-w-xl leading-7 text-muted">
              Active sessions come first, followed by ready plans, due reviews, and evidence-backed
              recommendations. Today is a starting point, not a wall of charts.
            </p>
          </div>
          <div
            class="rounded-xl border border-default bg-default p-5"
            aria-label="Illustrative next action card"
          >
            <div class="flex items-center justify-between gap-4">
              <span class="text-sm font-medium">Backend interview preparation</span
              ><UBadge color="success" variant="subtle">In progress</UBadge>
            </div>
            <p class="mt-5 text-2xl font-semibold">Continue Study Session</p>
            <p class="mt-2 text-sm text-muted">2 of 4 items completed</p>
            <div class="mt-5 h-2 overflow-hidden rounded-full bg-muted">
              <div class="h-full w-1/2 rounded-full bg-primary" />
            </div>
          </div>
        </div>
      </MarketingSection>

      <section class="py-20 sm:py-28">
        <div class="rounded-3xl bg-inverted px-6 py-14 text-center text-inverted sm:px-12 sm:py-20">
          <p class="text-sm font-semibold text-primary">Keep the learning loop moving</p>
          <h2
            class="mx-auto mt-4 max-w-3xl text-3xl font-semibold tracking-tight text-pretty sm:text-5xl"
          >
            Make your learning easier to continue.
          </h2>
          <p class="mx-auto mt-5 max-w-2xl text-base leading-7 opacity-75 sm:text-lg">
            Build your developer knowledge, practice deliberately, and always know what to do next.
          </p>
          <div class="mt-9"><MarketingCta final /></div>
        </div>
      </section>
    </MarketingContainer>
  </div>
</template>

<style scoped>
.hero-grid {
  position: relative;
  background: radial-gradient(
    circle at 25% 15%,
    color-mix(in srgb, var(--ui-primary) 14%, transparent),
    transparent 42%
  );
}

.hero-grid::after {
  position: absolute;
  inset: 8% 0 auto 45%;
  z-index: -1;
  height: 60%;
  content: '';
  background-image: radial-gradient(color-mix(in srgb, var(--ui-primary) 22%, transparent) 1px, transparent 1px);
  background-size: 22px 22px;
  mask-image: linear-gradient(to bottom, black, transparent);
  opacity: 0.5;
}
</style>
