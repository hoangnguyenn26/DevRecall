# Study Plans

Study Plans represent future study intent, not bookmarks or proof of learning.
See [Study Experience](study-experience.md) for lifecycle, conversion, concurrency and evidence rules.

Published Learning Content can be added through
`POST /api/v1/study-plans/{studyPlanId}/learning-content/{slug}` with `expectedVersion` and `submissionId`.
`GET /api/v1/study-plans/learning-content/{slug}/options` returns only the current user's Draft plans,
including plan-specific `alreadyContains`. Duplicate additions are success-equivalent; other-user plans
return 404. Draft/Archived sources are unavailable. Existing completed-lesson exclusion is unchanged.

Both Lessons and ExternalResources use `LearningContent` as resource type. Detail metadata distinguishes
their intent; no new resource enum or persistence model is introduced. External resources have no
global progress. In a Session they are explicitly finished as study tasks without lesson evidence,
whereas Lesson items retain the canonical evidence requirement. See [External Resources](external-resources.md).
