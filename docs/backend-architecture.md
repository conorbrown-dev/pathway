# Backend architecture

The API follows a light Clean Architecture split into four projects, with the web project organised as vertical slices (one folder per feature).

```
backend/
├── Pathway.slnx
├── Directory.Build.props        shared target framework and compiler settings
├── Dockerfile.vercel            builds Pathway.Api (Vercel root directory: backend)
├── Pathway.Domain/              course model and grading rules
├── Pathway.Content/             course material and catalogs
├── Pathway.Infrastructure/      Postgres, sandbox evaluator, coach model
├── Pathway.Api/                 HTTP host and feature slices
└── Pathway.Api.Tests/           architecture, curriculum and API tests
```

## What each project is for

| Project | Holds | May depend on |
| --- | --- | --- |
| **Pathway.Domain** | `Course`, `Lesson`, `Exercise` and the other content records; grading of choice, numeric and circuit answers (`AnswerGrader`, `CircuitExerciseValidator`); the advisory `CodeReviewer`; the spaced-review interval rule (`SpacedReview`). | Nothing but .NET itself. |
| **Pathway.Content** | Every course's lessons (`Courses/`), the career, checkpoint and project-template catalogs, and `CourseCatalog`, the single public entry point to content. | Domain. |
| **Pathway.Infrastructure** | `ProgressDbContext` and its entities, schema setup, the Postgres connection-string parser, `EvaluatorClient` (sandbox), `CoachModelClient` (OpenAI) and the development-only `LocalAuthoringChecker`. | Domain, plus EF Core, Npgsql and Roslyn. |
| **Pathway.Api** | `Program.cs` (configuration and middleware only), `Hosting/` (rate limits, security headers), `Identity/` (learner IDs) and `Features/`. | All of the above. |

Dependencies only point inward: Api → Content/Infrastructure → Domain. `ArchitectureTests` fails the build if Domain starts using another Pathway project, ASP.NET Core, EF Core or Roslyn, if Content uses Infrastructure or the web host, or if Infrastructure uses Content or ASP.NET Core.

## Feature slices

Each folder under `Pathway.Api/Features/` owns its routes, request and response records, and logic:

| Slice | Routes |
| --- | --- |
| `Health` | `/health`, `/ready`, `/openapi/v1.json` |
| `Courses` | `/api/courses`, `/api/courses/{id}`, `/api/lessons/{slug}` |
| `Progress` | `/api/progress` |
| `Submissions` | `/api/submissions/validate` |
| `HookPlayground` | `/api/claude-hooks/evaluate` |
| `Analytics` | `/api/activity`, `/api/admin/analytics` |
| `Experience/*` | `/api/experience/...`: Dashboard, Reviews, Projects, Career, Assessments, Coach, Community |

A slice may use `ProgressDbContext` directly. There is no repository layer or mediator: the slices are small, and adding one would add indirection without protecting a rule. Logic that two slices share moves out of either slice: grading rules into Domain, review scheduling into `Experience/Reviews/ReviewScheduler`, input validation into `Experience/InputRules`.

## Adding things

- **A lesson or course:** edit or add a file in `Pathway.Content/Courses/`, then register a new course in `CourseCatalog`. `CurriculumInvariantTests` checks ordering, links, stamps and that answers never leak into public JSON.
- **An endpoint:** add it to the slice it belongs to, or create `Features/<Name>/<Name>Endpoints.cs` with a `Map...` extension method and call it from `Program.cs`.
- **A new external service:** put the client in Infrastructure and register it in `DependencyInjection.AddPathwayInfrastructure`.

## Why course content and the catalog are separate classes

`Curriculum` is one `partial` class spread over many files. C# sets up a class's static fields in file order, so a field that reads a value defined in a later file silently gets `null` (this is how the Tailwind lessons once lost their version stamps). Version stamps are therefore properties, and the assembled catalog lives in its own class, `CourseCatalog`, which is only built after `Curriculum` is fully set up.
