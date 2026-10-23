# Production launch runbook

## Release gates

- Confirm Railway deployments for Web, Api, KeyCloak, ModalBroker, and both Postgres services are successful.
- Open `https://<api-domain>/ready`; it must return `200` and `{"status":"ready"}`.
- Create a disposable learner account and verify registration, sign-in, sign-out, persisted progress, a passing code submission, saved project work, and community posting.
- Confirm the API uses the Modal broker private URL and that the broker has the current `MODAL_TOKEN_ID`, `MODAL_TOKEN_SECRET`, and `RUNNER_SHARED_SECRET`.
- Confirm the Auth0 application's callback, logout and web-origin URLs list only the production origin. Use a separate Auth0 application for local development.

## Monitoring and response

- Vercel has no health checks for container functions. Monitor `GET /api/...` through the site and `GET /ready` on the API project, because `/ready` verifies the application database.
- Create uptime checks for the web root, API `/health` and API `/ready`. Alert the operator on two consecutive failures. Expect the first check after an idle period to be slower, because containers scale to zero after 5 minutes.
- Enable Railway log retention and configure an error-tracking provider before broad public traffic. Never send learner code, bearer tokens, authorization headers, or Modal credentials to telemetry.
- Configure a Modal spend alert and a monthly budget ceiling. Review sandbox usage daily during the first launch week.

## Data protection and recovery

- Keep the application database on its own Neon project; identity data lives in Auth0.
- Enable or schedule provider backups, retain at least seven daily restore points, and perform one documented restore rehearsal before launch.
- Limit Vercel, Neon and Auth0 access to named operators, use MFA for Vercel, GitHub, Modal and Auth0 admins, and rotate service credentials after any suspected exposure.
- The site has draft privacy, terms, and support pages. Replace the operator, legal entity, jurisdiction, retention, and security-contact placeholders after legal review.

## Operational controls

- API rate limits: public reads 180/minute, learner traffic 90/minute, submissions 10/minute, coaching 20/minute, and community writes 15/minute. Limits partition by signed-in user, otherwise source IP.
- Code execution fails closed when the broker is unavailable. Never bypass the Modal broker by executing learner code in API, Web, or Railway processes.
- Keep the legacy Railway `Evaluator` service until a signed production learner completes a Modal-backed code exercise. Then remove it from Railway; it is no longer on the API route.
- Set a real support mailbox and a private security-reporting channel before public launch. GitHub Issues is only an interim support route.
