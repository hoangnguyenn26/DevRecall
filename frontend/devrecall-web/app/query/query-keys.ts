export const queryKeys = {
  today: 'today-dashboard',
  navigationIndicators: 'navigation-indicators',
  knowledgeListBase: 'knowledge:list',
  knowledgeList: (canonicalFilters: string) => `knowledge:list:${canonicalFilters}`,
  knowledgeTopics: 'knowledge:topic-tree',
  knowledgeTags: (normalizedQuery = '') => `knowledge:tags:${normalizedQuery.trim().toLowerCase()}`,
  interviewList: 'interview:list',
  dsaList: 'dsa:list',
  reviewDue: 'review:due',
} as const
