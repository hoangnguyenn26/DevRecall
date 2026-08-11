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
