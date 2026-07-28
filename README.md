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

## Implementation status

### Week 3 — Identity and Authentication

Completed:

- User domain model and case-insensitive email identity
- PostgreSQL user persistence and unique normalized email
- Registration with framework-supported password hashing
- Login with secure cookie authentication
- Current-user and logout endpoints
- Named authenticated-user authorization policy
- PostgreSQL-backed authentication API workflow tests

### Week 4 — Knowledge Tree Core

Completed:

- Knowledge node aggregate with rename, move, archive, and hierarchy invariants
- PostgreSQL knowledge node persistence with ownership and active-tree filtering
- Create, read tree, rename, move, and archive REST endpoints
- Stable Problem Details error codes for knowledge operations
- Cycle prevention for self-parenting and descendant moves
- PostgreSQL-backed domain, application, integration, and API regression coverage

### Week 5 — Knowledge Advanced

Completed:

- Knowledge note content
- Knowledge description and source URL
- Knowledge detail API with archived-note access
- Safe content updates using expected timestamps
- No-op content update detection
- Persistent sibling ordering and append-to-end creation
- Root and child reorder workflows
- Tree ordering based on stored positions

### Week 6 — Knowledge Tags and Tree Interaction

Completed:

- User-scoped tags
- Tag name normalization and uniqueness
- Tag create, list, rename, and archive workflows
- Assign and remove tags from knowledge notes
- Knowledge detail with tags
- Knowledge filtering by one or multiple tags
- AND semantics for multi-tag filtering
- Unified tree position endpoint
- Same-parent reorder and cross-parent move
- Move to root
- Cycle and ownership protection

### Week 7 — Interview Question Bank Core

Completed:

- Interview question domain model
- Difficulty and lifecycle status
- PostgreSQL persistence and indexes
- Create question workflow
- Active question list with pagination
- Topic and difficulty filtering
- Question detail
- Question update
- Question archive
- Ownership isolation
- Archived-question semantics

### Week 8 — Interview Answers and Follow-ups

Completed:

- Versioned interview answers
- Draft and published answer lifecycle
- One active draft per question
- Automatic answer version numbering
- Published-answer immutability
- Answer publishing workflow
- Current published answer selection
- Latest draft selection
- Answer version history
- Interview follow-up questions
- Follow-up update and ordering
- Follow-up archive workflow
- Complete interview question detail
- Ownership and archived-question protection

### Week 9 — DSA Problem Bank Core

Completed:

- DSA problem domain model
- Difficulty and lifecycle status
- PostgreSQL problem persistence
- Owned topic collection persistence
- Create DSA problem workflow
- Active problem list with pagination
- Difficulty, topic and source filtering
- DSA problem detail
- Problem update and topic replacement
- Problem archive workflow
- Ownership isolation
- Archived-problem semantics

### Week 10 — DSA Attempt History

Completed:

- Immutable DSA attempt snapshots
- Attempt result and duration validation
- Automatic attempt numbering per problem
- PostgreSQL attempt persistence
- Unique problem-attempt sequence constraint
- Attempt creation workflow
- Paginated attempt history
- Result filtering
- Full attempt detail
- Latest successful attempt
- Attempt comparison
- Complete DSA problem progress summary
- Recent-attempt overview
- Ownership and archived-problem protection

## Health endpoints

Liveness:

```http
GET /health/live
```

Readiness:

```http
GET /health/ready
```

Liveness reports process health independently of PostgreSQL. Readiness verifies that PostgreSQL is reachable and returns `503 Unhealthy` when the database is unavailable.

## Request correlation

Clients may provide an `X-Correlation-ID` request header. The API echoes the same value in the response and includes it as `traceId` in Problem Details responses. If the header is omitted, the API generates a correlation ID automatically.

## Database migrations

Open Visual Studio Package Manager Console and select `DevRecall.Infrastructure` as the default project.

Create a migration:

```powershell
Add-Migration MigrationName -Project DevRecall.Infrastructure -StartupProject DevRecall.Api -OutputDir Persistence/Migrations
```

Apply migrations:

```powershell
Update-Database -Project DevRecall.Infrastructure -StartupProject DevRecall.Api
```

Rollback all migrations:

```powershell
Update-Database 0 -Project DevRecall.Infrastructure -StartupProject DevRecall.Api
```

Migrations are applied explicitly as a deployment step. The API does not call `Database.Migrate()` during startup.

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

## Development commands

## Integration tests

Integration tests use Testcontainers with PostgreSQL.

Requirements:

- Docker Desktop must be running.

Run:

```bash
dotnet test tests/DevRecall.IntegrationTests
```

The test suite creates, migrates and disposes its own PostgreSQL container. It does not use the local development database or User Secrets.

### Restore packages
```bash
dotnet restore
```

### Build solution
```bash
dotnet build
```

### Run tests
```bash
dotnet test
```

### Run API
```bash
dotnet run --project src/DevRecall.Api
```

### Format code
```bash
dotnet format
```

## Development environment

- Backend IDE: Visual Studio 2026
- Frontend IDE: Visual Studio Code
- Package management: NuGet Package Manager
- Runtime: .NET 10
