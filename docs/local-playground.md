# Local dogfood playground

Local database seeded on 2026-10-12. Backup before seeding:
`D:/DevRecall-data/backups/devrecall-before-playground-20261012.dump`.

An explicit, Development-only fixture for sustained product exploration. It creates
`hoang.playground@devrecall.local` (display name **Hoang Playground**) without changing
existing users, passwords, lessons or user-owned data. The password is supplied through
`DEVRECALL_DOGFOOD_PASSWORD`, never committed to source control.

## Contents

- 43 Knowledge nodes: eight topic roots, 32 concept notes, three lesson takeaways; eight tags.
- 32 Interview questions, 64 answer versions (published plus editable draft), 64 follow-ups
  and 64 simulated practice attempts.
- 20 DSA problems with concrete prompts/approaches and 80 simulated attempts.
- 50 Review items with 182 historical ratings, including six lesson candidate snapshots.
  Ten are initially due; the others have future dates calculated by the normal scheduler.
- The existing global catalog remains available: 11 lessons and five curated resources.
  Three lessons are completed with canonical evidence, one is in progress, seven remain
  available for fresh learning/Discover.
- Seven Study Plans: one Draft, three Ready, one Converted and two Cancelled.
- 27 Study Sessions: 23 Completed, one InProgress, two Planned and one Cancelled.
  Mixed sessions retain the lesson/external-task distinction.
- Weak Topic profiles and Recommendations are derived through the production signal
  readers and scoring policies from this fixture's outcomes, not manually invented scores.
  The initial run produced 60 profiles (12 with nonzero weakness) and 12 recommendations.

Dates are relative to the first seed run, spanning approximately eight weeks. Historical
outcomes are **simulated baseline data**, not an assessment of the person using the account.
Actual interactions after seeding naturally join this history. For an entirely personal
history, use your own existing account; the global Learn catalog is shared.

## Run or replay

After migrations and the existing Learning Content seed, build the API image and run:

```powershell
docker compose -f deploy/docker-compose.yml build api
$env:DEVRECALL_DOGFOOD_PASSWORD = "choose-a-local-password-of-at-least-12-characters"
docker compose -f deploy/docker-compose.yml run --rm --no-deps `
  -e ASPNETCORE_ENVIRONMENT=Development -e DEVRECALL_DOGFOOD_PASSWORD `
  api --seed-dogfood
Remove-Item Env:DEVRECALL_DOGFOOD_PASSWORD
```

The command uses a transaction and advisory lock. If the account already exists, it makes
no changes, including no password reset, history refill or moving due dates forward.
It does not run automatically on startup. No database migration is added.

## Suggested first session

1. Open Today and clear a few due Review cards using Reveal and a recall rating.
2. Resume the active backend Study Session, or open the in-progress lesson from Learn.
3. Try one untouched lesson, save a personal Knowledge note and select Review candidates.
4. Edit an Interview draft and publish it; compare a new DSA attempt with its baseline.
5. Open a Ready plan or edit the single Draft. External reading remains an explicit study
   task; opening the source does not complete the resource or create lesson evidence.
6. Inspect Analytics with a 30/90-day range, then Weak Topics and Recommendations.

The seed's PostgreSQL smoke checks replay/password preservation, catalog preservation,
lesson progress/evidence and completed external tasks with no lesson evidence. It also
exercises Weak Topic batch reads with mixed Session content. Those readers explicitly
exclude LearningContent from the existing personal-resource taxonomy.

Live verification passed: login, Today (10 due cards), Discover (four lessons and four
resources), History (three completions), plan/session lists, Analytics overview and batch
weakness recalculation. Seed replay made no changes. The production API image includes
the batch-reader correction; the database schema and existing users were preserved.
