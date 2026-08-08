<script setup lang="ts">
import type { ReviewRating, ReviewResult } from '../review.types'
import PracticeCompletion from '../../practice/components/PracticeCompletion.vue'
const props = defineProps<{ results: ReviewResult[]; durationMinutes: number; remainingDueCount: number }>()
const count = (rating: ReviewRating) => props.results.filter(result => result.evaluation === rating).length
const summary = computed(() => ({ title: 'Review complete', completedCount: props.results.length, durationMinutes: props.durationMinutes, primaryMetricLabel: 'Still due', primaryMetricValue: String(props.remainingDueCount), secondaryMetrics: (['Again', 'Hard', 'Good', 'Easy'] as ReviewRating[]).map(rating => ({ label: rating, value: String(count(rating)) })) }))
</script>
<template><PracticeCompletion :summary="summary" primary-to="/app/review" primary-label="Return to Review" /></template>
