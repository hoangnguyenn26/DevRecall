<script setup lang="ts">
import type { PagedResponse } from '~/types/api'
import type { DueReviewItem } from '~/features/review/review.types'
import ReviewOverview from '~/features/review/components/ReviewOverview.vue'
import { queryKeys } from '~/query/query-keys'
definePageMeta({ layout: 'app', middleware: 'auth' })
useSeoMeta({ title: 'Review', robots: 'noindex, nofollow' })
const api = useApi()
const { data, status, error, refresh } = await useAsyncData<PagedResponse<DueReviewItem>>(queryKeys.reviewDue, () => api.get('/review-items/due', { page: 1, pageSize: 20 }), { server: false })
</script>
<template><ReviewOverview :due-count="data?.totalCount ?? 0" :loading="status === 'pending'" :error="error" @retry="refresh" /></template>
