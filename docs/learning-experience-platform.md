# Learning-experience platform foundation

This document describes the API foundation for the next-generation Pathway experience. It keeps learning data server-side and ties learner-owned data to the Auth0 subject (`sub`) in production.

## Secure execution boundary

`pathway-api` never runs learner code. It forwards code to `EVALUATOR_URL`, the sandbox broker (`sandbox-broker/`). The broker verifies an internal shared-secret header and creates a fresh Vercel Sandbox (a Firecracker microVM) for every submission. The evaluator is responsible for an isolated runtime per submission with all of the following controls:

- A disposable filesystem and non-root process.
- No outbound network access, no host mounts, and no Docker socket.
- Language/runtime allowlist; C#/.NET and Python are separate execution profiles.
- CPU, memory, process-count, output-size, and wall-clock limits.
- No secrets in the execution environment.
- Structured results only: test counts, compiler/runtime diagnostics, and bounded review metadata.

The broker refuses any request without the shared secret. It authenticates to Vercel Sandbox with the short-lived OIDC token Vercel attaches to each of its requests, so it holds no long-lived platform credential, and the sandbox receives no credentials at all. New code lessons must add a deterministic fixture in `sandbox-broker/src/evaluator.mjs` before they can advance learners.

### Why a separate sandbox

The API and broker run as Vercel Functions, which cannot create the isolation learner code needs, so the legacy Bubblewrap evaluator stays fail-closed and is not the production execution target. The broker is only a request broker: learner code runs in a separate microVM with `networkPolicy: 'deny-all'`, 1 vCPU, a 12-second command limit and bounded output, created from the images in `sandbox-broker/images/`. A broker without its shared secret or sandbox settings reports unhealthy; the API fails closed on any broker failure.

The API fails closed for progression if the evaluator is unavailable. It can still provide static review feedback, but never declares a code exercise complete without the private evaluator.

## API capabilities

All `/api/experience/*` endpoints require an Auth0 access token in production. Guest endpoints work locally when authentication is intentionally not configured.

- `GET /dashboard` — completed lessons, streak, projects, recent work, and review queue.
- `GET /reviews` and `POST /reviews/{lessonSlug}/complete` — spaced-repetition items with 1, 2, 4…30-day intervals.
- `GET /projects/templates`, `POST /projects/{templateId}`, `GET /projects/{id}`, and `PUT /projects/{id}/files/{path}` — multi-file project workspace persistence.
- `GET`/`POST /assessments/{courseId}/{moduleId}` — module checkpoints and targeted lesson recommendations.
- `POST /coach` — guided, secret-aware coaching that refuses copy-paste answers.
- Community endpoints — bounded posts, mentor-request flag, and replies. Mentor status comes from the Auth0 `mentor` permission (RBAC with permissions in the access token; see `docs/hosting.md`).

## Rollout

Set `OPENAI_API_KEY` and `COACH_MODEL` only as server-side Railway variables when enabling provider-backed coaching. The coach sends `store: false`, screens inputs for common credentials before any provider request, and rejects generated content containing code fences or oversized responses. With either variable absent or the provider unavailable, it returns the deterministic Socratic guidance instead.

The application creates the new tables with idempotent PostgreSQL DDL at startup so the existing Railway database can receive the feature without a destructive reset. Move this schema to versioned EF migrations before multiple environments or regulated data require formal migration history.
