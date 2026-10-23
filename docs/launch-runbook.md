# Production launch runbook

## Release gates

- Confirm the latest production deployments of the Vercel projects `pathway`, `pathway-api` and `pathway-broker` are Ready, and that the Sandbox images workflow published all four images.
- Open `https://pathway-api-carl-brown87-9852.vercel.app/ready` (the API project's production URL; `/ready` is not under the site's `/api` rewrite); it must return `200` and `{"status":"ready"}`.
- Create a disposable learner account and verify registration, sign-in, sign-out, persisted progress, a passing code submission, saved project work, and community posting.
- Confirm the API's `EVALUATOR_URL` points at `pathway-broker`, and that the API's `EVALUATOR_SHARED_SECRET` equals the broker's `RUNNER_SHARED_SECRET`.
- Confirm the Auth0 application's callback, logout and web-origin URLs list only the production origin. Use a separate Auth0 application for local development.

## Monitoring and response

- Vercel has no health checks for container functions. Monitor `GET /api/...` through the site and `GET /ready` on the API project, because `/ready` verifies the application database.
- Create uptime checks for the web root, API `/health` and API `/ready`. Alert the operator on two consecutive failures. Expect the first check after an idle period to be slower, because containers scale to zero after 5 minutes.
- Configure log retention (a Vercel log drain) and an error-tracking provider before broad public traffic. Never send learner code, bearer tokens or authorization headers to telemetry.
- Watch Vercel Sandbox usage against the Hobby allowance (5,000 sandbox creations and 5 hours of active CPU a month; creation pauses, without charges, when exceeded). Review it daily during the first launch week and move to Pro, with a spend limit, before it becomes a constraint.

## Data protection and recovery

- Keep the application database on its own Neon project; identity data lives in Auth0.
- Enable or schedule provider backups, retain at least seven daily restore points, and perform one documented restore rehearsal before launch.
- Limit Vercel, Neon and Auth0 access to named operators, use MFA for Vercel, GitHub and Auth0 admins, and rotate service credentials after any suspected exposure.
- The site has draft privacy, terms, and support pages. Replace the operator, legal entity, jurisdiction, retention, and security-contact placeholders after legal review.

## Operational controls

- API rate limits: public reads 180/minute, learner traffic 90/minute, submissions 10/minute, coaching 20/minute, and community writes 15/minute. Limits partition by signed-in user, otherwise source IP.
- Code execution fails closed when the broker is unavailable. Never bypass the sandbox broker by executing learner code in the API or web processes.
- The legacy Railway `Evaluator` service is no longer on any route; it goes when the Railway project is deleted.
- Set a real support mailbox and a private security-reporting channel before public launch. GitHub Issues is only an interim support route.
