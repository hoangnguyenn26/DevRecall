# Study Experience

Study Plans are planning artifacts; Study Sessions are execution artifacts. The application never treats opening a resource as completion.

## Lifecycles

```text
Study Plan: Draft -> Ready -> Converted
Study Session: Planned -> InProgress -> Completed
Study Session Item: Pending -> InProgress -> Completed | Skipped
```

Only Draft plans can be edited. Starting a Ready plan atomically creates and starts its Session and makes the plan terminal. A Session completes once every item is completed or skipped. A completed Session is immutable except for its user-authored reflection.

## Plan-to-Session snapshot

Starting a Session snapshots the ordered learning items, resource titles, and planned durations. Execution and historical summaries use this snapshot when a live resource is unavailable; later resource or planning changes cannot rewrite historical context.

## Evidence and practice handoff

Knowledge items use explicit completion. Interview and DSA items may be completed with a validated, owner-scoped practice attempt matching the item resource. Evidence summaries are bounded projections and never include full answers, code, or solutions. Missing historical evidence does not make the Session unreadable.

Successful practice and successful Session-item completion are separate mutations. The practice result remains successful if the subsequent Session update fails. The frontend retains the same completion submission ID when retrying the Session mutation and does not resubmit the attempt.

Session context in practice URLs is navigation context only. Backend ownership, resource matching, lifecycle, and evidence checks remain authoritative, so standalone practice continues to work without Session parameters.

## Idempotency and concurrency

Plan conversion is idempotent for the converted plan. Complete and Skip commands carry a submission ID protected by a named unique constraint. Reusing the same submission for the same logical transition returns the existing state; reusing it for another item conflicts.

Study Plan, Study Session, and reflection writes use `ExpectedVersion`. Stale writes return stable `409` Problem Details and are never retried automatically. Persistence exceptions and named PostgreSQL constraints are translated in Infrastructure; Application code does not inspect provider messages.

## Reads, summary, and reflection

The backend selects the current learning item and returns authoritative counts and remaining planned minutes. Completed summaries distinguish completed and skipped items, planned time and elapsed Session time. Elapsed time is wall-clock duration, not focused study time.

Reflection is an optional editable note on a completed Session. It is trimmed, bounded, concurrency-protected, and normalized no-op updates do not change the version or completion activity.

Session history is owner-scoped, paginated, and projection-only. Active Sessions appear first; completed history is ordered by completion time so later reflection edits do not reorder it.

## Query invalidation

Plan and Session mutations invalidate only their affected reads. Focus workflows mark Today stale rather than eagerly loading it after every item. Returning to Today therefore resolves the next action from backend state without distracting requests during practice.
