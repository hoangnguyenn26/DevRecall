# AGENTS.md — DevRecall Engineering Instructions

## 1. Project identity

**Project:** DevRecall  
**Product type:** Local-first personal learning and technical interview preparation system  
**Primary user:** A software developer preparing for C#/.NET backend interviews and practicing DSA  
**Current phase:** Backend-first MVP implementation  
**AI scope:** AI integration is explicitly excluded from the initial MVP

DevRecall is not a generic note-taking application. Its core value is to connect technical knowledge, active recall, spaced repetition, DSA practice, study planning, and mock interview workflows into one system.

The product learning loop is:

```text
Capture knowledge
→ Practice recall
→ Record the answer or attempt
→ Evaluate performance
→ Schedule the next review
→ Detect weak topics
→ Repeat
```

---

## 2. Required technology stack

### Backend

- ASP.NET Core Web API
- .NET 10
- Entity Framework Core
- PostgreSQL
- Clean Architecture
- Modular Monolith
- REST API
- OpenAPI / Swagger
- Docker Compose

### Frontend

- Vue.js
- TypeScript
- Vue Router
- Pinia
- Typed HTTP client

### Testing

- xUnit
- FluentAssertions
- Testcontainers for PostgreSQL integration tests
- ASP.NET Core integration testing infrastructure
- Architecture tests

Do not replace the selected stack without an explicit architectural decision.

---

## 3. Architectural direction

DevRecall must be implemented as a **backend-first modular monolith** following **Clean Architecture with pragmatic boundaries**.

Expected dependency direction:

```text
DevRecall.Api
    ↓
DevRecall.Application
    ↓
DevRecall.Domain

DevRecall.Infrastructure
    → implements abstractions from Application or Domain
```

### Layer responsibilities

#### Domain

Contains:

- Entities
- Aggregate roots
- Value objects
- Domain services when genuinely required
- Domain errors
- Business invariants
- State transitions

Domain must not depend on:

- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- HTTP
- Vue
- File system
- Logging frameworks

#### Application

Contains:

- Use cases
- Commands and queries
- Handlers
- DTOs
- Validators
- Authorization checks
- Persistence and infrastructure abstractions
- Transaction orchestration

Application depends on Domain, not Infrastructure or API.

#### Infrastructure

Contains:

- EF Core DbContext and mappings
- PostgreSQL implementation
- Authentication implementation
- Search implementation
- Background jobs
- Import/export
- Backup and restore
- System clock implementation

#### API

Contains:

- HTTP endpoints or controllers
- Authentication middleware
- Exception handling
- Problem Details mapping
- OpenAPI configuration
- Dependency registration
- Request and response translation

The API layer must not contain business logic.

---

## 4. Solution structure

Use this structure unless there is a documented reason to change it:

```text
DevRecall/
├── src/
│   ├── DevRecall.Api/
│   ├── DevRecall.Application/
│   ├── DevRecall.Domain/
│   ├── DevRecall.Infrastructure/
│   └── DevRecall.Contracts/
├── tests/
│   ├── DevRecall.Domain.Tests/
│   ├── DevRecall.Application.Tests/
│   ├── DevRecall.IntegrationTests/
│   ├── DevRecall.Api.Tests/
│   └── DevRecall.ArchitectureTests/
├── frontend/
│   └── devrecall-web/
├── docs/
│   ├── architecture/
│   ├── adr/
│   ├── api/
│   └── database/
├── deploy/
│   └── docker-compose.yml
├── AGENTS.md
├── README.md
├── Directory.Build.props
├── Directory.Packages.props
└── DevRecall.sln
```

Organize Application code by feature, not by broad technical folders.

Preferred:

```text
Interview/
└── Questions/
    ├── CreateQuestion/
    ├── UpdateQuestion/
    ├── GetQuestion/
    └── SearchQuestions/
```

Avoid:

```text
Commands/
Queries/
Handlers/
Validators/
Dtos/
```

with unrelated features mixed together.

---

## 5. Business modules

The MVP contains the following modules:

1. Identity
2. Knowledge
3. Interview
4. DSA
5. Review
6. Study
7. Analytics
8. System

### Identity

Responsibilities:

- Local account initialization
- Login and logout
- Password change
- User preferences
- Time zone and daily learning targets

### Knowledge

Responsibilities:

- Hierarchical knowledge tree
- Create, rename, move, reorder, archive and restore nodes
- Tags
- Notes and references
- Tree search

Important invariant:

- A node cannot become its own parent.
- A node cannot be moved under one of its descendants.

### Interview

Responsibilities:

- Interview question bank
- Answer versions
- Short, standard and deep answers
- Follow-up questions
- Project stories
- Mock interview sessions
- Session scores and history

Important invariants:

