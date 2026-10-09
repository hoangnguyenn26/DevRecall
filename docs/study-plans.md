# Study Plans

Current V2 closure and validation limits: [Core Learning OS RC](v2-release-candidate.md).

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

## Planning intent and evidence

A Study Plan item represents planned learning work. For an external resource, finishing its Session
item means the planned task is done; it does not mean the resource is globally completed or learned.
Read-now remains independent of planning. Reference-only saving is not a Study Plan responsibility.
Keep plans small, intentional and time-bounded rather than using them as permanent link collections.

| Action | Plan / Session | Content state | Learning evidence |
| --- | --- | --- | --- |
| Add resource to plan | Item planned in that owned Draft plan | Unchanged | None |
| Open resource/source | Unchanged | Unchanged | None |
| Mark study task complete | Owned Session item Completed | Unchanged | No resource evidence |
| Complete lesson | Session item can complete after evidence validation | Lesson Completed | Canonical LearningContentCompleted |

The same resource may belong to multiple plans independently. Completing a task in one Session
does not complete/remove another plan's task or membership. Picker options/membership are account-
scoped and Draft-only, never a global Planned/Completed resource status. Duplicate membership is
success-equivalent, not a learning mutation. Existing lesson-picker default selection is retained;
the chosen plan is visible and nothing is added before explicit confirmation.

The existing PostgreSQL/application rule permits only one Draft plan per account at a time.
Independent plans do not imply multiple concurrent Drafts: move the first plan to Ready/Converted
before creating the next Draft. The picker excludes that earlier non-Draft plan and checks membership
only within the new plan. This checkpoint does not relax the constraint or invent a resource-only bypass.

EstimatedMinutes may contribute to estimated plan and Session planned duration. These planning totals
never become actual StudyMinutes. Actual duration follows the existing Study Session timing rules.
Archived resources cannot be newly added/opened/converted; already materialized external Session
tasks can still be explicitly finalized or skipped. No proof-of-reading, outbound-open prerequisite,
resource timer, bookmark domain or recommendation suppression is introduced.
