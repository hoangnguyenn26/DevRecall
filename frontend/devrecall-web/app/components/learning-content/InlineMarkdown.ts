import { defineComponent, h } from 'vue'
import { parseInlineMarkdown } from '~/features/learning-content/learning-markdown'

export default defineComponent({
  name: 'InlineMarkdown',
  props: { text: { type: String, required: true } },
  setup(props) {
    return () => h('span', parseInlineMarkdown(props.text).map((token) => {
      if (token.type === 'strong') return h('strong', token.text)
      if (token.type === 'emphasis') return h('em', token.text)
      if (token.type === 'code') return h('code', { class: 'inline-code' }, token.text)
      if (token.type === 'link' && token.safe) {
        const external = /^https?:/.test(token.url)
        return h('a', { href: token.url, ...(external
          ? { target: '_blank', rel: 'noopener noreferrer' } : {}) }, token.text)
      }
      return token.text
    }))
  },
})