- Published answer versions are immutable.
- Completed interview sessions cannot accept new answers.
- Archived questions cannot be selected for new sessions.

### DSA

Responsibilities:

- DSA patterns
- Problems
- Attempts
- Hint levels
- Solutions
- Mistakes
- Complexity analysis
- Mastery evaluation

Important invariants:

- An attempt cannot be completed twice.
- A solution viewed in full cannot be classified as independently solved.
- A problem is not mastered after only one successful attempt.
- Mastery requires at least two independent solves on separate review dates.

### Review

Responsibilities:

- Review items
- Due review queue
- Again, Hard, Good and Easy ratings
- Review history
- Next-review calculation
- Suspend, archive and restore

Important invariants:

- An archived review item cannot be reviewed.
- Every review submission creates an immutable review attempt.
- A source item cannot have multiple active review items under the same policy.

### Study

Responsibilities:

- Daily study plans
- Study tasks
- Study sessions
- Study reflections
- Available-time constraints
- Daily completion

Priority order for daily plan generation:

1. Overdue reviews
2. Items previously rated Again
3. Reviews due today
4. Weak topics
5. DSA target
6. Interview target
7. Optional new learning

### Analytics

Responsibilities:

- Read-only reporting
- Study duration
- Review accuracy
- DSA progress by pattern
- Interview score trends
- Weak topics
- Study streak

Analytics queries may use optimized projections and do not need to load domain aggregates.

### System

Responsibilities:

- Import
- Export
- Backup
- Restore
- Background jobs
- Health checks

---

## 6. Coding principles

Apply SOLID strictly but pragmatically.

### Required principles

- Prefer readable code over clever code.
- Prefer explicit behavior over hidden conventions.
- Prefer composition over inheritance.
- Keep business behavior inside aggregates where appropriate.
- Use immutable value objects.
- Use private setters for entity state.
- Mutate entities through business methods.
- Use `CancellationToken` throughout async call chains.
- Use UTC internally.
- Return DTOs, never EF Core entities.
- Use `AsNoTracking` for read-only queries.
- Apply filtering, projection and pagination before materialization.
- Use transactions for multi-table consistency boundaries.
- Use optimistic concurrency for editable content.
- Keep HTTP concerns in the API layer.
- Keep framework-specific code out of Domain.

### Avoid

- Generic repository that merely wraps `DbSet`.
- God services such as `LearningService`.
- Business logic in controllers or endpoints.
- Public setters on domain entities without a reason.
- Premature microservices.
- Event sourcing in the MVP.
- Distributed messaging in the MVP.
- Unnecessary abstraction layers.
- Returning `IQueryable` outside persistence boundaries.
- `.Result`, `.Wait()`, or fake async with `Task.Run`.
- Fire-and-forget work started from an HTTP request.
- Direct cross-module entity manipulation.

---

## 7. Persistence rules

Use PostgreSQL and EF Core.

### Conventions

- Table names: `snake_case`
- Column names: `snake_case`
- Primary keys: UUID
- Date-time values: `timestamp with time zone`
- JSONB only for genuinely unstructured payloads such as job payloads or import reports
- Archive semantics instead of a generic soft-delete mechanism everywhere
- No cascade delete for historical data such as attempts, sessions or review history

### Recommended index areas

- `knowledge_nodes(user_id, parent_id, sort_order)`
- `interview_questions(user_id, knowledge_node_id, status)`
- `dsa_problems(user_id, pattern_id, status)`
- `dsa_attempts(dsa_problem_id, attempted_at desc)`
- `review_items(user_id, next_review_at)`
- `review_attempts(review_item_id, reviewed_at desc)`
- `study_plans(user_id, plan_date)`
- `study_sessions(user_id, started_at desc)`

Do not add indexes without a query-based reason.

---

## 8. API standards

Base path:

```text
/api/v1
```

Use:

- REST-oriented resources
- JSON camelCase
- URL kebab-case
- ISO 8601 date-time values
- HTTP status codes with correct semantics
- Problem Details for errors
- Stable business error codes
- Offset pagination for ordinary management screens
- Idempotency keys only for operations where retries are risky

Do not wrap every successful response in a generic `{ success, data }` envelope.

Example validation error:

```json
{
  "type": "https://devrecall/errors/validation",
  "title": "Validation failed",
  "status": 400,
  "errors": {
    "question": [
      "Question is required."
    ]
  },
  "traceId": "..."
}
```

Example domain error code:

```text
KNOWLEDGE_NODE_CIRCULAR_PARENT
INTERVIEW_SESSION_COMPLETED
DSA_ATTEMPT_ALREADY_COMPLETED
REVIEW_ITEM_ARCHIVED
```

---

## 9. Authentication and security

