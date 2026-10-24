# Pathway

Implemented by AI. Designed by human intelligence.

A full-stack learning platform prototype for teaching C#/.NET from first principles through staff-level engineering.

## Stack

- **Frontend:** Vite, React 19, TypeScript, Tailwind CSS 4, and Lucide icons.
- **Backend:** ASP.NET Core on .NET 10, with an OpenAPI endpoint and a clean submission-validation contract.
- **Production evaluation:** a dedicated, sandboxed worker service. Never execute learner code in the web server process.

## Run locally

```sh
cd frontend && npm run dev
cd backend/Pathway.Api && dotnet run
```

The Vite dev server proxies `/api` to the API at `http://localhost:5100`.

## Curriculum invariant tests

```sh
dotnet test backend/Pathway.Api.Tests
```

These check every published course for version stamps, contiguous lesson order, and in-course `NextSlug` links, so content changes do not need hardcoded lesson counts.

## Browser smoke tests

Install the Playwright browser once, then run the suite locally or against production:

```sh
cd frontend
npx playwright install chromium

# Local: start the API in another terminal first.
npm run test:e2e

# Production (the API is served on the same origin under /api)
PLAYWRIGHT_BASE_URL=https://pathway.orangecastle.net \
PLAYWRIGHT_API_BASE_URL=https://pathway.orangecastle.net \
npm run test:e2e
```

The suite verifies API health and curriculum metadata, and checks that a guest learner can load and interact with the first lesson. It intentionally does not create production accounts or execute code exercises.

## Hosting

Production runs on **Vercel** (frontend, plus the API and evaluator broker as containers), **Neon** (Postgres) and **Auth0** (sign-in). [docs/hosting.md](docs/hosting.md) has the architecture, every environment variable, the Auth0 setup and the DNS record. Each Vercel project deploys on push to `main` when its own directory changes. The Railway deployment and its `railway.toml` files are legacy and will be removed once the new hosting is verified.

## Sign-in (Auth0)

The SPA signs learners in with Auth0 using the authorization code flow with PKCE (`@auth0/auth0-spa-js`). Auth0 hosts the sign-in and sign-up pages, so Pathway never sees a password. The API validates Auth0 access tokens (issuer, signature, audience, expiry) through standard JWT bearer authentication configured by `AUTH_AUTHORITY` and `AUTH_AUDIENCE`, so any OIDC provider that issues JWT access tokens works.

For local development, copy [frontend/.env.example](frontend/.env.example) to `frontend/.env.local` and fill in the `VITE_AUTH0_*` values from a development Auth0 application (add `http://localhost:5173` to its allowed callback, logout and web-origin URLs). The backend variables are in [appsettings.Development.json.example](backend/Pathway.Api/appsettings.Development.json.example); do not commit a populated local settings file. With no Auth0 configuration, the app runs in guest-only mode.

Signed-in learners' progress and submissions are stored under `user:<sub>`. Guests can learn without an account: the SPA sends a random ID in `X-Learner-Id`, stored as `guest:<id>` so it can never address a signed-in learner's records. Guests can answer every in-process exercise (multiple choice, numeric, circuit) and read their progress. Code exercises start a sandbox and require sign-in once authentication is configured. The learning-experience endpoints (workspaces, community, coach, module checkpoints) also require an account.

## Code evaluation

Learner code never runs in the API process. The API forwards code exercises to the sandbox broker (`sandbox-broker/`, `POST /evaluate` with `{ "lessonSlug", "code" }` and a shared-secret header: `EVALUATOR_SHARED_SECRET` = the broker's `RUNNER_SHARED_SECRET`). The broker creates a fresh **Vercel Sandbox** microVM per submission from one of four images in `sandbox-broker/images/` (`dotnet`, `rust`, `node`, `tools`): no network, 1 vCPU, a 12-second command limit and bounded output. Learner commands run as an unprivileged user through the images' `as-learner` wrapper. The Sandbox images workflow builds those images on pull requests and publishes them to the broker project's Vercel Container Registry from `main`. When `ASPNETCORE_ENVIRONMENT=Production`, code exercises fail closed unless the evaluator URL and secret are configured. The in-process deterministic validator exists only for local content development.

## Included experience

- A data-driven C# 14/.NET 10 course API with a review date and official source attached to every lesson.
- A lesson engine that supports multiple-choice and code-writing exercises, deterministic answer feedback, gating, next-lesson navigation, and browser-persisted completion state.
- A curriculum starter path spanning program basics, variables, `if`, records, sealed concrete API types, cancellation, and switch expressions.
- An embedded editor with reset, copy, tab indentation, and `Cmd/Ctrl + Enter` test execution.

## Content architecture

Course content is authored in `backend/Pathway.Api/Program.cs` as typed, versioned lesson records. Each lesson has concept copy, an example, requirements, a supported exercise type, tests or choices, hint, progression link, and a `C# 14` / `.NET 10` review stamp. This is intentionally structured so it can next move to a database-backed content-management workflow without changing the learner API shape.

`GET /api/courses` returns the language-track catalog and `GET /api/courses/{courseId}` returns a course’s modules. The first catalog item is C#/.NET; adding another language means authoring another course with its own version stamp and lesson set, not changing the lesson engine.

The local API first parses submitted C# with Roslyn and returns syntax diagnostics, then applies deterministic lesson checks. This makes authoring feedback useful without executing untrusted code. Replace the lesson checks with the isolated evaluator worker before accepting arbitrary production submissions.
