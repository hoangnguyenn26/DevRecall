export const primaryPublicCta = { label: 'Start learning', to: '/register' } as const

export const publicNavigation = [{ label: 'Features', to: '/#features' }] as const

export const learningLoopSteps = [
  {
    number: '01',
    label: 'Capture',
    description: 'Save knowledge, questions, and problems while the context is fresh.',
  },
  {
    number: '02',
    label: 'Practice',
    description: 'Review from memory, explain concepts, and solve deliberately.',
  },
  {
    number: '03',
    label: 'Understand',
    description: 'Use history and recent evidence to see recurring weak areas.',
  },
  {
    number: '04',
    label: 'Focus',
    description: 'Follow a recommendation or turn it into a focused study plan.',
  },
] as const

export const productFeatures = [
  {
    icon: 'i-lucide-library',
    eyebrow: 'Knowledge',
    title: 'Build a knowledge base you actually revisit',
    description:
      'Capture developer concepts, organize them by topic and tags, and find them quickly when you need them again.',
    points: [
      'Topic hierarchy and tags',
      'Global search and related knowledge',
      'Quick Capture from any workspace',
    ],
  },
  {
    icon: 'i-lucide-refresh-cw',
    eyebrow: 'Deliberate practice',
    title: 'Practice more than recognition',
    description:
      'Review knowledge from memory, explain interview concepts in your own words, and keep a history of DSA attempts and reflections.',
    points: [
      'Review with Again, Hard, Good, or Easy',
      'Interview answer versions and follow-ups',
      'DSA attempt history and comparison',
    ],
  },
  {
    icon: 'i-lucide-triangle-alert',
    eyebrow: 'Weak Topics',
    title: 'See where difficulty keeps repeating',
    description:
      'DevRecall combines real learning signals from reviews and practice history to surface weak topics and explain why they need attention.',
    points: ['Explainable signals', 'Recent supporting evidence', 'No opaque skill score'],
  },
  {
    icon: 'i-lucide-sparkles',
    eyebrow: 'Recommendations',
    title: 'Turn signals into a clear next action',
    description:
      'Recommendations connect weak areas to concrete learning actions, so you spend less time deciding what to study next.',
    points: [
      'Reason and priority are visible',
      'Actions link back to real learning resources',
      'Complete or dismiss when the context changes',
    ],
  },
  {
    icon: 'i-lucide-list-checks',
    eyebrow: 'Study Plans',
    title: 'Turn scattered practice into focused sessions',
    description:
      'Group related learning activities into a plan, work through them one by one, and resume where you left off.',
    points: [
      'Draft and reorder within a time budget',
      'Mark a plan ready when it is practical',
      'Convert it into a focused Study Session',
    ],
  },
  {
    icon: 'i-lucide-chart-no-axes-combined',
    eyebrow: 'Analytics',
    title: 'Track activity without pretending it is a skill score',
    description:
      'See study consistency, review outcomes, interview self-ratings, DSA attempt outcomes, and meaningful trends over time.',
    points: [
      'Readable summaries before charts',
      'Module and practice breakdowns',
      'Accessible data alternatives',
    ],
  },
] as const
