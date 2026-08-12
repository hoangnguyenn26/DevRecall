<script setup lang="ts">
import { computed, ref } from 'vue'
import { parseMarkdown } from '~/features/learning-content/learning-markdown'
import InlineMarkdown from './InlineMarkdown'

const props = defineProps<{ markdown: string }>()
const blocks = computed(() => parseMarkdown(props.markdown))
const copiedIndex = ref<number | null>(null)
const copyFailedIndex = ref<number | null>(null)

async function copyCode(code: string, index: number) {
  try {
    await navigator.clipboard.writeText(code)
    copiedIndex.value = index
    copyFailedIndex.value = null
    window.setTimeout(() => { if (copiedIndex.value === index) copiedIndex.value = null }, 1800)
  } catch {
    copyFailedIndex.value = index
  }
}
</script>

<template>
  <div class="learning-markdown">
    <template v-for="(block, index) in blocks" :key="`${block.type}-${index}`">
      <p v-if="block.type === 'paragraph'"><InlineMarkdown :text="block.text" /></p>
      <blockquote v-else-if="block.type === 'blockquote'"><InlineMarkdown :text="block.text" /></blockquote>
      <component :is="`h${block.level}`" v-else-if="block.type === 'heading'">
        <InlineMarkdown :text="block.text" />
      </component>
      <component :is="block.ordered ? 'ol' : 'ul'" v-else-if="block.type === 'list'">
        <li v-for="(item, itemIndex) in block.items" :key="itemIndex">
          <InlineMarkdown :text="item" />
        </li>
      </component>
      <div v-else-if="block.type === 'code'" class="code-block">
        <div class="code-toolbar">
          <span>{{ block.language || 'code' }}</span>
          <button type="button" :aria-label="`Copy ${block.language || 'code'} example`" @click="copyCode(block.code, index)">
            {{ copiedIndex === index ? 'Copied' : copyFailedIndex === index ? "Couldn't copy" : 'Copy' }}
          </button>
        </div>
        <pre tabindex="0"><code>{{ block.code }}</code></pre>
      </div>
    </template>
  </div>
</template>

<style scoped>
.learning-markdown{display:grid;gap:1rem;color:var(--ui-text);line-height:1.72;overflow-wrap:anywhere}.learning-markdown :deep(a){color:var(--ui-primary);text-decoration:underline;text-underline-offset:.2em}.learning-markdown :deep(.inline-code){border-radius:.3rem;background:var(--ui-bg-muted);padding:.12rem .32rem;font-family:var(--font-mono,ui-monospace,monospace);font-size:.9em}.learning-markdown h3,.learning-markdown h4,.learning-markdown h5,.learning-markdown h6{margin-top:.6rem;font-weight:700;line-height:1.3}.learning-markdown h3{font-size:1.2rem}.learning-markdown ul,.learning-markdown ol{display:grid;gap:.45rem;padding-left:1.4rem}.learning-markdown ul{list-style:disc}.learning-markdown ol{list-style:decimal}.learning-markdown blockquote{border-left:3px solid var(--ui-primary);padding:.25rem 0 .25rem 1rem;color:var(--ui-text-muted)}.code-block{min-width:0;overflow:hidden;border:1px solid var(--ui-border);border-radius:.75rem;background:var(--ui-bg-muted)}.code-toolbar{display:flex;align-items:center;justify-content:space-between;border-bottom:1px solid var(--ui-border);padding:.45rem .75rem;color:var(--ui-text-muted);font-size:.75rem;text-transform:uppercase}.code-toolbar button{border-radius:.35rem;padding:.25rem .45rem;text-transform:none}.code-toolbar button:hover,.code-toolbar button:focus-visible{background:var(--ui-bg-elevated);color:var(--ui-text)}pre{max-width:100%;overflow-x:auto;padding:1rem;font-family:var(--font-mono,ui-monospace,monospace);font-size:.875rem;line-height:1.65;tab-size:2}code{white-space:pre}
</style>
