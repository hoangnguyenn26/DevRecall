# DevRecall Demo Guide

## Prepare the application

1. Copy `deploy/.env.example` to `deploy/.env` and replace the database password.
2. Start PostgreSQL and build the application images:

   ```powershell
   docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d postgres
   docker compose --env-file deploy/.env -f deploy/docker-compose.yml build api web
   ```

3. Apply all migrations explicitly:

   ```powershell
   docker compose --env-file deploy/.env -f deploy/docker-compose.yml run --rm api --migrate
   ```

4. Start the API and web application:

   ```powershell
   docker compose --env-file deploy/.env -f deploy/docker-compose.yml up -d api web
   ```

5. Set a demo password outside source control and run the idempotent seeder:

   ```powershell
   $env:DEVRECALL_DEMO_PASSWORD = "choose-a-local-demo-password"
   $env:DEVRECALL_DEMO_API_URL = "http://localhost:3000/api/v1"
   dotnet run --project tools/DevRecall.DemoData
   ```

Open `http://localhost:3000` and sign in as `demo@devrecall.local` with the password supplied above.

## Recommended demo journey

1. Start at **Today** and explain how the next-best-action resolver prioritizes active work.
2. Open **Knowledge** and edit the Dependency Injection note.
3. Complete one item in the **Review** focus mode with keyboard shortcuts.
4. Open the interview question, update a draft, and show immutable published versions.
5. Inspect the Number of Islands attempt and record a new attempt.
6. Open the generated Study Plan, mark it Ready, and convert it to a Study Session if the seed did not already do so.
7. Complete the Study Session and show the updated Analytics and Weak Topics evidence.
8. Press `Ctrl+K` and search across Knowledge, Interview, and DSA.

## Known MVP limitations

- Mock Interview Sessions, AI evaluation, i18n, and code execution are not implemented.
- The Docker Compose profile is intended for local demonstration. It explicitly disables secure-cookie enforcement because it serves plain HTTP on localhost.
- Use HTTPS and leave `Authentication:RequireHttpsCookies` enabled outside the local demo profile.
