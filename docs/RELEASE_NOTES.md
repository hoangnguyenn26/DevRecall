# DevRecall v1.0-devrecall

## Calm Learning Intelligence

This release turns the backend learning loop into an end-to-end application:

- Nuxt 4 public landing, authentication, responsive application shell, dark mode, and keyboard command palette.
- Today dashboard with deterministic next-best-action prioritization.
- Knowledge three-pane workspace and Review focus mode.
- Interview answer-version practice and DSA attempt workspace.
- Study Plan builder, optimistic concurrency handling, and Study Session execution.
- Accessible Analytics charts, Weak Topic explanations, and Recommendation lifecycle actions.
- Owner-scoped PostgreSQL full-text search across Knowledge, Interview, and DSA.
- Antiforgery protection, same-origin deployment proxy, explicit migrations, and idempotent demo-data tooling.

## Reliability

- Owner-scoped data isolation and consistent cross-user 404 behavior.
- Optimistic concurrency for editable Knowledge, Study Plans, and Study Sessions.
- Idempotent Review, practice, conversion, and session-completion workflows.
- PostgreSQL migrations verified from an empty database.
- Keyboard, responsive, reduced-motion, empty-state, and error-state hardening.

## Known limitations

- DSA records self-reported attempt outcomes; it does not execute or judge code.
- Interview ratings are self-ratings, not automated evaluation.
- Mock Interview Sessions, AI evaluation, i18n, cloud sync, and collaboration are not included.
