<script setup lang="ts">
import { BarChart, LineChart, PieChart } from 'echarts/charts'
import { AriaComponent, GridComponent, TooltipComponent } from 'echarts/components'
import { init, use, type EChartsCoreOption, type EChartsType } from 'echarts/core'
import { CanvasRenderer } from 'echarts/renderers'

use([LineChart, BarChart, PieChart, GridComponent, TooltipComponent, AriaComponent, CanvasRenderer])
const props = defineProps<{ title: string; summary: string; metric?: string; option: EChartsCoreOption }>()
const host = useTemplateRef<HTMLDivElement>('host'); let chart: EChartsType | undefined; let observer: ResizeObserver | undefined
onMounted(() => { if (!host.value) return; chart = init(host.value); chart.setOption({ aria: { enabled: true, description: props.summary }, animationDuration: 180, ...props.option }); observer = new ResizeObserver(() => chart?.resize()); observer.observe(host.value) })
watch(() => props.option, value => chart?.setOption({ aria: { enabled: true, description: props.summary }, ...value }, true), { deep: true }); onBeforeUnmount(() => { observer?.disconnect(); chart?.dispose() })
</script>
<template><section class="chart-card"><header><div><h2>{{ title }}</h2><p>{{ summary }}</p></div><strong v-if="metric">{{ metric }}</strong></header><div ref="host" class="chart" role="img" :aria-label="summary" /><details><summary>View accessible data</summary><div class="table"><slot /></div></details></section></template>
<style scoped>.chart-card { padding: 1rem; border: 1px solid var(--ui-border); border-radius: .8rem; }.chart-card header { display: flex; justify-content: space-between; gap: 1rem; }.chart-card h2 { font-weight: 700; }.chart-card p { color: var(--ui-text-muted); font-size: .85rem; }.chart-card header strong { font-size: 1.5rem; }.chart { height: 22rem; margin-top: 1rem; }.chart-card details { margin-top: .5rem; color: var(--ui-text-muted); font-size: .8rem; }.table { margin-top: .7rem; color: var(--ui-text); }</style>
