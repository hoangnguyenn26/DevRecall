# DevRecall

DevRecall is a local-first personal learning and technical interview preparation system for software developers.

The application combines structured technical knowledge, DSA practice, active recall, spaced repetition, daily study planning and mock interview workflows in one platform.

## Product goal

The goal of DevRecall is to help a developer answer three questions every day:

1. What should I study today?
2. What knowledge am I starting to forget?
3. Which technical areas are still weak?

DevRecall is not intended to be a generic note-taking application. It manages the full learning lifecycle:

```text
Capture
→ Practice
→ Evaluate
→ Review
→ Measure
→ Improve
```

## Main use cases

- Organize knowledge in a hierarchical tree.
- Maintain a C#/.NET interview question bank.
- Store short, standard and deep answer versions.
- Track DSA problems and every attempt.
- Record hints, mistakes and complexity analysis.
- Review items with Again, Hard, Good and Easy ratings.
- Generate a daily study plan based on available time.
- Run timed mock interview sessions.
- Track weak topics and learning progress.
- Export, back up and restore personal data.

## Technology stack

### Backend

- .NET 10
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- Clean Architecture
- Modular Monolith
- OpenAPI
- Docker Compose

### Frontend

- Vue.js
- TypeScript
- Vue Router
- Pinia

### Testing

- xUnit
- FluentAssertions
- Testcontainers
- ASP.NET Core integration tests
- Architecture tests

## Architecture

The backend follows Clean Architecture with pragmatic boundaries:

```text
API
 ↓
Application
 ↓
Domain

Infrastructure implements inward-facing abstractions.
```

The system is deployed as a modular monolith. The initial modules are:

- Identity
- Knowledge
- Interview
- DSA
- Review
- Study
- Analytics
- System

The Domain project is framework-independent. Business logic must not be placed in controllers or EF Core configurations.

See [`AGENTS.md`](./AGENTS.md) for implementation rules.

## Repository structure

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
└── README.md
```

## MVP scope

### Included

- Local account and authentication
- User study preferences
- Knowledge Tree
- Interview question bank
- Answer version history
- DSA problem catalog
- DSA attempt history
- Review scheduler
- Daily review queue
- Daily study plans
- Study sessions
- Mock interview sessions
- PostgreSQL full-text search
- Basic analytics
- Import and export
- Backup and restore
- Persisted background jobs

### Excluded

- AI evaluation
- AI-generated questions
- Voice recognition
- Semantic search
- Cloud synchronization
- Mobile application
- Multi-user collaboration
- Social features
- Microservices
- Message broker
- Kubernetes
- Payment

## Planned solution bootstrap

The initial backend setup should include:

- Central package management
- Nullable reference types
- Warnings as errors for project code
- PostgreSQL through Docker Compose
- EF Core migrations
- OpenAPI
- Problem Details
- Global exception handling
- Structured logging
- Health checks
- Authentication skeleton
- Testcontainers integration-test fixture
- Architecture tests

## Local development prerequisites

- .NET 10 SDK
- Node.js LTS
- Docker Desktop or compatible Docker runtime
- PostgreSQL client tools are optional

## Intended startup workflow

The final local startup workflow should be:

```bash
docker compose up -d postgres
dotnet ef database update --project src/DevRecall.Infrastructure --startup-project src/DevRecall.Api
dotnet run --project src/DevRecall.Api
npm install --prefix frontend/devrecall-web
npm run dev --prefix frontend/devrecall-web
```

The exact commands may change during the bootstrap phase.

## API conventions

- Base path: `/api/v1`
- JSON properties: camelCase
- URLs: kebab-case
- Date and time: ISO 8601, stored as UTC
- Errors: Problem Details
- IDs: UUID
- Pagination: offset pagination for MVP management screens
- Authentication: secure cookie preferred for same-origin deployment

The planned endpoint catalog is documented in [`docs/API_CATALOG.md`](./docs/API_CATALOG.md).

## Development roadmap

### Phase 1 — Foundation

- Bootstrap solution
- Configure PostgreSQL and EF Core
- Add logging, health checks and error handling
- Add authentication foundation

### Phase 2 — Knowledge and Interview

- Knowledge Tree
- Tags
- Interview questions
- Answer versions
- Search v1

### Phase 3 — DSA and Review

- DSA patterns and problems
- Attempt tracking
- Mistakes and solutions
- Review scheduler
- Due review queue

### Phase 4 — Study and Mock Interview

- Daily study plan
- Study sessions
- Mock interview sessions
- Review generation from weak answers

### Phase 5 — Reliability

- Analytics
- Import/export
- Backup/restore
- Persisted background jobs
- Performance and security review

### Phase 6 — Final MVP

- Regression testing
- Documentation
- Docker Compose startup
- Demo data
- Release candidate

## Contribution rules

Before implementing a feature:

1. Read `AGENTS.md`.
2. Identify the related module and use case.
3. Define business invariants.
4. Define the API contract.
5. Implement from Domain inward to API.
6. Add tests.
7. Update documentation.

## Status

The project is currently in the **documentation and implementation-planning phase**. AI features are deferred until the non-AI MVP is stable.
