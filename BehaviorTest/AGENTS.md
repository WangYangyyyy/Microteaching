Always answer my questions in Chinese.

# Repository Guidelines

## Project Structure & Module Organization
- `BehaviorTest.Application`: core application services (RBAC, audio/transcription pipeline, prompts, DTOs).
- `BehaviorTest.Core`: shared domain abstractions and Furion extensions.
- `BehaviorTest.EntityFramework.Core`: EF Core DbContext and database setup (`database.json`).
- `BehaviorTest.Database.Migrations`: migrations project for MySQL.
- `BehaviorTest.Web.Core`: ASP.NET Core server/Blazor setup, middleware, handlers.
- `BehaviorTest.Web.Entry`: entrypoint web host (`Program.cs`), static assets under `wwwroot/`.

## Build, Test, and Development Commands
- Restore all projects: `dotnet restore`.
- Build web host: `dotnet build BehaviorTest.Web.Entry/BehaviorTest.Web.Entry.csproj -c Release`.
- Run locally (HTTPS redirect on): `dotnet run --project BehaviorTest.Web.Entry/BehaviorTest.Web.Entry.csproj`.
- Database migrations (example): `dotnet ef migrations add <Name> --project BehaviorTest.Database.Migrations --startup-project BehaviorTest.Web.Entry`.

## Coding Style & Naming Conventions
- C# 4-space indentation; favor expression-bodied members for small accessors.
- Naming: `PascalCase` for classes/interfaces (`IService`), `camelCase` for locals/parameters, `PascalCase` for public properties.
- Use `async`/`await` with `Task`-based APIs; avoid sync-over-async.
- JSON naming policy is camelCase (see `Startup.cs`), match DTO fields accordingly.
- Keep HTTP endpoints thin; push logic into Application services.

## Testing Guidelines
- Preferred: `dotnet test` once test projects are added; name files `<TypeName>Tests.cs`.
- Add integration tests around services handling transcription, summaries, and Q&A flows.
- Aim to cover error paths (missing files, failed HTTP calls) and database interactions with in-memory or test MySQL.

## Commit & Pull Request Guidelines
- Commits: concise imperative subject (e.g., `Add audio batch transcription`), group related changes.
- PRs: include summary, linked issue/requirement, screenshots for UI changes, and steps to verify (build/run, key endpoints).
- Ensure migrations are included when schema changes; note any config updates (e.g., connection strings).

## Security & Configuration Tips
- Secrets/connection strings should live in user secrets or environment variables; do not commit credentials.
- `database.json` and `applicationsettings.json` are copied to output—review before shipping.
- External services (FFmpeg, transcription endpoint) must be reachable in the target environment; handle timeouts and log errors.***
