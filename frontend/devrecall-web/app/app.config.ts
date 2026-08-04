export default defineAppConfig({
  ui: {
    colors: {
      primary: 'indigo',
      neutral: 'slate',
      success: 'emerald',
      warning: 'amber',
      error: 'red',
      info: 'blue',
    },
    button: {
      slots: {
        base: 'font-medium transition-colors focus-visible:outline-none',
      },
    },
    card: {
      slots: {
        root: 'shadow-none ring-1 ring-default',
      },
    },
  },
})
