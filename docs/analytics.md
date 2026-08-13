# Analytics semantics

DevRecall analytics report historical learning facts without inferring mastery or unmeasured time.

## Learning content completion

- `LearningContentCompletionEvidence` is the canonical source for lesson completion analytics.
- Opening or starting a lesson is intent, not a completed learning activity.
- One completion evidence contributes exactly one `learningContentCompletedCount`.
- A lesson completion contributes to the user's active UTC day using the same half-open date ranges as existing analytics.
- `EstimatedMinutes` is content metadata and never contributes to measured study minutes.
- Completing a Learning Content Study Session item does not create another lesson completion. Study Session actual duration can still contribute to study minutes under the existing Study semantics.

Only lessons create completion evidence in the current MVP. The UI labels the metric **Lessons completed**; it does not claim that topics were learned or mastered.

## Historical activity

Recent lesson completion activity uses the evidence title snapshot. Archiving a lesson does not remove its completion from counts or history. A current published source can link back to the lesson; an unavailable source remains visible without a route.

Summary counts query evidence directly and do not join against the current content status. Queries are owner-scoped, bounded to 7, 30, or 90-day half-open ranges, and use database-side aggregation.
