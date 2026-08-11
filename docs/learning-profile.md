# Learning Profile

## Purpose

Learning Profile stores the current user's declared learning direction. It provides stable inputs for future content discovery without coupling recommendation work to authentication or account identity.

Learning Profile contains user-provided preference and experience signals. It is not an objective assessment of developer skill.

## Domain model

Each user can own at most one profile. The aggregate contains:

- target role;
- current experience level;
- available study minutes per day;
- one or more structured technologies, optionally marked primary;
- one or more structured learning goals;
- an optimistic concurrency version.

Technology and goal collections have unique database constraints. Values use a deliberately small, stable taxonomy; free-text values are not accepted.

The existing onboarding preference remains separate. Onboarding controls the initial product experience, while Learning Profile supplies declared signals for v2 content discovery.

## Configuration semantics

An absent profile is a normal state. `GET /api/v1/learning-profile` returns HTTP 200 with `isConfigured: false`, empty collections, and nullable scalar values.

A persisted profile is complete and therefore configured. Initial creation requires all five signal groups. Partial database aggregates are not stored.

## API

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/v1/learning-profile` | Read the authenticated user's profile. |
| `GET` | `/api/v1/learning-profile/options` | Read stable values and English display metadata. |
| `PUT` | `/api/v1/learning-profile` | Create or replace the authenticated user's profile. |

The server derives ownership from the authenticated session. Requests never accept a user ID. Public enum values are case-sensitive strings; numeric enum representations are rejected.

## Concurrency and no-op behavior

Creation requires `expectedVersion: null`. Updating an existing profile requires the current version. A stale or invalid expectation returns HTTP 409 with `LEARNING_PROFILE_CONFLICT`.

Technology and goal ordering is not semantically significant. Submitting an unchanged profile does not call `SaveChanges`, increment `version`, or change `updatedAtUtc`.

The PUT response is the canonical saved profile. Clients update their cache from it and do not need a follow-up GET.

## Deliberate exclusions

This foundation does not score profiles, infer skill, recommend content, modify Today priority, rewrite onboarding, ingest external resources, or introduce AI behavior.

## Declared and observed signals

Learning Profile is the canonical source of declared signals: what the user says they are working toward. It remains separate from observed signals produced by actual learning behavior.

```mermaid
flowchart TD
    User["User"] --> Profile["Learning Profile"]
    Profile --> Declared["Declared Signals"]
    Declared --> Content["Content Metadata"]
    Declared --> Future["Future Recommendation Engine"]
    Observed["Observed Signals"] --> Future
    Observed --- Evidence["Weak topics · Practice · Reviews · Learning completions"]
```

The internal `LearningProfileSignals` contract preserves target role, experience level, primary technologies, other technologies, goals, available minutes, and configured state. It does not infer technologies from a role or convert a self-reported level into a skill score.

Future recommendation work may combine these declared signals with observed signals, but the two sources retain clear provenance.

## Future content matching contract

Learning Content should provide enough metadata to compare candidates along these dimensions:

- target roles;
- technologies;
- difficulty;
- estimated minutes;
- goals or intent;
- topics.

An empty target-role collection means role-neutral content, not content that matches nobody. Technology metadata follows the same principle when empty.

Expected difficulty compatibility is deliberately soft: Beginner content suits beginners; Junior users may receive Beginner or Intermediate content; Mid-level and Senior users may receive Intermediate or Advanced content. No persistent mapping table or scoring rule exists yet.

Estimated duration is also a soft signal. Content longer than the user's daily availability remains discoverable. Goal matching will eventually influence ranking—for example, interview preparation can favor core concepts and common interview topics—but no matching or scoring is implemented during Week 1.

## Privacy and lifecycle boundaries

External catalog acquisition is global. DevRecall must not send a user's profile, identity, career intent, or goals to an external content provider merely to fetch catalog data. Personalization occurs inside DevRecall.

A future recommendation may persist the specific reason signals needed to explain why it was generated; it should not snapshot the whole profile by default.

Changing a Learning Profile may make future Discover recommendations stale. It does not:

- create learning activity or analytics evidence;
- change Weak Topics;
- rewrite Knowledge or Review history;
- mutate an existing Study Plan;
- rewrite historical recommendations.

Users without a Learning Profile retain full access to existing v1 workflows. Future Discover behavior should fall back to curated or popular content instead of treating a missing profile as an error.