The MVP is local-first but must still follow secure defaults.

Preferred production-like setup:

- Cookie authentication when Vue and API run under the same origin
- Secure, HttpOnly and SameSite cookie configuration
- CSRF protection when required
- Password hashing through ASP.NET Core Identity infrastructure or `PasswordHasher`
- User ownership checks for every user-owned resource
- Rate limiting for login
- Secrets provided through environment variables or user secrets
- No passwords, tokens, secrets, connection strings or personal answers in logs

Do not store access tokens in browser localStorage unless an explicit decision requires it.

---

## 10. Testing requirements

Every feature must be tested at the appropriate level.

### Domain tests

Required for:

- Knowledge tree cycle prevention
- DSA mastery rules
- Review scheduling
- Interview session transitions
- Study session transitions
- Daily-plan capacity rules

### Application tests

Required for:

- Use-case orchestration
- Authorization
- Validation
- Error behavior

### Integration tests

Use a real PostgreSQL instance through Testcontainers for:

- EF Core mappings
- Constraints
- Transactions
- Full-text search
- Optimistic concurrency
- Database migrations

Do not treat the EF Core InMemory provider as a substitute for PostgreSQL integration tests.

### API tests

Cover:

- Authentication
- Authorization
- Request validation
- Status codes
- Problem Details
- Pagination
- Serialization

### Architecture tests

Enforce:

- Domain does not reference Infrastructure
- Application does not reference API
- API endpoints do not directly use DbContext
- Domain entities are not exposed by API contracts

---

## 11. Definition of Done

A backend feature is complete only when:

- The use case and acceptance criteria are clear.
- Domain rules are implemented in the correct layer.
- Request and response contracts are defined.
- Validation is implemented.
- Authorization and ownership are checked.
- Database migration is added when required.
- Unit or application tests are present.
- Integration tests cover important persistence behavior.
- API behavior is documented in OpenAPI.
- Errors use Problem Details and stable error codes.
- Structured logging is included where useful.
- No EF entity is exposed through the API.
- Build succeeds without new warnings.
- Relevant documentation is updated.

---

## 12. Implementation order

Build in this order:

1. Solution bootstrap and cross-cutting foundation
2. Identity
3. Knowledge Tree
4. Interview Questions and Answer Versions
5. Search v1
6. DSA Catalog
7. DSA Attempts
8. Review Engine
9. Daily Study Plan
10. Study Sessions
11. Mock Interview Sessions
12. Analytics
13. Import and Export
14. Backup and Restore
15. Persisted Background Jobs

Do not build dashboard analytics before the underlying workflows exist.

---

## 13. How Codex should work in this repository

Before implementing a feature:

1. Read this file.
2. Read the related use case and architecture documents.
3. Inspect existing patterns in adjacent features.
4. Identify domain rules, transaction boundaries and ownership requirements.
5. Propose the smallest coherent implementation.
6. List files to create or modify.
7. Implement from Domain inward to API.
8. Add tests.
9. Update OpenAPI or documentation.
10. Report assumptions and trade-offs.

When requirements are ambiguous:

- Do not silently invent business rules.
- State the ambiguity.
- Propose a reasonable default.
- Keep the implementation easy to revise.

When refactoring:

- Preserve public API behavior unless a breaking change is explicitly requested.
- Prefer incremental changes.
- Do not move code across layers merely for aesthetic reasons.
- Do not introduce a design pattern without a concrete problem.

---

## 14. Prompt template for implementing a feature

Use this prompt structure when asking Codex to implement work:

```text
Implement the following DevRecall feature:

Feature:
<feature name>

Business goal:
<what problem this solves>

Use case:
<main flow, alternatives and exceptions>

Business invariants:
<rule IDs or explicit rules>

API contract:
<method, route, request, response and errors>

Persistence impact:
<tables, columns, indexes and transaction boundary>

Architecture constraints:
- Follow AGENTS.md.
- Keep Domain independent from frameworks.
- Do not use a generic repository.
- Do not expose EF entities.
- Use Problem Details.
- Add CancellationToken support.
- Add tests at the appropriate levels.

Deliverables:
- Domain changes
- Application use case
- Infrastructure implementation
- API endpoint
- Automated tests
- Documentation updates

Before coding, provide:
1. Assumptions
2. Proposed design
3. Files to add or modify
4. Risks and trade-offs
```

---

## 15. MVP exclusions

Do not implement the following unless explicitly requested:

- AI answer evaluation
- AI-generated follow-up questions
- Voice recording or speech recognition
- Semantic/vector search
- Cloud synchronization
- Mobile application
- Multi-user collaboration
- Payment
- Social features
- Microservices
- RabbitMQ or Kafka
- Kubernetes
- Event sourcing
- Public question marketplace
