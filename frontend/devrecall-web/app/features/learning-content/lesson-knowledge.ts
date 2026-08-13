import type { LearningContentDetail, SaveLessonToKnowledgeInput } from './learning-content.types'

export function buildLessonKnowledgeDraft(lesson: LearningContentDetail, submissionId: string): SaveLessonToKnowledgeInput {
  const takeaways = lesson.sections.filter(section => section.type === 'KeyTakeaway')
    .sort((left, right) => left.position - right.position)
    .map(section => section.bodyMarkdown.trim()).filter(Boolean)
  return {
    title: lesson.title,
    content: takeaways.length ? `## Key takeaways\n\n${takeaways.join('\n\n')}` : '',
    topicId: null,
    tagIds: [],
    submissionId,
  }
}
