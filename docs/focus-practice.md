# Focus Practice

Focus Practice is the shared distraction-free shell for Review, Interview, and DSA workflows. Every focus route uses the `focus` layout and authenticated middleware. The app sidebar, top bar, command palette, and global `g` navigation chords are unavailable while the focus layout is active.

## Lifecycle boundaries

- Review loads a bounded due queue, reveals the current prompt, records one Again/Hard/Good/Easy rating, and uses the server response as the scheduling result. A stale review-count conflict reloads state and is never retried as a mutation.
- Interview keeps the answer and follow-up drafts in component memory, compares against the reference answer selected at practice start, self-rates, then records one immutable attempt.
- DSA keeps solution and reflection drafts in component memory, records an outcome and complexity, then creates one immutable attempt. DevRecall does not execute or judge code.

Practice attempts are immutable learning records. Practice mode never edits historical attempts.

## Submission idempotency

Interview and focused DSA practice create a submission UUID when the session starts. A retry of the same logical submission reuses that UUID. A successful response is the completion source of truth; a later GET refresh failure does not turn the mutation into a failed session. Reusing a submission UUID for another resource returns a conflict.

## Exit and keyboard semantics

Clean sessions exit immediately. Dirty Interview and DSA sessions require confirmation; drafts are not written to URLs or browser storage. Review preserves server-recorded progress. Practice shortcuts are phase-scoped and plain-key shortcuts do not run while an editable field has focus. The shared help dialog lists only active commands.

Interview progress uses Answer, Compare, and Follow-up/Complete phases. DSA uses Solve, Reflect, and Complete. Review retains item-count progress because its queue is server-defined.

## History boundary

Interview and DSA history is shown inside its resource detail. Lists are paginated and contain summaries only; full answers and solutions are loaded only for an owner-scoped attempt detail. Comparison is limited to a selected attempt and the previous attempt and makes only deterministic observations. Review history remains owned by the scheduling workflow. Cross-user or missing attempts return the same not-found behavior.

## Query invalidation

- Review completion refreshes the due queue, Today, and navigation indicators.
- Interview completion refreshes that question's first history page and marks Today stale.
- DSA completion refreshes that problem's first history page and detail and marks Today stale.

Unrelated modules are not refreshed, and Today is not fetched during each practice phase.

Study Plan and Study Session execution are intentionally outside Week 20 and continue in Week 21.
