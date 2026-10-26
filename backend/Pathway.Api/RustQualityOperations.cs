static partial class Curriculum
{
    // Authored lessons for the "Quality and operations" module: testable boundaries, metrics,
    // latency and capacity, resilience patterns, and delivering a service safely.
    // Fixtures live in sandbox-broker/src/evaluator.mjs.

    private static Lesson RustTestingIntegration(Lesson lesson) => lesson with
    {
        Title = "Test at useful boundaries",
        Subtitle = "Put the outside world (stock, payments, time) behind small traits so business rules can be tested fast and exhaustively, and keep a few slower tests for the real integrations.",
        Concept = "Code that calls databases, payment providers and the system clock directly can only be tested slowly and partially. Pass those dependencies in as trait objects instead: production gets the real implementations, tests get simple fakes that record what happened and can be told to fail. Then every failure path, including the compensating actions after a partial failure, gets a fast, deterministic test.",
        Body = """
            ## What makes code hard to test
            Imagine a checkout function that calls the inventory database, the payment provider's HTTP API and `SystemTime::now()` directly. Testing it needs a database, a sandbox payment account and control over time. The tests are slow, flaky, and almost never cover the interesting cases: the card is declined *after* stock was reserved; the second of three items is out of stock. Those are exactly the cases that lose money in production.

            ## Boundaries as traits
            Draw a line around each thing outside your code's control and give it a small trait describing what your code needs from it:

            ```rust
            trait Inventory {
                fn reserve(&mut self, sku: &str, quantity: u32) -> bool;
                fn release(&mut self, sku: &str, quantity: u32);
            }

            trait Payments {
                fn charge(&mut self, cents: u64) -> Result<String, String>;
            }

            trait Clock {
                fn now(&self) -> u64;
            }
            ```

            The business logic takes them as parameters (`&mut dyn Inventory`, or generics) instead of reaching for globals. This is **dependency injection**, and in Rust it's just passing trait objects; no framework required. Design each trait from the caller's point of view and keep it small: "reserve stock" rather than "run this SQL".

            ## Fakes that remember and misbehave
            A test implementation of a trait (a **fake**) is a few lines: a `HashMap` of stock, a list of charges, a flag that makes the next payment fail, a clock that returns a fixed time. Fakes can also **record** every call, so a test can assert not just the result but the sequence of side effects:

            ```rust
            assert_eq!(stock.log, ["reserve mug 3", "release mug 3"]);
            ```

            That's how you prove the compensating action happened: after a declined payment, reserved stock must be released, or it leaks until someone notices the shop is "out of stock" of things in the warehouse.

            Prefer hand-written fakes for small traits. Mocking libraries like `mockall` generate them, which helps for large interfaces, but tests that assert every call in detail break whenever the implementation changes harmlessly; assert the outcomes and side effects that matter.

            ## Partial failure and compensation
            Multi-step operations across systems can't use a database transaction: you can't roll back a charge sent to a payment provider. Instead, order the steps so failures are cheap to undo, and write explicit compensation:

            1. Reserve each item. If one fails, release the ones already reserved.
            2. Charge the total. If it's declined, release everything.
            3. Only then report success.

            Each arrow out of that sequence is a test. The pattern scales up to **sagas** in distributed systems, where each step has a compensating step.

            Time is a dependency too. Code that calls the real clock can't be tested for "expires after 24 hours" without waiting 24 hours. Injecting a `Clock` makes it a one-line test.

            ## Which tests at which boundary
            - **Unit tests with fakes** for business rules and every failure path: milliseconds each, so have many.
            - **Contract tests** that run the *same* test suite against the fake and the real implementation (a real SQLite database, a payment provider's sandbox), proving the fake behaves like the real thing. Without them, fakes drift and tests pass while production fails.
            - **Integration tests** in `tests/` that start the real service, with a real database in a container (the `testcontainers` crate) or a temporary SQLite file, and exercise HTTP endpoints end to end. Fewer, slower, and they catch wiring mistakes.
            - **A handful of end-to-end tests** over the most important user journeys.

            This shape, many fast tests and a few slow ones, is often drawn as a pyramid. The point isn't the shape, it's that each test is placed at the cheapest boundary that can catch the bug it's for.

            ## Common mistakes
            - Business logic that calls real services directly, leaving failure paths untested.
            - Fakes that only ever succeed.
            - Asserting only the return value, not the side effects (released stock, nothing charged).
            - Fakes nobody checks against the real implementation.
            - Calling the real clock inside logic that depends on time.

            ## Try it locally
            1. Write `checkout` against the real `std::time::SystemTime` and try to test "receipts are stamped with the current time". Then inject a `Clock`.
            2. Write a contract test function `fn inventory_contract(inv: &mut dyn Inventory)` and run it against your fake and a SQLite-backed implementation.
            3. Add an integration test in `tests/` that starts the Axum service from the previous module and makes a real HTTP request with `reqwest`.
            """,
        Example = """
            struct FixedClock(u64);

            impl Clock for FixedClock {
                fn now(&self) -> u64 {
                    self.0
                }
            }

            let mut payments = FakePayments { decline: true, charged: Vec::new() };
            let result = checkout(&lines, &mut stock, &mut payments, &FixedClock(1_700_000_000));
            assert!(matches!(result, Err(CheckoutError::PaymentDeclined(_))));
            assert_eq!(stock.stock["mug"], 5); // released again
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Write checkout logic that survives partial failure",
            "Implement `checkout` against injected dependencies. The grader provides the traits, a `Line` type, and fakes that record calls and can fail, then checks every success and failure path. Don't write a `main` function or redefine the provided items.",
            [
                "The grader provides `trait Inventory { fn reserve(&mut self, sku: &str, quantity: u32) -> bool; fn release(&mut self, sku: &str, quantity: u32); }`, `trait Payments { fn charge(&mut self, cents: u64) -> Result<String, String>; }`, `trait Clock { fn now(&self) -> u64; }` and `struct Line { sku: String, quantity: u32, unit_cents: u64 }`",
                "Define `#[derive(Debug, PartialEq)] struct Receipt { payment_id: String, total_cents: u64, at: u64 }` and `#[derive(Debug, PartialEq)] enum CheckoutError { EmptyCart, OutOfStock(String), PaymentDeclined(String) }`",
                "`fn checkout(lines: &[Line], inventory: &mut dyn Inventory, payments: &mut dyn Payments, clock: &dyn Clock) -> Result<Receipt, CheckoutError>`: an empty cart touches nothing; reserve every line, releasing earlier reservations if one fails; charge the total once; release everything if the payment is declined; stamp the receipt with `clock.now()`",
            ],
            """
            // The grader provides: trait Inventory, trait Payments, trait Clock, struct Line.

            #[derive(Debug, PartialEq)]
            struct Receipt {
                payment_id: String,
                total_cents: u64,
                at: u64,
            }

            #[derive(Debug, PartialEq)]
            enum CheckoutError {
                EmptyCart,
                OutOfStock(String),
                PaymentDeclined(String),
            }

            fn checkout(lines: &[Line], inventory: &mut dyn Inventory, payments: &mut dyn Payments, clock: &dyn Clock) -> Result<Receipt, CheckoutError> {
                todo!()
            }
            """,
            null,
            [],
            "Keep a `Vec` of the lines reserved so far; when a reservation fails, release each of them before returning `OutOfStock(sku)`. Compute the total as the sum of `quantity as u64 * unit_cents`. On `Err(reason)` from `charge`, release every line and return `PaymentDeclined(reason)`.",
            ["A successful checkout charges and stamps the time", "Empty carts touch nothing", "A shortage releases earlier reservations", "A declined payment releases stock"]),
    };

    private static Lesson RustObservability(Lesson lesson) => lesson with
    {
        Title = "Metrics, logs, and traces",
        Subtitle = "Instrument a service so you can tell, from the outside, whether users are succeeding: request metrics with bounded labels, histograms for latency, structured logs and traces, and alerts on what users feel.",
        Concept = "Metrics are cheap numeric time series (counts, durations) aggregated over all requests; logs are detailed records of individual events; traces follow one request across services. Record request counts and latency histograms in one middleware, label them by route template rather than raw URL so the number of series stays bounded, and alert on symptoms users experience, such as error rate and latency against an objective.",
        Body = """
            ## Three kinds of signal
            When a service misbehaves at 3 a.m., the people on call can only see what it emits. Three kinds of telemetry answer different questions:

            - **Metrics**: numbers aggregated over time. "How many requests per second? What fraction failed? How slow was the 99th percentile?" Cheap to store and query, so they drive dashboards and alerts.
            - **Logs**: one record per event, with detail. "What happened to *this* request? Which validation failed?"
            - **Traces**: a request's path through every service and database call, with timings for each step. "Where did these 800 milliseconds go?"

            ## Metrics from a middleware
            Every route should be measured the same way, so measure in one place: **middleware**, code that wraps every handler. In Axum:

            ```rust
            async fn track(State(metrics): State<Arc<Metrics>>, request: Request, next: Next) -> Response {
                let route = request.extensions().get::<MatchedPath>()
                    .map(|path| path.as_str().to_string())
                    .unwrap_or_else(|| String::from("unmatched"));
                let started = Instant::now();
                let response = next.run(request).await;   // run the handler
                metrics.record(&route, response.status().as_u16(), started.elapsed().as_millis() as u64);
                response
            }

            let app = Router::new()
                .route("/courses/{id}", get(get_course))
                .layer(axum::middleware::from_fn_with_state(metrics.clone(), track));
            ```

            ## Labels and cardinality
            Each distinct combination of label values is a separate **time series** that the monitoring system stores. Labelling by the raw URL (`/courses/1`, `/courses/2`, ...) creates one series per id: millions of series, a monitoring bill to match, and dashboards nobody can read. This **cardinality explosion** is the most common metrics mistake. Label by the **route template** instead (`/courses/{id}`), which `MatchedPath` provides, and group unmatched requests under one label. Never use user ids, emails or request ids as metric labels; those belong in logs and traces.

            ## Counters and histograms
            The **Prometheus** text format is the most widely used way to expose metrics: the monitoring system scrapes a `/metrics` endpoint every few seconds.

            ```text
            # TYPE http_requests_total counter
            http_requests_total{route="/courses/{id}",status="200"} 3
            # TYPE http_request_duration_ms histogram
            http_request_duration_ms_bucket{le="5"} 1
            http_request_duration_ms_bucket{le="25"} 3
            http_request_duration_ms_bucket{le="+Inf"} 5
            http_request_duration_ms_sum 9443
            http_request_duration_ms_count 5
            ```

            - A **counter** only goes up. The monitoring system computes rates from it ("requests per second over the last five minutes").
            - A **histogram** counts observations into **buckets**. Prometheus buckets are **cumulative**: `le="25"` counts every request that took 25 ms *or less*, including those already counted in `le="5"`. The `+Inf` bucket equals the total count. Percentiles are then estimated from the buckets across all instances, which is why averages of per-instance percentiles are never computed.

            The `prometheus` and `metrics` crates implement this properly (with atomic counters rather than a mutex), and **OpenTelemetry** is the vendor-neutral standard for exporting all three signal types.

            ## Structured logs and traces
            Logs should be **structured**: key-value fields, usually emitted as JSON, rather than sentences, so they can be searched and aggregated (`route="/checkout" status=502 duration_ms=812`). In Rust the standard is the **tracing** crate: `tracing::info!(order_id, amount, "payment captured")`. Its **spans** mark units of work (a request, a database query) with timing and fields, and `tracing-opentelemetry` exports them as distributed traces. A **request id** propagated through headers ties logs and traces from different services together.

            Never log secrets, tokens, passwords or full personal data; redact the `Authorization` header in request logging.

            ## What to alert on
            Alert on **symptoms users feel**, not on every internal cause:

            - The **RED** method for request-driven services: **R**ate, **E**rrors, **D**uration.
            - **SLOs** (service level objectives): "99.9% of requests succeed and 99% complete under 300 ms, over 30 days". The remaining 0.1% is the **error budget**. Alert when the budget is being consumed fast enough to run out (a **burn-rate** alert), which catches real incidents early without paging on every blip.
            - Every alert needs an owner and a runbook entry: what it means, and the first things to check.

            High CPU or a full queue are causes worth graphing; they page someone only when users are affected.

            ## Common mistakes
            - Raw URLs, user ids or other unbounded values as metric labels.
            - Non-cumulative histogram buckets, or averaging percentiles.
            - Free-text logs that can't be queried.
            - Alerting on causes, so the on-call engineer is paged constantly for things users never notice.
            - Logging secrets.

            ## Try it locally
            1. Add a `/metrics` route that returns `metrics.render()`, run Prometheus in Docker scraping it, and graph `rate(http_requests_total[1m])`.
            2. Add `tracing-subscriber` with JSON output to the service and log one structured event per request.
            3. Write down an SLO for one endpoint and the burn-rate alert you'd set for it.
            """,
        Example = """
            async fn track(State(metrics): State<Arc<Metrics>>, request: Request, next: Next) -> Response {
                let route = request.extensions().get::<MatchedPath>()
                    .map(|p| p.as_str().to_string())
                    .unwrap_or_else(|| String::from("unmatched"));
                let started = Instant::now();
                let response = next.run(request).await;
                metrics.record(&route, response.status().as_u16(), started.elapsed().as_millis() as u64);
                response
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Measure every request with bounded labels",
            "Write a `Metrics` type that renders Prometheus text, and the middleware that feeds it. The grader layers your middleware on a router, sends requests to templated, fixed and unknown paths, then records chosen durations directly to check the histogram. axum is available. Don't write a `main` function.",
            [
                "`#[derive(Default)] struct Metrics` with `fn record(&self, route: &str, status: u16, duration_ms: u64)` and `fn render(&self) -> String`, safe to share between requests",
                "`render` includes, one per line, `http_requests_total{route=\"<route>\",status=\"<status>\"} <count>` for each route and status, histogram buckets `http_request_duration_ms_bucket{le=\"<bound>\"} <count>` for bounds 5, 25, 100, 500, 2500 and `+Inf` (cumulative), plus `http_request_duration_ms_sum` and `http_request_duration_ms_count`",
                "`async fn track(State(metrics): State<Arc<Metrics>>, request: Request, next: Next) -> Response` labels by the matched route template (`/courses/{id}`), or `unmatched` when no route matched, and records status and duration",
            ],
            """
            use std::collections::BTreeMap;
            use std::sync::{Arc, Mutex};
            use std::time::Instant;

            use axum::extract::{MatchedPath, Request, State};
            use axum::middleware::Next;
            use axum::response::Response;

            #[derive(Default)]
            struct Metrics {
                // Your fields, e.g. Mutex-protected counts and buckets.
            }

            impl Metrics {
                fn record(&self, route: &str, status: u16, duration_ms: u64) {
                    todo!()
                }

                fn render(&self) -> String {
                    todo!()
                }
            }

            async fn track(State(metrics): State<Arc<Metrics>>, request: Request, next: Next) -> Response {
                todo!()
            }
            """,
            null,
            [],
            "Store counts in a `Mutex<BTreeMap<(String, u16), u64>>` (sorted output for free) and bucket counts in an array, adding 1 to every bucket whose bound is at least the duration. Inside `format!`, write literal braces as `{{` and `}}`. Get the route with `request.extensions().get::<MatchedPath>()` before calling `next.run(request)`.",
            ["Labels use route templates", "Histogram buckets are cumulative", "Every request is counted and timed"]),
    };

    private static Lesson RustPerformanceProfiling(Lesson lesson) => lesson with
    {
        Title = "Latency, percentiles, and capacity",
        Subtitle = "Describe service performance the way users experience it, with percentiles instead of averages, size capacity with Little's law, and remove the N+1 query pattern that dominates slow services.",
        Concept = "Users experience the slow requests, so latency is described by percentiles (p50, p95, p99), not averages. Little's law links throughput and latency to the concurrency a service needs: requests in flight equals arrival rate times time per request. In data-backed services, the most common slowdown is one query per item instead of one query for all items.",
        Body = """
            ## Why averages lie
            Ten requests take 20 ms and one takes 2 seconds: the average is about 200 ms, which describes none of them. Latency distributions are skewed, with a long tail, and the tail is where users get frustrated. A page that makes 50 backend calls is slow whenever *any* of them is, so even a 1% tail touches most page loads.

            **Percentiles** describe the distribution: p50 (the median) is the typical request; p95 and p99 describe the slow tail; the maximum is often noise. Report several, and set objectives on the tail: "p99 under 300 ms".

            ## Computing a percentile
            The **nearest-rank** method is the simplest exact definition: sort the samples, then p-th percentile = the value at position `ceil(p / 100 × n)`, counting from 1. For 100 samples, p99 is the 99th smallest; for 5 samples, p90 is `ceil(4.5)` = the 5th, the maximum. Other methods interpolate between neighbours; whichever you use, use it consistently.

            In production you can't keep every sample. Histograms (the observability lesson) approximate percentiles from bucket counts; algorithms like HDR Histogram and t-digest track them precisely in fixed memory. Never average percentiles across servers: the average of two p99s isn't the p99 of the combined traffic. Merge the histograms instead.

            ## Little's law
            A beautifully simple rule from queueing theory holds for any stable system:

            **requests in progress = arrival rate × time each request spends in the system**

            At 200 requests per second, each taking 50 ms (0.05 s), on average 200 × 0.05 = **10** requests are in flight at any moment. So the service needs at least 10 concurrent workers, database connections or tasks, or requests queue up. Round up, since you can't have half a worker. Read it the other way round too: a pool of 20 database connections and queries that take 100 ms caps the database work at 200 queries per second, whatever the CPUs could do.

            The law also explains why latency explodes near capacity. As the arrival rate approaches what the workers can handle, queues grow, each request waits longer, and more are in flight, which is why services are run with headroom (often under 70–80% utilisation) and why load tests push past the expected peak to find the cliff.

            ## The N+1 query
            The most common performance bug in data-backed services: load a list, then make one more query per item.

            ```rust
            // 1 query for the list, then 1 per id: N+1 round trips
            let titles: Vec<String> = ids.iter().filter_map(|id| db.get(*id)).collect();
            ```

            Each query is a network round trip, maybe a millisecond or two, plus database work. For 100 items that's 100 round trips, invisible in development with a local database and ten rows, and a disaster in production. The fix is to fetch everything in **one** query (`WHERE id IN (...)` or `= ANY($1)`) and reassemble the results in the order requested, since databases don't promise any particular order:

            ```rust
            let found: HashMap<u64, String> = db.get_many(ids).into_iter().collect();
            let titles: Vec<String> = ids.iter().filter_map(|id| found.get(id).cloned()).collect();
            ```

            Tracing (one span per query) makes N+1 obvious: a request with 101 identical database spans. GraphQL servers address the same problem with a **DataLoader** that batches lookups automatically.

            ## Load testing
            Find out where the cliff is before users do. Tools such as **k6**, **oha** and **wrk** generate load and report percentiles. Test with realistic data volumes, ramp the rate up until latency or errors break your objective, and record that number: it's your capacity. Measure with production-like resources; a laptop result says little about a 1-vCPU container.

            Two traps: **coordinated omission** (a load generator that waits for each response before sending the next under-reports latency, because it stops sending exactly when the service is slow; use open-model tools that send at a fixed rate), and testing only the happy path while production traffic includes slow queries and cache misses.

            ## Common mistakes
            - Reporting average latency, or averaging percentiles.
            - Sizing pools and concurrency limits by guesswork instead of Little's law.
            - One query per item in a loop.
            - Assuming the database returns rows in the order requested.
            - Load tests that never reach saturation.

            ## Try it locally
            1. Record the latency of 1,000 requests to your Axum service, compute p50, p95 and p99, and compare with the mean.
            2. Add a 2 ms sleep to a fake `get` and time `load_titles` with N+1 versus batched for 500 ids.
            3. Run `oha -z 30s -q 500 http://127.0.0.1:3000/health` and increase the rate until p99 jumps.
            """,
        Example = """
            fn percentile(samples: &mut [u64], p: f64) -> Option<u64> {
                if samples.is_empty() || !(0.0..=100.0).contains(&p) {
                    return None;
                }
                samples.sort_unstable();
                let rank = ((p / 100.0) * samples.len() as f64).ceil() as usize;
                Some(samples[rank.max(1) - 1]) // nearest rank, counting from 1
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Measure the tail, size capacity, and remove an N+1",
            "Write three functions. The grader provides `trait CourseDb { fn get(&self, id: u64) -> Option<String>; fn get_many(&self, ids: &[u64]) -> Vec<(u64, String)>; }` with a fake that counts queries and returns rows out of order. Don't write a `main` function.",
            [
                "`fn percentile(samples: &mut [u64], p: f64) -> Option<u64>` returns the nearest-rank percentile (sorting the samples), `None` for no samples or `p` outside 0–100; p0 is the minimum",
                "`fn required_workers(requests_per_second: f64, latency_ms: f64) -> u64` applies Little's law, rounding up",
                "`fn load_titles(ids: &[u64], db: &dyn CourseDb) -> Vec<String>` returns the titles that exist, in the order of `ids`, using exactly one `get_many` call (and no call at all for no ids)",
            ],
            """
            // The grader provides: trait CourseDb { fn get(&self, id: u64) -> Option<String>; fn get_many(&self, ids: &[u64]) -> Vec<(u64, String)>; }

            fn percentile(samples: &mut [u64], p: f64) -> Option<u64> {
                todo!()
            }

            fn required_workers(requests_per_second: f64, latency_ms: f64) -> u64 {
                todo!()
            }

            fn load_titles(ids: &[u64], db: &dyn CourseDb) -> Vec<String> {
                todo!()
            }
            """,
            null,
            [],
            "Nearest rank is `ceil(p / 100 * n)`, used as a 1-based position, with `max(1)` so p0 returns the first value. Little's law: `(rps * latency_ms / 1000.0).ceil()`. For titles, return early on empty input, collect `get_many` into a `HashMap<u64, String>`, then map over `ids` in order.",
            ["Percentiles use nearest rank", "Capacity follows Little's law", "Titles load in one query, in order"]),
    };

    private static Lesson RustResilienceQueues(Lesson lesson) => lesson with
    {
        Title = "Retries, circuit breakers, and queues",
        Subtitle = "Survive failing dependencies without making them worse: retry with capped exponential backoff and jitter, stop calling a dependency that's down, and process queued work exactly once in effect, with a dead-letter queue for what can't be processed.",
        Concept = "Retries recover from brief failures but can multiply load on a struggling dependency, so they need exponential backoff, a cap, randomness (jitter) and a limit. A circuit breaker stops calling a dependency that keeps failing and probes it carefully before resuming. Message queues deliver at least once, so consumers must be idempotent, and messages that keep failing go to a dead-letter queue instead of blocking everything behind them.",
        Body = """
            ## Failure is normal
            In a system of many services, something is always slow or failing: a deploy, a network blip, an overloaded database. Resilience patterns decide whether that becomes a brief hiccup or a cascading outage.

            ## Retries, done carefully
            Retrying a failed call recovers from **transient** failures: a dropped connection, a 503 during a deploy. Retrying blindly does harm:

            - Immediate retries hit a struggling service again at once, adding load exactly when it can least handle it. If every client retries three times, a dependency at the edge of capacity suddenly receives four times the traffic: a **retry storm**.
            - Retrying non-idempotent operations can do them twice (the API-contracts lesson's idempotency keys exist for this).
            - Some errors will never succeed on retry: 400, 401, 404, validation failures. Retry only what can be transient: timeouts, connection errors, 429, 502, 503, 504.

            **Exponential backoff** waits longer after each failure: 100 ms, 200 ms, 400 ms, 800 ms... up to a **cap**, so waits don't grow without bound. **Jitter** randomises each wait. Without it, clients that failed at the same moment retry at the same moments, in synchronised waves. With "full jitter", each wait is a random amount between zero and the current ceiling:

            ```rust
            fn backoff_ms(attempt: u32, base_ms: u64, cap_ms: u64, jitter: f64) -> u64 {
                let exponential = base_ms.checked_shl(attempt)
                    .filter(|value| value >> attempt == base_ms) // detect bits shifted out
                    .unwrap_or(u64::MAX);
                (exponential.min(cap_ms) as f64 * jitter) as u64   // jitter: random in [0, 1)
            }
            ```

            Note the overflow care: `base * 2^attempt` overflows for large attempts, and a backoff function must saturate at the cap rather than wrap to a tiny delay or panic. Taking the jitter as a parameter, rather than calling a random number generator inside, keeps the function deterministic and testable.

            Also cap the **number** of attempts, and the total time: a request with a 2-second deadline shouldn't start a retry that can't finish in time. Libraries: `backoff`, `tokio-retry`, and `tower`'s retry middleware with budgets that limit retries to a fraction of normal traffic.

            ## Circuit breakers
            When a dependency is down, calling it anyway wastes time (each call waits for a timeout), holds resources, and keeps the dependency from recovering. A **circuit breaker**, named after the electrical device, sits in front of the calls:

            - **Closed** (normal): calls go through. Consecutive failures are counted; a success resets the count.
            - **Open**: after a threshold of failures, calls **fail fast** without being attempted, for a cooldown period. Callers can fall back: serve cached data, a default, or a clear error.
            - **Half-open**: after the cooldown, **one** trial call is allowed. Success closes the breaker; failure reopens it and restarts the cooldown. Allowing every caller through while half-open would hit a recovering service with a full flood.

            The breaker is a small state machine, and modelling it as an enum makes every transition explicit and testable. Pass the current time in, as with the clock in the testing lesson.

            ## Queues and at-least-once delivery
            A **message queue** (RabbitMQ, SQS, Kafka, NATS, or a database table used as a queue) decouples a producer from slow or unreliable work: send the welcome email *later*, outside the request. Queues usually promise **at-least-once** delivery: a message is redelivered until the consumer acknowledges it. If the consumer crashes after doing the work but before acknowledging, the message comes again.

            So consumers must be **idempotent**: processing the same message twice must have the effect of processing it once. The usual technique is to give every message an id and record processed ids (in the same database transaction as the work's effects, when possible), skipping duplicates. "Exactly-once" processing is this: at-least-once delivery plus idempotent handling.

            ## Poison messages and dead letters
            Some messages will never succeed: malformed payloads, references to deleted data, a bug. Retried forever, a **poison message** blocks the queue or burns resources. After a maximum number of attempts, move it to a **dead-letter queue** (DLQ) with its last error, alert someone, and continue with the next message. Once the cause is fixed, the dead-lettered messages can be replayed.

            ## Common mistakes
            - Retrying immediately, forever, or for errors that can't succeed.
            - Backoff without jitter, or arithmetic that overflows for large attempt counts.
            - No circuit breaker, so a dead dependency ties up every worker in timeouts.
            - A half-open breaker that lets every caller through.
            - Consumers that assume each message arrives exactly once.
            - No dead-letter queue, so one bad message stalls everything.

            ## Try it locally
            1. Simulate 1,000 clients retrying a failure at the same instant with and without jitter, and plot the retry times.
            2. Put a circuit breaker in front of an HTTP call to a local server, stop the server, and watch calls fail fast; restart it and watch the half-open trial.
            3. Deliver the same message twice to a consumer that writes to SQLite, and make it idempotent with a processed-ids table in the same transaction.
            """,
        Example = """
            #[derive(Debug, PartialEq, Clone, Copy)]
            enum BreakerState {
                Closed,
                Open,
                HalfOpen,
            }

            // allow(now): Closed -> true; Open -> false until the cooldown passes,
            // then become HalfOpen and allow exactly one trial; HalfOpen -> false.
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Back off, break the circuit, and dead-letter poison messages",
            "Implement three resilience building blocks. They take time and randomness as parameters, so the grader can check exact behaviour. Don't write a `main` function.",
            [
                "`fn backoff_ms(attempt: u32, base_ms: u64, cap_ms: u64, jitter: f64) -> u64` returns `min(cap, base × 2^attempt) × jitter`, saturating at the cap for any attempt number without overflowing",
                "`struct CircuitBreaker` with `fn new(threshold: u32, cooldown_ms: u64) -> CircuitBreaker`, `fn state(&self) -> BreakerState` (`#[derive(Debug, PartialEq, Clone, Copy)] enum BreakerState { Closed, Open, HalfOpen }`), `fn allow(&mut self, now_ms: u64) -> bool`, `fn record_success(&mut self)` and `fn record_failure(&mut self, now_ms: u64)`: opens after `threshold` consecutive failures, fails fast during the cooldown, then allows one half-open trial whose result closes or reopens it",
                "`struct Consumer` with `fn new(max_attempts: u32) -> Consumer`, a public `dead_letters: Vec<String>`, and `fn deliver(&mut self, id: &str, handler: &mut dyn FnMut() -> Result<(), String>) -> Outcome` where `#[derive(Debug, PartialEq)] enum Outcome { Delivered { attempts: u32 }, DeadLettered { attempts: u32, last_error: String }, Duplicate }`: an id already delivered is a `Duplicate` and the handler isn't called; a dead-lettered id can be delivered again later",
            ],
            """
            use std::collections::HashSet;

            fn backoff_ms(attempt: u32, base_ms: u64, cap_ms: u64, jitter: f64) -> u64 {
                todo!()
            }

            #[derive(Debug, PartialEq, Clone, Copy)]
            enum BreakerState {
                Closed,
                Open,
                HalfOpen,
            }

            struct CircuitBreaker {
                // Your fields.
            }

            #[derive(Debug, PartialEq)]
            enum Outcome {
                Delivered { attempts: u32 },
                DeadLettered { attempts: u32, last_error: String },
                Duplicate,
            }

            struct Consumer {
                seen: HashSet<String>,
                max_attempts: u32,
                dead_letters: Vec<String>,
            }
            """,
            null,
            [],
            "For backoff, `base_ms.checked_shl(attempt)` returns `None` only for shifts of 64 or more; also check `value >> attempt == base_ms` to catch bits shifted out, and fall back to `u64::MAX` before taking `min(cap_ms)`. In the breaker, a failure while half-open reopens immediately. In the consumer, remember an id only after a successful delivery.",
            ["Backoff is capped, jittered and overflow-safe", "The breaker follows closed, open and half-open", "Duplicates are skipped and poison messages dead-lettered"]),
    };

    private static Lesson RustContainersDelivery(Lesson lesson) => lesson with
    {
        Title = "Configure, package, and shut down safely",
        Subtitle = "Ship a service that fails fast on bad configuration, tells the platform when it's alive and when it's ready, builds into a small container, and finishes in-flight requests when it's asked to stop.",
        Concept = "Configuration comes from the environment and is validated completely at startup, reporting every problem without revealing secrets. Liveness says the process is working; readiness says it should receive traffic now. On shutdown a service stops being ready, stops accepting connections, and lets in-flight requests finish before exiting, so deploys don't drop requests.",
        Body = """
            ## Configuration
            Following the **Twelve-Factor App** practice, a service reads its configuration (ports, database URLs, feature switches, credentials) from **environment variables** set by the platform, so the same build runs in every environment. Two rules make that safe:

            - **Validate everything at startup and fail fast.** A typo in `DATABASE_URL` should stop the deploy in seconds, not surface as an error on the first request an hour later. Parse the environment into a typed `Config` struct once, in `main`, and pass it on; nothing else reads environment variables.
            - **Report every problem at once.** Collecting all errors into a list saves the operator a deploy-fail-fix loop per variable.

            And never include **secret values** in error messages or logs: database URLs contain passwords. Say "DATABASE_URL is invalid", not what it was. Wrap secrets in a type that prints as `[redacted]` (the `secrecy` crate) so they can't leak through a stray `{:?}`.

            Testing gets easier if the loader takes the variables as a map rather than reading the process environment directly: `fn load_config(env: &HashMap<String, String>)`, called from `main` with `std::env::vars().collect()`.

            ## Liveness and readiness
            Platforms like Kubernetes, Fly.io and load balancers probe two endpoints, and they mean different things:

            - **Liveness** (`/livez`): is the process healthy? If not, *restart* it. It should be trivial, and must not check dependencies: if the database is down, restarting every instance won't fix it and makes things worse.
            - **Readiness** (`/readyz`): should this instance receive traffic *right now*? It's false until startup work is done (migrations run, caches warm, connection pool open) and false again while shutting down. A failing readiness check takes the instance out of the load balancer without restarting it.

            Both return status codes, 200 or 503; the body is for humans.

            ## Graceful shutdown
            Deploys, scaling and node maintenance stop instances constantly. The platform sends **SIGTERM**, waits a grace period (30 seconds is common), then sends SIGKILL. A service that exits immediately on SIGTERM drops every in-flight request: users see errors on every deploy.

            The graceful sequence:

            1. Receive the signal (`tokio::signal::ctrl_c()` or `tokio::signal::unix::signal(SignalKind::terminate())`).
            2. Mark the instance **draining**, so readiness turns false and the load balancer stops sending new requests.
            3. Stop accepting new connections.
            4. Let in-flight requests finish, bounded by a deadline shorter than the grace period.
            5. Flush logs and metrics, close database pools, exit.

            Axum supports steps 3 and 4 directly:

            ```rust
            axum::serve(listener, app)
                .with_graceful_shutdown(async move {
                    let _ = shutdown.await;          // a signal, or a channel in tests
                    health.draining.store(true, Ordering::SeqCst);
                })
                .await
            ```

            When the future passed to `with_graceful_shutdown` completes, the server stops accepting connections and `.await` returns only after active connections finish. Compare racing the server against the signal with `select!`: it returns immediately, `main` carries on to exit, and the runtime shutting down cuts off every request still in progress. (In practice also wait a few seconds after marking unready before stopping the listener, because load balancers notice readiness changes with a delay.)

            ## The container image
            A Rust service compiles to a single binary, which makes for small, simple container images. A **multi-stage** Dockerfile builds in one image and copies only the binary into a minimal runtime image:

            ```dockerfile
            FROM rust:1.97 AS build
            WORKDIR /src
            COPY . .
            RUN cargo build --release --locked

            FROM gcr.io/distroless/cc-debian12:nonroot
            COPY --from=build /src/target/release/learner-api /app
            USER nonroot
            ENTRYPOINT ["/app"]
            ```

            - `--locked` builds exactly the `Cargo.lock` versions (the Cargo lesson).
            - The runtime image has no shell or package manager: less to patch and less for an attacker to use. Static musl binaries can even run `FROM scratch`.
            - Run as a **non-root** user.
            - Cache dependency builds by copying `Cargo.toml` and `Cargo.lock` and building before copying the source (`cargo-chef` automates this), so code changes don't rebuild every crate.
            - Scan images for known vulnerabilities in CI, and tag them by commit, never only `latest`.

            ## Rolling out safely
            Even a good build can be a bad release. Roll out gradually (a **canary** or a percentage of instances), watch the error-rate and latency SLOs from the observability lesson, and have a **rollback** that takes one command. Database migrations must work with both the old and new versions running at once: add columns before code uses them, remove them only after no running code does (the **expand-and-contract** pattern).

            ## Common mistakes
            - Reading environment variables all over the code, or lazily on first use.
            - Echoing secrets in configuration errors.
            - A liveness check that depends on the database.
            - Exiting immediately on SIGTERM.
            - Large images built and run as root.

            ## Try it locally
            1. Build the multi-stage image, compare its size with a single-stage one, and run it with `docker run -e PORT=8080 ...`.
            2. Start the service, run a load generator, and send SIGTERM; count failed requests with and without graceful shutdown.
            3. Write the Kubernetes probe configuration for `/livez` and `/readyz` with sensible periods and thresholds.
            """,
        Example = """
            async fn serve_until(listener: TcpListener, app: Router, health: Arc<Health>, shutdown: oneshot::Receiver<()>) -> io::Result<()> {
                axum::serve(listener, app)
                    .with_graceful_shutdown(async move {
                        let _ = shutdown.await;
                        health.draining.store(true, Ordering::SeqCst); // readiness turns false
                    })
                    .await // returns after in-flight requests finish
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Validate configuration, report health, and drain on shutdown",
            "Write a configuration loader, health endpoints and a graceful server loop. The grader checks configuration errors (including that secrets never appear), flips readiness, and shuts the server down while a 300 ms request is in progress. axum and Tokio are available. Don't write a `main` function.",
            [
                "`#[derive(Debug, PartialEq)] struct Config { port: u16, database_url: String, log_level: String }` and `fn load_config(env: &HashMap<String, String>) -> Result<Config, Vec<String>>`: `PORT` required (1–65535), `DATABASE_URL` required and non-empty, `LOG_LEVEL` one of error, warn, info, debug, trace (default info); every problem is reported, each naming its variable, and no message contains a variable's value",
                "`struct Health { ready: AtomicBool, draining: AtomicBool }` and `fn app(health: Arc<Health>) -> Router` with `GET /livez` always 200 and `GET /readyz` 200 only when ready and not draining, otherwise 503",
                "`async fn serve_until(listener: tokio::net::TcpListener, app: Router, health: Arc<Health>, shutdown: tokio::sync::oneshot::Receiver<()>) -> std::io::Result<()>` serves until the shutdown signal, then marks the service draining and lets in-flight requests complete before returning",
            ],
            """
            use std::collections::HashMap;
            use std::sync::Arc;
            use std::sync::atomic::{AtomicBool, Ordering};

            use axum::extract::State;
            use axum::http::StatusCode;
            use axum::routing::get;
            use axum::Router;

            #[derive(Debug, PartialEq)]
            struct Config {
                port: u16,
                database_url: String,
                log_level: String,
            }

            struct Health {
                ready: AtomicBool,
                draining: AtomicBool,
            }

            fn load_config(env: &HashMap<String, String>) -> Result<Config, Vec<String>> {
                todo!()
            }

            fn app(health: Arc<Health>) -> Router {
                todo!()
            }

            async fn serve_until(listener: tokio::net::TcpListener, app: Router, health: Arc<Health>, shutdown: tokio::sync::oneshot::Receiver<()>) -> std::io::Result<()> {
                todo!()
            }
            """,
            null,
            [],
            "Collect problems in a `Vec<String>` while parsing each variable, and only build the `Config` when the list is empty. For readiness, load both flags with `Ordering::SeqCst`. For shutdown, `axum::serve(listener, app).with_graceful_shutdown(async move { ... }).await`, setting `draining` inside the future after `shutdown.await`.",
            ["Configuration errors are complete", "Secrets never appear in errors", "Liveness and readiness are distinct", "In-flight requests finish on shutdown"]),
    };
}
