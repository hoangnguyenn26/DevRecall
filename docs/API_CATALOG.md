# DevRecall MVP API Catalog

## 1. General conventions

Base path:

```text
/api/v1
```

General rules:

- Use JSON with camelCase properties.
- Use UUID resource identifiers.
- Use ISO 8601 date-time values.
- Store date-time values in UTC.
- Return Problem Details for errors.
- Apply authentication to all user-owned resources.
- Apply ownership checks on every resource query and mutation.
- Use offset pagination for ordinary list screens.
- Do not expose EF Core entities.

Standard list query:

```text
?page=1&pageSize=20&sortBy=updatedAt&sortDirection=desc
```

Standard paged response:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0
}
```

---

## 2. Authentication and profile

### Register

```http
POST /api/v1/auth/register
Content-Type: application/json
```

Request:

```json
{
  "email": "hoang@example.com",
  "displayName": "Hoang Nguyen",
  "password": "Example123!"
}
```

Returns `201 Created` with the new user's `id`, `email`, and `displayName`.
The response never includes the password, password hash, or normalized email.

Errors:

- `400 VALIDATION_FAILED` for missing fields, an invalid email, or a password outside 8–128 characters.
- `409 IDENTITY_EMAIL_ALREADY_EXISTS` when the normalized email is already registered.

### Account initialization

```http
POST /api/v1/auth/initialize
```

Creates the first local account when the application has not yet been initialized.

### Login

```http
POST /api/v1/auth/login
Content-Type: application/json
```

Request:

```json
{
  "email": "hoang@example.com",
  "password": "Example123!"
}
```

Returns `200 OK` with `id`, `email`, and `displayName`, and issues the
`devrecall.auth` authentication cookie. Invalid credentials return
`401 IDENTITY_INVALID_CREDENTIALS` without revealing whether the email exists.

### Logout

```http
POST /api/v1/auth/logout
```

Requires authentication. Returns `204 No Content` and removes the
`devrecall.auth` authentication cookie.

### Current user

```http
GET /api/v1/auth/me
```

Requires authentication. Returns `200 OK` with the authenticated user's
`id`, `email`, and `displayName`; anonymous requests return `401 Unauthorized`.

### Change password

```http
POST /api/v1/auth/change-password
```

### Get user preferences

```http
GET /api/v1/user-preferences
```

### Update user preferences

```http
PUT /api/v1/user-preferences
```

Representative preference fields:

- timeZone
- dailyAvailableMinutes
- dailyDsaTarget
- dailyInterviewTarget
- preferredAnswerDurationSeconds
- maximumDailyReviewItems

---

## 3. Knowledge Tree

### Get complete tree

```http
GET /api/v1/knowledge-nodes/tree
```

Requires authentication. Returns the authenticated user's active nodes as a
nested tree, ordered by title and then ID. Every leaf has `children: []`.
An account with no active root nodes receives `200 OK` with:

```json
[]
```

### Get node details

```http
GET /api/v1/knowledge-nodes/{nodeId}
```

### Create node

```http
POST /api/v1/knowledge-nodes
Content-Type: application/json
```

Requires authentication.

```json
{
  "title": "C#",
  "parentId": "uuid-or-null"
}
```

Returns `201 Created`. A missing or cross-user parent returns
`404 KNOWLEDGE_PARENT_NOT_FOUND`; an archived parent returns
`409 KNOWLEDGE_INVALID_PARENT`.

### Update node

```http
PUT /api/v1/knowledge-nodes/{nodeId}
```

Requires authentication and ownership.

```json
{
  "title": "Software Engineering"
}
```

Returns `200 OK`. Missing or cross-user nodes return
`404 KNOWLEDGE_NODE_NOT_FOUND`; archived nodes return
`409 KNOWLEDGE_NODE_ARCHIVED`.

### Move node

```http
POST /api/v1/knowledge-nodes/{nodeId}/move
```

Request:

```json
{
  "newParentId": "uuid-or-null",
  "newSortOrder": 2,
  "version": 4
}
```

Possible errors:

- `KNOWLEDGE_NODE_NOT_FOUND`
- `KNOWLEDGE_NODE_CIRCULAR_PARENT`
- `KNOWLEDGE_NODE_ARCHIVED`
- `CONCURRENCY_CONFLICT`

### Reorder siblings

```http
POST /api/v1/knowledge-nodes/reorder
```

### Archive node

```http
POST /api/v1/knowledge-nodes/{nodeId}/archive
```

Requires authentication and ownership. Returns `204 No Content`; repeated
archive requests are idempotent. Archiving changes node status without deleting
the database row.

### Restore node

```http
POST /api/v1/knowledge-nodes/{nodeId}/restore
```

### List tags

```http
GET /api/v1/tags
```

### Create tag

```http
POST /api/v1/tags
```

### Update tag

```http
PUT /api/v1/tags/{tagId}
```

### Attach tag to node

```http
POST /api/v1/knowledge-nodes/{nodeId}/tags/{tagId}
```

### Remove tag from node

```http
DELETE /api/v1/knowledge-nodes/{nodeId}/tags/{tagId}
```

---

## 4. Interview Questions

### List questions

```http
GET /api/v1/interview-questions
```

Filters:

```text
?knowledgeNodeId=
&questionType=
&difficulty=
&status=
&confidenceLevel=
&tagId=
&search=
&page=
&pageSize=
```

### Get question details

```http
GET /api/v1/interview-questions/{questionId}
```

### Create question

```http
POST /api/v1/interview-questions
```

### Update question metadata

```http
PUT /api/v1/interview-questions/{questionId}
```

### Archive question

```http
POST /api/v1/interview-questions/{questionId}/archive
```

### Restore question

```http
POST /api/v1/interview-questions/{questionId}/restore
```

### List answer versions

```http
GET /api/v1/interview-questions/{questionId}/answer-versions
```

### Get answer version

```http
GET /api/v1/interview-questions/{questionId}/answer-versions/{versionId}
```

### Create answer version

```http
POST /api/v1/interview-questions/{questionId}/answer-versions
```

Representative fields:

- shortAnswer
- standardAnswer
- deepAnswer
- internalMechanism
- whenToUse
- whenNotToUse
- commonMistakes
- tradeOffs
- codeExample
- projectExample
- language

### Publish answer version

```http
POST /api/v1/interview-questions/{questionId}/answer-versions/{versionId}/publish
```

### List follow-up questions

```http
GET /api/v1/interview-questions/{questionId}/follow-ups
```

### Add follow-up question

```http
POST /api/v1/interview-questions/{questionId}/follow-ups
```

### Update follow-up question

```http
PUT /api/v1/interview-questions/{questionId}/follow-ups/{followUpId}
```

### Delete follow-up question

```http
DELETE /api/v1/interview-questions/{questionId}/follow-ups/{followUpId}
```

---

## 5. Project Stories

### List project stories

```http
GET /api/v1/project-stories
```

### Get project story

```http
GET /api/v1/project-stories/{storyId}
```

### Create project story

```http
POST /api/v1/project-stories
```

Representative fields:

- title
- situation
- task
- action
- result
- lessonsLearned
- englishVersion
- confidenceLevel
- tags

### Update project story

```http
PUT /api/v1/project-stories/{storyId}
```

### Archive project story

```http
POST /api/v1/project-stories/{storyId}/archive
```

### Restore project story

```http
POST /api/v1/project-stories/{storyId}/restore
```

---

## 6. DSA Patterns and Problems

### List patterns

```http
GET /api/v1/dsa/patterns
```

### Create pattern

```http
POST /api/v1/dsa/patterns
```

### Update pattern

```http
PUT /api/v1/dsa/patterns/{patternId}
```

### List DSA problems

```http
GET /api/v1/dsa/problems
```

Filters:

```text
?patternId=
&difficulty=
&status=
&confidenceLevel=
&needsReview=
&search=
&page=
&pageSize=
```

### Get DSA problem

```http
GET /api/v1/dsa/problems/{problemId}
```

### Create DSA problem

```http
POST /api/v1/dsa/problems
```

### Update DSA problem

```http
PUT /api/v1/dsa/problems/{problemId}
```

### Archive DSA problem

```http
POST /api/v1/dsa/problems/{problemId}/archive
```

### Restore DSA problem

```http
POST /api/v1/dsa/problems/{problemId}/restore
```

### Get DSA problem progress

```http
GET /api/v1/dsa/problems/{problemId}/progress
```

---

## 7. DSA Attempts

### List attempts for a problem

```http
GET /api/v1/dsa/problems/{problemId}/attempts
```

### Start attempt

```http
POST /api/v1/dsa/problems/{problemId}/attempts
```

Response includes:

- attemptId
- startedAt
- status

### Get attempt

```http
GET /api/v1/dsa/attempts/{attemptId}
```

### Record hint usage

```http
POST /api/v1/dsa/attempts/{attemptId}/hints
```

Representative values:

- smallHint
- patternHint
- pseudocode
- fullSolution

### Complete attempt

```http
POST /api/v1/dsa/attempts/{attemptId}/complete
```

Representative fields:

- result
- solutionCode
- bruteForceApproach
- optimalApproach
- timeComplexity
- spaceComplexity
- confidenceLevel
- mistakes
- notes

Possible errors:

- `DSA_ATTEMPT_NOT_FOUND`
- `DSA_ATTEMPT_ALREADY_COMPLETED`
- `DSA_PROBLEM_ARCHIVED`
- `INVALID_INDEPENDENT_SOLUTION_CLASSIFICATION`

### Add solution version

```http
POST /api/v1/dsa/problems/{problemId}/solution-versions
```

### List solution versions

```http
GET /api/v1/dsa/problems/{problemId}/solution-versions
```

### Add mistake

```http
POST /api/v1/dsa/problems/{problemId}/mistakes
```

### Update mistake

```http
PUT /api/v1/dsa/problems/{problemId}/mistakes/{mistakeId}
```

### Resolve mistake

```http
POST /api/v1/dsa/problems/{problemId}/mistakes/{mistakeId}/resolve
```

---

## 8. Review Items

### Get due review queue

```http
GET /api/v1/reviews/due
```

Query:

```text
?date=
&sourceType=
&includeOverdue=true
&limit=50
```

### Get review item

```http
GET /api/v1/reviews/{reviewItemId}
```

### Enable review for source

```http
POST /api/v1/reviews
```

Request:

```json
{
  "sourceType": "interviewQuestion",
  "sourceId": "uuid",
  "reviewPolicy": "default"
}
```

### Submit review

```http
POST /api/v1/reviews/{reviewItemId}/submit
```

Request:

```json
{
  "rating": "good",
  "answer": "optional user answer",
  "durationSeconds": 52,
  "notes": "optional notes"
}
```

Response includes:

- nextReviewAt
- intervalDays
- reviewCount
- failureCount

### Get review history

```http
GET /api/v1/reviews/{reviewItemId}/attempts
```

### Suspend review

```http
POST /api/v1/reviews/{reviewItemId}/suspend
```

### Resume review

```http
POST /api/v1/reviews/{reviewItemId}/resume
```

### Archive review

```http
POST /api/v1/reviews/{reviewItemId}/archive
```

### Restore review

```http
POST /api/v1/reviews/{reviewItemId}/restore
```

### Manually reschedule review

```http
POST /api/v1/reviews/{reviewItemId}/reschedule
```

Possible errors:

- `REVIEW_ITEM_ARCHIVED`
- `REVIEW_ITEM_SUSPENDED`
- `REVIEW_SOURCE_NOT_FOUND`
- `ACTIVE_REVIEW_ALREADY_EXISTS`

---

## 9. Daily Study Plans

### Get today's study plan

```http
GET /api/v1/study-plans/today
```

### Get plan by date

```http
GET /api/v1/study-plans/{date}
```

Date format:

```text
YYYY-MM-DD
```

### Generate daily plan

```http
POST /api/v1/study-plans/generate
```

Request:

```json
{
  "date": "2026-07-15",
  "availableMinutes": 60,
  "forceRegenerate": false
}
```

This operation should support idempotency.

### Regenerate plan

```http
POST /api/v1/study-plans/{planId}/regenerate
```

### Skip task

```http
POST /api/v1/study-plans/{planId}/tasks/{taskId}/skip
```

### Reschedule task

```http
POST /api/v1/study-plans/{planId}/tasks/{taskId}/reschedule
```

### Complete task

```http
POST /api/v1/study-plans/{planId}/tasks/{taskId}/complete
```

### Reopen task

```http
POST /api/v1/study-plans/{planId}/tasks/{taskId}/reopen
```

Only include this endpoint if reopening completed tasks is accepted by the business rules.

---

## 10. Study Sessions

### List study sessions

```http
GET /api/v1/study-sessions
```

### Start study session

```http
POST /api/v1/study-sessions
```

Optional request fields:

- studyTaskId
- sessionType
- plannedDurationMinutes

### Get active session

```http
GET /api/v1/study-sessions/active
```

### Get session details

```http
GET /api/v1/study-sessions/{sessionId}
```

### Pause session

```http
POST /api/v1/study-sessions/{sessionId}/pause
```

### Resume session

```http
POST /api/v1/study-sessions/{sessionId}/resume
```

### Complete session

```http
POST /api/v1/study-sessions/{sessionId}/complete
```

Representative fields:

- focusScore
- summary
- struggledWith
- reviewTomorrow

### Cancel session

```http
POST /api/v1/study-sessions/{sessionId}/cancel
```

Possible errors:

- `STUDY_SESSION_NOT_ACTIVE`
- `STUDY_SESSION_ALREADY_COMPLETED`
- `ACTIVE_STUDY_SESSION_ALREADY_EXISTS`

---

## 11. Mock Interview Sessions

### List interview sessions

```http
GET /api/v1/interview-sessions
```

### Create interview session

```http
POST /api/v1/interview-sessions
```

Representative criteria:

- interviewType
- knowledgeNodeIds
- difficulties
- questionCount
- answerDurationSeconds
- allowSkip
- excludeRecentlyAskedDays

### Get session

```http
GET /api/v1/interview-sessions/{sessionId}
```

### Start session

```http
POST /api/v1/interview-sessions/{sessionId}/start
```

### Get current question

```http
GET /api/v1/interview-sessions/{sessionId}/current-question
```

### Submit answer

```http
POST /api/v1/interview-sessions/{sessionId}/answers
```

Representative fields:

- sessionItemId
- userAnswer
- selfScore
- confidenceLevel
- durationSeconds
- notes

### Skip question

```http
POST /api/v1/interview-sessions/{sessionId}/items/{sessionItemId}/skip
```

### Move to next question

```http
POST /api/v1/interview-sessions/{sessionId}/next
```

### Complete session

```http
POST /api/v1/interview-sessions/{sessionId}/complete
```

### Cancel session

```http
POST /api/v1/interview-sessions/{sessionId}/cancel
```

### Get session result

```http
GET /api/v1/interview-sessions/{sessionId}/result
```

### Get session items

```http
GET /api/v1/interview-sessions/{sessionId}/items
```

Possible errors:

- `INTERVIEW_SESSION_NOT_STARTED`
- `INTERVIEW_SESSION_COMPLETED`
- `INTERVIEW_SESSION_CANCELLED`
- `INTERVIEW_ITEM_ALREADY_ANSWERED`
- `INSUFFICIENT_QUESTIONS_FOR_CRITERIA`

---

## 12. Search

### Global search

```http
GET /api/v1/search
```

Query:

```text
?q=
&modules=knowledge,interview,dsa,projectStory
&tagId=
&difficulty=
&page=
&pageSize=
```

Search targets:

- Knowledge nodes
- Interview questions
- Answer versions
- Code examples
- DSA problems
- DSA mistakes
- Project stories

PostgreSQL full-text search is sufficient for the MVP.

---

## 13. Analytics

### Dashboard summary

```http
GET /api/v1/analytics/dashboard
```

### Weekly study summary

```http
GET /api/v1/analytics/study/weekly
```

Query:

```text
?weekStart=YYYY-MM-DD
```

### Study time trend

```http
GET /api/v1/analytics/study/time
```

### Review accuracy

```http
GET /api/v1/analytics/reviews/accuracy
```

### DSA progress by pattern

```http
GET /api/v1/analytics/dsa/patterns
```

### Interview score trend

```http
GET /api/v1/analytics/interviews/scores
```

### Weak topics

```http
GET /api/v1/analytics/weak-topics
```

### Study streak

```http
GET /api/v1/analytics/streak
```

Analytics endpoints are read-only and may use optimized query projections.

---

## 14. Import and Export

### Create full export

```http
POST /api/v1/exports
```

This should support an idempotency key.

### Get export status

```http
GET /api/v1/exports/{exportId}
```

### Download export

```http
GET /api/v1/exports/{exportId}/download
```

### Export interview questions as Markdown

```http
POST /api/v1/exports/interview-questions
```

### Export DSA progress as CSV

```http
POST /api/v1/exports/dsa-progress
```

### Create import preview

```http
POST /api/v1/imports/preview
```

### Commit import

```http
POST /api/v1/imports/{importId}/commit
```

### Get import report

```http
GET /api/v1/imports/{importId}
```

### Cancel import

```http
POST /api/v1/imports/{importId}/cancel
```

Import requirements:

- Schema validation
- Duplicate detection
- Preview
- Item-level errors
- Transactional commit strategy
- Idempotency for repeated import requests

---

## 15. Backup and Restore

### List backups

```http
GET /api/v1/backups
```

### Create backup

```http
POST /api/v1/backups
```

This operation should support an idempotency key.

### Get backup metadata

```http
GET /api/v1/backups/{backupId}
```

### Download backup

```http
GET /api/v1/backups/{backupId}/download
```

### Validate backup before restore

```http
POST /api/v1/backups/validate
```

### Restore backup

```http
POST /api/v1/backups/{backupId}/restore
```

Possible errors:

- `BACKUP_VERSION_UNSUPPORTED`
- `BACKUP_CHECKSUM_INVALID`
- `BACKUP_RESTORE_FAILED`
- `RESTORE_OPERATION_ALREADY_RUNNING`

---

## 16. Background Jobs

These endpoints are primarily for local administration and troubleshooting.

### List jobs

```http
GET /api/v1/system/jobs
```

### Get job

```http
GET /api/v1/system/jobs/{jobId}
```

### Retry failed job

```http
POST /api/v1/system/jobs/{jobId}/retry
```

### Cancel pending job

```http
POST /api/v1/system/jobs/{jobId}/cancel
```

### Delete completed job history

```http
DELETE /api/v1/system/jobs/completed
```

Only expose administrative endpoints that are genuinely useful in the local MVP.

---

## 17. Health and system information

### Liveness

```http
GET /health/live
```

### Readiness

```http
GET /health/ready
```

### Application information

```http
GET /api/v1/system/info
```

Response:

```json
{
  "applicationName": "DevRecall",
  "version": "0.1.0",
  "environment": "Development",
  "currentTimeUtc": "2026-07-23T00:00:00+00:00"
}
```

---

## 18. Recommended implementation priority

### Priority 1 — Foundation

- Auth initialize/login/logout/me
- User preferences
- Health checks

### Priority 2 — Core content

- Knowledge Tree
- Interview Questions
- Answer Versions
- Tags
- Search v1

### Priority 3 — Practice

- DSA Patterns
- DSA Problems
- DSA Attempts
- Review Items
- Due Review Queue

### Priority 4 — Learning workflow

- Daily Study Plans
- Study Sessions
- Mock Interview Sessions

### Priority 5 — Reliability and reporting

- Analytics
- Import/export
- Backup/restore
- Background jobs

---

## 19. APIs intentionally excluded from the initial MVP

Do not implement APIs for:

- AI answer evaluation
- AI-generated follow-up questions
- AI-created flashcards
- Embeddings
- Vector search
- Voice recording
- Speech-to-text
- Pronunciation scoring
- Cloud synchronization
- Team collaboration
- Public sharing
- Payment
