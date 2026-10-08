# V2 Week 9 — External Resource Planning checkpoint

## Decision

Close External Resource Planning. Study Plan is sufficient for **intentional future
study**, not a general reference/bookmark library. Bookmark remains deferred: repeated
reference-only retrieval pain has not been established. This is a scope decision,
not a claim of measured satisfaction or actual human reading.

Next focus: **Today / Next Best Action integration**, reusing the existing Today
experience. Candidate priorities and implementation are deliberately left to that
phase; no Week 10 design or feature is introduced here.

## Preserved boundaries

- Discover finds relevant possibilities; Learn teaches lessons or provides curated
  source access; plans declare intent; sessions execute it; Knowledge retains
  personal understanding; Review measures recall.
- External resources have no global progress, lifetime completion, learning history,
  or `ExternalResourceCompleted` evidence. Add/Open remain read-only with respect to
  learning evidence and do not improve Weak Topics.
- Explicit **Mark study task complete** changes the owned Session task through the
  existing orchestration lifecycle. Lessons instead require canonical owner/resource
  completion evidence recorded after the Session starts.
- Lesson → Resource → Lesson produces two lesson completions/history entries, not
  three. Estimates contribute to planned duration only; actual StudyMinutes and
  activity retain existing Session semantics without a second resource signal.
- Duplicate membership is prevented within a plan; separate plans remain independent.
  The existing one-Draft-per-account constraint is unchanged.
- Archived resources cannot be newly planned/discovered/opened/converted. Existing
  plan/session items survive; an existing external Session task may explicitly finish
  after archival. Session titles retain their snapshot and show source unavailability.
- Ownership, expected-version conflicts and submission replay semantics remain intact.

## Lightweight validation

The two existing PostgreSQL-backed API scenarios passed (2 passed, 0 failed):

- `MixedStudySession_ShouldPreserveOrderCanonicalEvidenceAndActualTime`
- `ExternalResourceStudyTask_ShouldBeOwnerScopedDeduplicatedAndNeverCreateLessonEvidence`

They cover mixed execution, canonical lesson evidence, stale attachment/replay,
archive-safe task completion, independent plans, account isolation and the absence
of resource lesson progress/evidence. No new suite or full regression was added.

Local browser smoke verified normal login return to Discover, separate lesson/resource
sections, source/kind/rough reading estimate, explicit source CTA and secondary planning
action. The resource has no lesson Start/Complete controls. The no-Draft picker explains
the requirement and offers Create Study Plan. The outbound link has `_blank` and
`noopener noreferrer`. No live plan/task was created or completed for this inspection;
the full mixed execution is API-tested, not claimed as browser-dogfooded. External
reading usefulness, visual perception of Session actions and repeated retrieval pain
still need human feedback.

No application code, schema or deployment changed at this checkpoint, so unrelated
frontend gates, production rebuild and full backend suites were not rerun.

## Deferred scope

No Bookmark/SavedResource, resource lifecycle/history/analytics, proof-of-reading,
Discover suppression, recommendation feedback, provider ingestion, crawler, AI,
formal learning paths or new curriculum quota. Resource task completion is not proof
of mastery. Planning should remain optional, actionable and small enough to execute.
