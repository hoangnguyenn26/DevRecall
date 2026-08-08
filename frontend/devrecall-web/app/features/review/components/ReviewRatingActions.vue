<script setup lang="ts">
import { reviewRatingMeta, reviewRatings } from '../review.meta'
import type { ReviewRating } from '../review.types'
defineProps<{ disabled?: boolean }>()
defineEmits<{ rate: [rating: ReviewRating] }>()
</script>
<template><div class="grid w-full grid-cols-2 gap-2 sm:grid-cols-4" role="group" aria-label="How well did you recall this item?"><UButton v-for="rating in reviewRatings" :key="rating" class="min-h-14 justify-center" :color="reviewRatingMeta[rating].color" variant="soft" :disabled="disabled" :aria-label="`${rating} — ${reviewRatingMeta[rating].description} — shortcut ${reviewRatingMeta[rating].shortcut}`" @click="$emit('rate', rating)"><UKbd>{{ reviewRatingMeta[rating].shortcut }}</UKbd><span class="ml-2">{{ reviewRatingMeta[rating].label }}</span></UButton></div></template>
