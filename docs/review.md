# Review

## Learning-content provenance

Review owns recall, reveal, evaluation, history, concurrency, and scheduling. A card selected from a
Learning Content lesson is an ordinary `ReviewItem` with resource type `LearningContent`; there is no
lesson-specific scheduler or rating path.

At creation, the card stores user-owned snapshots of the candidate prompt and answer plus the lesson
title. Candidate or lesson edits therefore do not silently rewrite an existing card. Source metadata is
supporting provenance only:

```json
{
  "type": "LearningContent",
  "title": "ASP.NET Core Service Lifetimes",
  "slug": "aspnet-core-service-lifetimes",
  "isAvailable": true
}
```

Internal lesson and candidate identifiers are not part of the source contract. If the lesson is
Archived, `isAvailable` becomes false while the prompt and answer remain usable. Review deletion/archive
follows the existing Review lifecycle and does not alter the lesson or its completion evidence.

## Retention semantics

The closed loop is:

```text
Complete lesson -> select candidates -> normal Review queue -> Recall -> Reveal -> Rate
                -> existing scheduler -> next due review
```

- Creating a card creates no Review outcome, learning completion, Weak Topic update, or study minutes.
- `Again`, `Hard`, `Good`, and `Easy` use the same scheduler state transitions for every resource type.
- Rating creates one normal Review history/evidence record and never changes lesson progress.
- A completed lesson remains Completed after weak recall; Completed does not mean Mastered.
- Learning History contains lesson completions, not Review attempts.
- Analytics counts lesson completion and Review outcomes independently and deduplicates active days by
  its canonical date rules.
- Weak Topics use only existing valid Review attribution. Global Learning Content topics are not
  automatically created or mapped into a user's personal topic taxonomy.

In Focus Mode the source label stays subordinate to the prompt. The answer is never visible before
Reveal. The source action appears only after a rating has been stored and opens the lesson in a new tab,
preserving the active session without a custom restoration mechanism. Manual and lesson-derived cards
can coexist in the same queue with identical keyboard and concurrency behavior.
