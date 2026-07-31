import withNuxt from './.nuxt/eslint.config.mjs'

export default withNuxt({
  rules: {
    'import/newline-after-import': 'off',
    'vue/block-tag-newline': 'off',
    'vue/max-attributes-per-line': 'off',
    'vue/padding-line-between-blocks': 'off',
    'vue/singleline-html-element-content-newline': 'off',
    'vue/multi-word-component-names': 'off',
  },
})
