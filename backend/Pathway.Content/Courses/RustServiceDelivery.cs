namespace Pathway.Content;

static partial class Curriculum
{
    // Authored lessons for the "Service delivery" module: Axum routing, API contracts, error
    // mapping, database transactions with SQLite, and authorization. Graders use the image's
    // prebuilt axum, tower, serde_json and rusqlite through pathway-rustc, and call the router
    // in memory with tower's `oneshot`, so no network is needed.

    private static Lesson RustWebAxum(Lesson lesson) => lesson with
    {
        Subtitle = "Build an HTTP service on the networking module's foundations: routes, handlers, extractors and responses, kept thin around ordinary Rust.",
        Concept = "Axum maps a method and path to an async handler function. Extractors in the handler's parameters (Path, Query, Json, State) parse the request, and anything that implements IntoResponse becomes the response. Handlers stay thin: HTTP details at the edge, business rules in ordinary functions that don't know about HTTP.",
        Body = """
            ## What a web framework adds
            The networking module built servers from sockets. HTTP adds a standard request shape on top of TCP: a **method** (GET, POST, PUT, DELETE...), a **path**, **headers**, and an optional **body**; and a response with a **status code**, headers and a body. Writing an HTTP parser by hand is the framing lesson's problem with far more edge cases. **Axum**, built on Tokio and the Hyper HTTP library, does that work and leaves you to write handlers.

            ## Routes and handlers
            ```rust
            use axum::{Router, routing::{get, post}};

            fn app() -> Router {
                Router::new()
                    .route("/health", get(health))
                    .route("/courses/{id}", get(get_course))
                    .route("/echo", post(echo))
            }

            async fn health() -> &'static str {
                "ok"
            }
            ```

            A **handler** is an ordinary async function. `{id}` in a path is a parameter. One path can have several methods: `get(list).post(create)`. A request to a known path with a method that has no handler gets **405 Method Not Allowed**; an unknown path gets **404**. Axum does both for you.

            To serve it for real, bind a Tokio listener and hand it the router, which is the networking module's accept loop, written once by the framework:

            ```rust
            let listener = tokio::net::TcpListener::bind("127.0.0.1:3000").await?;
            axum::serve(listener, app()).await?;
            ```

            ## Extractors: parsing the request
            A handler declares what it needs from the request as parameters, and Axum fills them in:

            - `Path(id): Path<String>` reads path parameters, parsed into the type you name (`Path<u64>` rejects `abc`).
            - `Query(params): Query<Page>` deserializes the query string (`?limit=10`) into a struct.
            - `Json(body): Json<NewUser>` reads the body, checks the `Content-Type: application/json` header and deserializes with serde.
            - `State(state): State<AppState>` gives access to shared application state.
            - `HeaderMap` gives all headers.

            If an extractor fails, the handler isn't called and Axum responds with a **rejection**: a body without a JSON content type is **415 Unsupported Media Type**, malformed JSON is **400**, well-formed JSON with the wrong fields is **422**. The body extractor (`Json`, `String`) must be the last parameter, because it consumes the request body.

            ## Responses
            A handler returns anything implementing `IntoResponse`:

            - `&'static str` or `String`: 200 with a text body.
            - `Json(value)`: 200 with `Content-Type: application/json`, serialized with serde.
            - `StatusCode::NOT_FOUND`: a bare status.
            - Tuples combine parts: `(StatusCode::CREATED, [(header::LOCATION, url)], Json(user))`.
            - `Result<T, E>` where both implement `IntoResponse`, so `?` works in handlers.

            ```rust
            async fn get_course(Path(id): Path<String>) -> Result<Json<Course>, StatusCode> {
                catalog().into_iter().find(|c| c.id == id).map(Json).ok_or(StatusCode::NOT_FOUND)
            }
            ```

            Returning **404 for a missing resource** instead of 200 with `null` matters: clients, caches, monitoring and retries all act on status codes, not on body contents.

            ## Status codes worth knowing
            - **200 OK**, **201 Created** (with a `Location` header for the new resource), **204 No Content**.
            - **400 Bad Request** (malformed input), **401 Unauthorized** (who are you?), **403 Forbidden** (I know who you are; no), **404 Not Found**, **405 Method Not Allowed**, **409 Conflict**, **415 Unsupported Media Type**, **422 Unprocessable Content** (well-formed but invalid), **429 Too Many Requests**.
            - **500 Internal Server Error** (our bug), **503 Service Unavailable** (temporarily can't serve).

            The rule of thumb: 4xx means the client should change the request; 5xx means the server failed and the same request might work later.

            ## Thin handlers
            Keep handlers to translation: extract, call a domain function, map the result to a response. Business rules (what makes an enrollment valid, how a price is computed) belong in ordinary functions and types that know nothing about HTTP. They're easier to test, and they survive when you add a CLI or a queue consumer next to the API.

            ## Testing without a network
            A `Router` is a **tower `Service`**: a function from request to response. Tests can call it directly with `oneshot`, with no socket at all:

            ```rust
            use tower::ServiceExt;

            let response = app()
                .oneshot(Request::builder().uri("/health").body(Body::empty()).unwrap())
                .await
                .unwrap();
            assert_eq!(response.status(), StatusCode::OK);
            ```

            This lesson's grader works exactly this way, and so should most of your API tests: fast, deterministic, and covering every status code.

            ## Common mistakes
            - 200 with an error message in the body.
            - Business logic inside handlers.
            - Putting the `Json` extractor before other extractors.
            - Testing only through a running server, which is slower and flakier than `oneshot`.

            ## Try it locally
            1. `cargo new course_api`, add `axum`, `tokio` (with `full`), `serde` and `serde_json`, and serve the router with `axum::serve`.
            2. Call it with `curl -i` (the `-i` shows status and headers): a known course, an unknown one, a POST without `-H 'content-type: application/json'`.
            3. Write one `oneshot` test per status code your API can return.
            """,
        Example = """
            use axum::{Json, Router, extract::Path, http::StatusCode, routing::get};

            async fn get_course(Path(id): Path<String>) -> Result<Json<Course>, StatusCode> {
                catalog().into_iter().find(|c| c.id == id).map(Json).ok_or(StatusCode::NOT_FOUND)
            }

            fn app() -> Router {
                Router::new().route("/courses/{id}", get(get_course))
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Build a small Axum router",
            "Write three handlers and the router that serves them. The grader provides `struct Course { id: String, title: String }` (Serialize, Clone) and `fn catalog() -> Vec<Course>`, and calls your router in memory. axum, serde and serde_json are available. Don't write a `main` function.",
            [
                "`GET /health` returns 200 with the text `ok`",
                "`GET /courses/{id}` returns the matching course from `catalog()` as JSON, or 404 if there is none",
                "`POST /echo` accepts a JSON body (`serde_json::Value`) and returns it unchanged as JSON; `fn app() -> Router` wires all three",
            ],
            """
            use axum::extract::Path;
            use axum::http::StatusCode;
            use axum::routing::{get, post};
            use axum::{Json, Router};

            // The grader provides: struct Course { id: String, title: String } and fn catalog() -> Vec<Course>

            async fn health() -> &'static str {
                todo!()
            }

            async fn get_course(Path(id): Path<String>) -> Result<Json<Course>, StatusCode> {
                todo!()
            }

            async fn echo(Json(value): Json<serde_json::Value>) -> Json<serde_json::Value> {
                todo!()
            }

            fn app() -> Router {
                todo!()
            }
            """,
            null,
            [],
            "Find the course with `catalog().into_iter().find(...)`, then `.map(Json).ok_or(StatusCode::NOT_FOUND)`. Register routes with `Router::new().route(\"/health\", get(health))` and so on; path parameters are written `{id}`.",
            ["Health and routing status codes are right", "Courses are returned as JSON or 404", "JSON is accepted and echoed"]),
    };

    private static Lesson RustApiContracts(Lesson lesson) => lesson with
    {
        Subtitle = "Design an HTTP contract clients can rely on: resource shapes, Location headers, problem documents, safe retries with idempotency keys, and bounded cursor pagination.",
        Concept = "An API is a promise to code you don't control. Use status codes and headers the way HTTP defines them, return errors in one machine-readable shape, let clients retry writes safely with an idempotency key, and never let a single request ask for unbounded data. Additive changes keep the promise; removals and renames break it.",
        Body = """
            ## The contract outlives the code
            Once a mobile app, another team's service or a customer's script calls your API, every detail becomes something they depend on: field names, status codes, error shapes, ordering, defaults. Changing them breaks clients you can't see or update. So design the contract deliberately before writing handlers, and evolve it **additively**: new optional fields and new endpoints are safe; removing, renaming or changing the meaning of anything is a breaking change that needs a new version (`/v2/...`) and a migration period.

            ## Creating resources
            `POST /v1/enrollments` with a JSON body creates an enrollment. The response:

            - **201 Created**, not 200.
            - A **`Location`** header with the new resource's URL: `/v1/enrollments/1`.
            - The created resource in the body, including server-assigned fields like `id`.

            ## Errors as problem documents
            Clients need to handle errors programmatically, so every error should have the same shape. **RFC 9457 "Problem Details"** is the standard: `Content-Type: application/problem+json` and a body like:

            ```json
            {"type": "about:blank", "title": "Unprocessable Content", "status": 422, "detail": "learner must not be empty"}
            ```

            `title` describes the kind of problem, `detail` this occurrence, `status` repeats the HTTP status (useful once the body is logged far from the response). You can add your own fields, such as a list of invalid fields. Never put stack traces, SQL or internal identifiers in errors: they help attackers and confuse users.

            ## Safe retries: idempotency keys
            Networks fail in the worst place: the client sends `POST /enrollments`, the server creates the enrollment, and the connection drops before the response arrives. The client can't tell whether it worked. If it retries, it may create a duplicate; if it doesn't, it may lose the request. For payments, orders and enrollments, both are bad.

            GET, PUT and DELETE are defined as **idempotent**: doing them twice has the same effect as once. POST isn't. The standard fix is an **idempotency key**: the client generates a unique value (a UUID) per logical operation and sends it in an `Idempotency-Key` header. The server records the key with the result. A repeated request with the same key gets the **original response**, and nothing is created twice. Stripe, and most payment APIs, work this way.

            Production details to think through: keys should expire (24 hours is common); the record must be written in the same transaction as the resource, or a crash between them breaks the guarantee; and a reused key with a *different* body is a client bug, usually answered with 409 or 422.

            ## Pagination
            A list endpoint must never return "everything": the table grows, and one request eventually times out or exhausts memory. Paginate, with a **default** page size and a **maximum** that clients can't exceed however much they ask for.

            - **Offset pagination** (`?offset=40&limit=20`) is simple but slow on large tables (the database still walks past the skipped rows) and unstable: if rows are inserted while a client pages, items shift between pages and get skipped or repeated.
            - **Cursor pagination** (`?after=40&limit=20`) asks for items *after* a position, typically the last id the client saw. It stays fast with an index and stable under inserts. The response includes the cursor for the next page, or `null` when there are no more:

            ```json
            {"items": [{"id": 41, ...}, {"id": 42, ...}], "next": 42}
            ```

            Clients should treat the cursor as an opaque token; many APIs encode it (base64 of the last sort key) so they can change its meaning later.

            ## Shared state in Axum
            The API needs a store shared by all requests. Wrap it as the networking module did and give it to the router with `with_state`:

            ```rust
            type Db = Arc<Mutex<Store>>;

            fn app(db: Db) -> Router {
                Router::new()
                    .route("/v1/enrollments", get(list).post(create))
                    .route("/v1/enrollments/{id}", get(fetch))
                    .with_state(db)
            }

            async fn create(State(db): State<Db>, headers: HeaderMap, Json(input): Json<NewEnrollment>) -> Response {
                // read the Idempotency-Key header, validate, then lock and create
            }
            ```

            A handler can return `Response` directly, built with `(status, headers, body).into_response()`, when different branches return different shapes.

            ## Common mistakes
            - 200 for creation, or no `Location` header.
            - A different error shape for every endpoint.
            - POST endpoints that duplicate work when a client retries.
            - Unbounded list endpoints, or a `limit` the client can set to a million.
            - Offset pagination over data that changes while clients page.
            - Renaming a field in place and calling it a minor change.

            ## Try it locally
            1. Run the API and `curl -i` a POST twice with the same `Idempotency-Key`, then twice with different keys.
            2. Page through the list with `after`, inserting new enrollments between requests, and compare with offset pagination.
            3. Write an OpenAPI description of the contract (the `utoipa` crate can generate one from your types).
            """,
        Example = """
            fn problem(status: StatusCode, detail: &str) -> Response {
                let body = serde_json::json!({
                    "type": "about:blank",
                    "title": status.canonical_reason().unwrap_or("Error"),
                    "status": status.as_u16(),
                    "detail": detail,
                });
                (status, [(header::CONTENT_TYPE, "application/problem+json")], body.to_string()).into_response()
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Implement an enrollment API contract",
            "Build the three endpoints below over an in-memory store. The grader retries requests, pages through 60 enrollments, and checks status codes, headers and error documents. axum, serde and serde_json are available. Don't write a `main` function.",
            [
                "`fn app(db: Db) -> Router` where `type Db = Arc<Mutex<Store>>` and `Store` implements `Default`; enrollments are `{\"id\", \"learner\", \"course\"}` with ids starting at 1",
                "`POST /v1/enrollments` requires an `Idempotency-Key` header (400 if missing), rejects a blank learner or course (422), and returns 201 with `Location: /v1/enrollments/<id>`; repeating a key returns the original enrollment and creates nothing",
                "`GET /v1/enrollments?limit=&after=` returns `{\"items\": [...], \"next\": <last id or null>}` in id order: default limit 20, capped at 50, `limit=0` is 422; `GET /v1/enrollments/{id}` returns one enrollment or 404",
                "Every error is a problem document: `Content-Type: application/problem+json` with `title`, `status` and `detail`",
            ],
            """
            use std::collections::HashMap;
            use std::sync::{Arc, Mutex};

            use axum::extract::{Path, Query, State};
            use axum::http::{HeaderMap, StatusCode, header};
            use axum::response::{IntoResponse, Response};
            use axum::routing::get;
            use axum::{Json, Router};
            use serde::{Deserialize, Serialize};

            #[derive(Default)]
            struct Store {
                // Your fields.
            }

            type Db = Arc<Mutex<Store>>;

            fn app(db: Db) -> Router {
                todo!()
            }
            """,
            null,
            [],
            "Keep a `Vec<Enrollment>` and a `HashMap<String, u64>` from key to id. Read the key with `headers.get(\"idempotency-key\")`. For paging, filter ids greater than `after`, take `limit.min(50)`, and set `next` to the last returned id only if more items remain. A `problem(status, detail)` helper keeps the error shape in one place.",
            ["Creation returns 201 with Location", "Retries with the same key are safe", "Pagination has a default, a cap and a cursor", "Errors are problem documents"]),
    };

    private static Lesson RustExtractorsErrors(Lesson lesson) => lesson with
    {
        Subtitle = "Turn untrusted request data into validated domain types at the boundary, and map every failure, including the framework's own rejections, to one consistent error response.",
        Concept = "Parse, don't validate: convert raw input into a type that can only hold valid values, such as an Email newtype built through TryFrom, so the rest of the code never re-checks it. Define one error enum for the API with an IntoResponse implementation that chooses the status and body for each variant, and catch extractor rejections so malformed requests get the same error shape as everything else.",
        Body = """
            ## Parse, don't validate
            A common pattern is to accept a `String`, check it in the handler, and pass the same `String` on. The check then has to be repeated, or trusted, everywhere the value travels, and one forgotten check is a bug. The alternative: convert the raw value **once**, at the boundary, into a type whose existence proves it's valid.

            ```rust
            #[derive(Debug, Clone, PartialEq)]
            struct Email(String);

            impl TryFrom<String> for Email {
                type Error = ApiError;

                fn try_from(raw: String) -> Result<Self, Self::Error> {
                    let email = raw.trim().to_lowercase();
                    // ... checks ...
                    Ok(Email(email))
                }
            }
            ```

            Keep the field private (in a module), so the only way to make an `Email` is `try_from`. Every function that takes an `Email` knows it's already trimmed, lowercased and plausible. This is the structs-and-enums lesson's "make invalid states unrepresentable", applied at the system's edge. Normalising during parsing (trimming, lowercasing) also means `Ada@Example.com` and `ada@example.com` are recognised as the same user, which prevents duplicate accounts.

            A note on email specifically: the only real proof an address works is sending a message to it. Validation should catch obvious mistakes (no `@`, two `@`s, no domain dot), not enforce a complex regular expression that rejects valid addresses.

            ## One error type for the API
            Each handler can fail in a few distinct ways. Give the API one enum for them, and let it decide how each becomes a response:

            ```rust
            enum ApiError {
                BadRequest(String),   // 400: the request itself is malformed
                NotFound(String),     // 404
                Conflict(String),     // 409: clashes with current state, e.g. a duplicate
                Validation(String),   // 422: well-formed but invalid values
            }

            impl IntoResponse for ApiError {
                fn into_response(self) -> Response {
                    let (status, detail) = match self { /* one arm per variant */ };
                    // build a problem+json response
                }
            }
            ```

            Now handlers return `Result<T, ApiError>`, use `?` freely, and the status-code decisions live in one `match`. Adding a variant forces you to decide its status, because the `match` must be exhaustive.

            Domain errors from lower layers convert into `ApiError` with `From` implementations (the errors lesson), so `?` maps a `RepositoryError::Missing` to `ApiError::NotFound` automatically. Log internal errors in full, but return a generic 500 to the client.

            ## 400 versus 422 versus 409
            These three are easy to blur, and clients rely on the difference:

            - **400 Bad Request**: the request can't be understood. Malformed JSON, a path parameter that isn't a number, a missing required header.
            - **422 Unprocessable Content**: the request is well-formed, but its values fail validation. An invalid email, a negative quantity.
            - **409 Conflict**: valid on its own, but conflicts with the current state. The email is already registered; the order was already shipped.

            ## Catching rejections
            When an extractor fails, Axum responds itself, with a plain-text body. That breaks the "one error shape" promise. Take the extractor's result instead of the extractor:

            ```rust
            use axum::extract::rejection::JsonRejection;

            async fn create_user(payload: Result<Json<NewUser>, JsonRejection>) -> Result<..., ApiError> {
                let Json(input) = payload.map_err(|rejection| ApiError::BadRequest(rejection.body_text()))?;
                let email = Email::try_from(input.email)?;
                // ...
            }
            ```

            `Result<Json<T>, JsonRejection>` is itself an extractor that never fails; it hands you the rejection to convert. `body_text()` gives a readable explanation. The same works for `Path` with `PathRejection` and `Query` with `QueryRejection`. For a large API, write a custom extractor once (`struct AppJson<T>`) that does this conversion, instead of repeating it per handler.

            ## Common mistakes
            - Validating in the handler and passing raw strings onward.
            - Every handler building its own error responses.
            - Returning 400 for everything, or 500 for client mistakes.
            - Leaking framework rejections in a different format from your own errors.
            - Echoing internal error messages (SQL, file paths) to clients.

            ## Try it locally
            1. Send malformed JSON to an endpoint using plain `Json<T>` and look at the response. Then switch to `Result<Json<T>, JsonRejection>`.
            2. Add a `ValidationErrors` variant carrying a list of field errors, and return all problems at once.
            3. Write a custom extractor `AppJson<T>` that produces `ApiError` on rejection and use it everywhere.
            """,
        Example = """
            async fn create_user(
                State(users): State<Users>,
                payload: Result<Json<NewUser>, JsonRejection>,
            ) -> Result<(StatusCode, Json<User>), ApiError> {
                let Json(input) = payload.map_err(|r| ApiError::BadRequest(r.body_text()))?;
                let email = Email::try_from(input.email)?; // 422 if invalid
                // ... 409 if it already exists, otherwise 201 ...
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Validate at the boundary and map every error",
            "Build a small users API with an error enum, a validated `Email` type, and handlers that turn every failure, including malformed JSON and bad path parameters, into a problem document. axum, serde and serde_json are available. Don't write a `main` function.",
            [
                "`#[derive(Debug, PartialEq)] enum ApiError { BadRequest(String), NotFound(String), Conflict(String), Validation(String) }` with `impl IntoResponse` producing 400, 404, 409 and 422 as `application/problem+json` with `title`, `status` and `detail`",
                "`#[derive(Debug, Clone, PartialEq, Serialize)] struct Email(String)` with `TryFrom<String>` (error `ApiError::Validation`) that trims, lowercases, and requires exactly one `@`, a non-empty local part, and a domain containing a dot that doesn't start or end with one",
                "`POST /users` with `{\"email\": ...}` returns 201 `{\"id\", \"email\"}` (ids from 1), 409 for a duplicate email, 422 for an invalid one, and 400 problem documents for malformed JSON; `GET /users/{id}` returns the user, 404, or a 400 problem for a non-numeric id",
                "`fn app(users: Users) -> Router` where `type Users = Arc<Mutex<Vec<User>>>`",
            ],
            """
            use std::sync::{Arc, Mutex};

            use axum::extract::rejection::{JsonRejection, PathRejection};
            use axum::extract::{Path, State};
            use axum::http::{StatusCode, header};
            use axum::response::{IntoResponse, Response};
            use axum::routing::{get, post};
            use axum::{Json, Router};
            use serde::{Deserialize, Serialize};

            #[derive(Debug, PartialEq)]
            enum ApiError {
                BadRequest(String),
                NotFound(String),
                Conflict(String),
                Validation(String),
            }

            #[derive(Debug, Clone, PartialEq, Serialize)]
            struct Email(String);

            #[derive(Clone, Serialize)]
            struct User {
                id: u64,
                email: Email,
            }

            type Users = Arc<Mutex<Vec<User>>>;

            fn app(users: Users) -> Router {
                todo!()
            }
            """,
            null,
            [],
            "Implement `IntoResponse` with one `match` that picks the status, then build `(status, [(header::CONTENT_TYPE, \"application/problem+json\")], body.to_string())`. Take `payload: Result<Json<NewUser>, JsonRejection>` and `id: Result<Path<u64>, PathRejection>`, converting rejections with `.body_text()`. `#[derive(Serialize)]` on a one-field tuple struct serializes as the inner string.",
            ["Every error variant maps to its status", "Emails are parsed into a validated type", "Rejections become problem documents", "The users API behaves as specified"]),
    };

    private static Lesson RustDatabaseTransactions(Lesson lesson) => lesson with
    {
        Subtitle = "Store data in SQLite from Rust: versioned migrations, parameterized queries, constraints as a safety net, and transactions that make multi-step changes all-or-nothing.",
        Concept = "A transaction groups several statements so they all take effect or none do, even if the process fails halfway. Parameters keep data separate from SQL, which makes injection impossible. Schema constraints (UNIQUE, NOT NULL, CHECK, foreign keys) enforce rules the database guarantees for every writer, and migrations change the schema in versioned, repeatable steps.",
        Body = """
            ## Why a database
            The capstone built a storage engine by hand, which is the right way to understand one. Most services use an existing database instead, because it already solves concurrency, crash recovery, indexing and querying. **SQLite** is a complete SQL database in a library: no server, one file, extremely well tested, and used in every phone and browser. Its concepts carry directly to PostgreSQL and MySQL. In Rust, **rusqlite** wraps it synchronously; **sqlx** is the common async choice for PostgreSQL and can check queries at compile time.

            ## Migrations
            The schema changes as the product does, and every copy of the database (your laptop, CI, production) must go through the same changes in the same order. A **migration** is one numbered schema change, and the database records which version it's at. SQLite has a built-in slot for that: `PRAGMA user_version`.

            ```rust
            fn migrate(conn: &Connection) -> rusqlite::Result<()> {
                let version: i64 = conn.query_row("PRAGMA user_version", [], |row| row.get(0))?;
                if version < 1 {
                    conn.execute_batch(
                        "BEGIN;
                         CREATE TABLE accounts (
                             id INTEGER PRIMARY KEY,
                             owner TEXT NOT NULL UNIQUE,
                             balance INTEGER NOT NULL CHECK (balance >= 0)
                         );
                         PRAGMA user_version = 1;
                         COMMIT;",
                    )?;
                }
                // if version < 2 { ... }
                Ok(())
            }
            ```

            Each step runs only if the database is older than it, so `migrate` can run at every startup and is safe to repeat. Wrapping a step and its version bump in one transaction means a crash can't leave the schema half-changed. Never edit a migration that has already run somewhere; add a new one. Tools such as `refinery` and `sqlx migrate` manage migration files for larger projects.

            ## Constraints are the last line of defence
            `NOT NULL`, `UNIQUE`, `CHECK (balance >= 0)` and `REFERENCES` (foreign keys) are rules the **database** enforces on every write, from every program and every bug. Application checks give good error messages; constraints guarantee the data stays valid even when an application check is missing. Have both. (In SQLite, foreign-key enforcement must be switched on per connection with `PRAGMA foreign_keys = ON`.)

            Money is stored as integer cents (`INTEGER`), never as floating point, as in the first lessons.

            ## Parameters, always
            Building SQL with `format!` is **SQL injection** waiting to happen:

            ```rust
            // Never:
            conn.execute_batch(&format!("INSERT INTO accounts (owner) VALUES ('{owner}')"))?;
            ```

            An owner named `o'brien'); DROP TABLE ledger; --` closes the string literal and runs a command of its own. Injection has been among the most exploited vulnerabilities for decades. **Parameters** send the SQL text and the values separately, so values can never be interpreted as SQL:

            ```rust
            conn.execute("INSERT INTO accounts (owner, balance) VALUES (?1, ?2)", params![owner, balance])?;
            ```

            `?1` and `?2` are placeholders; `params!` supplies the values. Reading rows uses `query_row` (exactly one row; add `.optional()` from the `OptionalExtension` trait when zero is normal) or `prepare` with `query_map` for many.

            ## Transactions
            A transfer is three changes: debit one account, credit another, record a ledger entry. If the process stops after the debit, money has vanished. A **transaction** makes them **atomic**:

            ```rust
            fn transfer(conn: &mut Connection, from: &str, to: &str, amount: i64) -> Result<(), TransferError> {
                let tx = conn.transaction()?;
                // read both accounts with tx.query_row(...)
                // return Err early if either is missing or funds are short
                tx.execute("UPDATE accounts SET balance = balance - ?1 WHERE id = ?2", params![amount, from_id])?;
                tx.execute("UPDATE accounts SET balance = balance + ?1 WHERE id = ?2", params![amount, to_id])?;
                tx.execute("INSERT INTO ledger (from_id, to_id, amount) VALUES (?1, ?2, ?3)", params![from_id, to_id, amount])?;
                tx.commit()?;
                Ok(())
            }
            ```

            Ownership makes this pattern safe: if the function returns early, through an explicit `return Err` or a `?`, the `Transaction` value is **dropped without commit, and its `Drop` rolls everything back**. Only reaching `commit()` makes the changes permanent. (`conn.transaction()` needs `&mut Connection`, so nothing else can use the connection while the transaction is open.)

            Transactions also give **isolation**: other connections don't see half-finished changes. Databases offer several isolation levels with different trade-offs between consistency and concurrency; SQLite transactions are serializable, which is the strongest.

            Keep transactions short. Never hold one open across a network call or user input: it blocks other writers and ties up resources.

            ## The service around it
            In an Axum service, a connection pool (`r2d2_sqlite`, `deadpool`, or sqlx's built-in pool) lends connections to handlers. With a synchronous driver like rusqlite, run database calls inside `tokio::task::spawn_blocking` so they don't block the async runtime, as the async lessons warned.

            ## Common mistakes
            - `format!` for SQL.
            - Multi-step changes without a transaction.
            - Checking a balance in one statement and updating in another outside a transaction, so two concurrent transfers both pass the check.
            - Editing an applied migration instead of adding a new one.
            - Relying only on application checks with no constraints.

            ## Try it locally
            1. Install the `sqlite3` command-line tool, open the database file your program creates, and run `.schema` and `PRAGMA user_version;`.
            2. Write the injection version, create the tricky account, and see which table disappears.
            3. Remove the transaction from `transfer`, make the credit fail, and inspect the balances.
            """,
        Example = """
            fn transfer(conn: &mut Connection, from: &str, to: &str, amount: i64) -> Result<(), TransferError> {
                let tx = conn.transaction()?;
                // ... look up both accounts with tx.query_row(..., params![...]) ...
                tx.execute("UPDATE accounts SET balance = balance - ?1 WHERE id = ?2", params![amount, from_id])?;
                tx.execute("UPDATE accounts SET balance = balance + ?1 WHERE id = ?2", params![amount, to_id])?;
                tx.commit()?; // returning before this line rolls everything back
                Ok(())
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Migrate a schema and move money atomically",
            "Write the migrations and queries for a small accounts database with rusqlite. The grader uses an in-memory SQLite database, runs your migrations twice, tries an injection, and checks that failed transfers change nothing. Don't write a `main` function.",
            [
                "`fn migrate(conn: &Connection) -> rusqlite::Result<()>` brings the schema to `user_version` 2 and is safe to run again: version 1 creates `accounts` (unique non-null `owner`, `balance` that can't go negative), version 2 creates a `ledger` table (`from_id`, `to_id`, positive `amount`)",
                "`fn open_account(conn: &Connection, owner: &str, balance: i64) -> rusqlite::Result<i64>` returns the new id, and `fn balance(conn: &Connection, owner: &str) -> rusqlite::Result<Option<i64>>`; both use parameters, so any owner name is just data",
                "`fn transfer(conn: &mut Connection, from: &str, to: &str, amount: i64) -> Result<(), TransferError>` with `#[derive(Debug, PartialEq)] enum TransferError { InvalidAmount, NoSuchAccount, InsufficientFunds, Database(String) }` moves the amount and writes a ledger row in one transaction; any failure changes nothing",
            ],
            """
            use rusqlite::{Connection, OptionalExtension, params};

            #[derive(Debug, PartialEq)]
            enum TransferError {
                InvalidAmount,
                NoSuchAccount,
                InsufficientFunds,
                Database(String),
            }

            impl From<rusqlite::Error> for TransferError {
                fn from(error: rusqlite::Error) -> Self {
                    TransferError::Database(error.to_string())
                }
            }

            fn migrate(conn: &Connection) -> rusqlite::Result<()> {
                todo!()
            }

            fn open_account(conn: &Connection, owner: &str, balance: i64) -> rusqlite::Result<i64> {
                todo!()
            }

            fn balance(conn: &Connection, owner: &str) -> rusqlite::Result<Option<i64>> {
                todo!()
            }

            fn transfer(conn: &mut Connection, from: &str, to: &str, amount: i64) -> Result<(), TransferError> {
                todo!()
            }
            """,
            null,
            [],
            "Read `PRAGMA user_version` and run each `execute_batch` step only when the version is lower. Use `?1`-style placeholders with `params!`, `last_insert_rowid()` for the new id, and `.optional()` for lookups. In `transfer`, reject amounts of 0 or less first, then do every read and write through `let tx = conn.transaction()?;` and finish with `tx.commit()?`.",
            ["Migrations are versioned and repeatable", "Queries use parameters, so injection fails", "Transfers move money and record it", "Failures leave everything unchanged"]),
    };

    private static Lesson RustAuthSecurity(Lesson lesson) => lesson with
    {
        Subtitle = "Separate who a caller is from what they may do: authenticate once in an extractor, authorize every resource access, choose between 401, 403 and 404 deliberately, and keep secrets out of code and logs.",
        Concept = "Authentication establishes identity: this request comes from Ada. Authorization decides access: may Ada read note 7? Authenticate in one place, a custom extractor that every protected handler requires, and authorize in every handler against the specific resource. The most common API vulnerability is a handler that checks the first and forgets the second.",
        Body = """
            ## Two questions
            - **Authentication (authn):** who is calling? Answered with a credential: a session cookie, an API key, or a **bearer token** such as a JWT, usually sent as `Authorization: Bearer <token>`.
            - **Authorization (authz):** is *this* caller allowed to do *this* action to *this* resource?

            Being signed in answers only the first. The OWASP API Security Top 10 lists **Broken Object Level Authorization** first: an endpoint like `GET /notes/{id}` that checks for a valid token but not whether the note belongs to the caller, so any user can read any note by changing the number in the URL. It's also called **IDOR** (insecure direct object reference), and it's behind many large data leaks.

            ## Authenticate once, with an extractor
            Axum lets you define your own extractors. Implement `FromRequestParts` for a `Principal` type (the authenticated caller), and any handler that lists `principal: Principal` as a parameter is protected; if extraction fails, the handler never runs:

            ```rust
            impl FromRequestParts<Arc<AppState>> for Principal {
                type Rejection = Response;

                async fn from_request_parts(parts: &mut Parts, state: &Arc<AppState>) -> Result<Self, Self::Rejection> {
                    let value = parts.headers.get(header::AUTHORIZATION)
                        .and_then(|v| v.to_str().ok())
                        .ok_or_else(unauthorized)?;
                    let token = value.strip_prefix("Bearer ").ok_or_else(unauthorized)?;
                    state.tokens.get(token).cloned().ok_or_else(unauthorized)
                }
            }
            ```

            Because the type system requires the `Principal` parameter, a protected handler can't accidentally skip authentication. Real systems verify the token's signature and expiry (with a JWT library such as `jsonwebtoken`) or look up a session, but the shape is the same. When comparing secrets directly, use a **constant-time** comparison (the `subtle` crate), so the time taken doesn't reveal how many characters matched.

            ## Authorize every resource
            Inside the handler, the question is about the specific resource:

            ```rust
            match notes.get(&id) {
                Some(note) if note.owner == principal.user || principal.admin => Json(note.clone()).into_response(),
                _ => StatusCode::NOT_FOUND.into_response(),
            }
            ```

            Put the ownership check where the data is loaded, or better, in the query itself (`WHERE id = ?1 AND owner = ?2`), so there's no code path that loads a resource without it. Roles like `admin` grant broader access, but each action decides what a role may do: here an admin may read any note but not delete other people's.

            ## 401, 403 or 404?
            - **401 Unauthorized** (badly named; it means *unauthenticated*): no credentials, or invalid ones. Include a `WWW-Authenticate: Bearer` header naming the scheme to use. A forged or expired token is 401, not 403.
            - **403 Forbidden**: the caller is known and may know the resource exists, but isn't allowed this action. The admin trying to delete someone else's note.
            - **404 Not Found** for resources the caller isn't allowed to know about. Answering 403 for `/notes/2` tells an attacker that note 2 exists, which lets them map your data. Returning 404 for "exists but not yours" and for "doesn't exist" makes them indistinguishable.

            ## Secrets and other essentials
            - **Secrets** (signing keys, database passwords, API keys) come from the environment or a secret manager at startup, never from source code or the repository. Rotate them, and never log them, including inside error messages or request dumps; redact the `Authorization` header in logs.
            - **Passwords**, if you store them at all, are hashed with a slow, salted algorithm built for passwords: **Argon2id** (the `argon2` crate). Never plain hashes like SHA-256, never encryption.
            - **Rate-limit** authentication endpoints so passwords and tokens can't be guessed at speed.
            - **TLS** everywhere in production. Bearer tokens are as good as passwords to anyone who sees them.
            - **CORS** only matters for browsers: configure the allowed origins narrowly (`tower-http`'s `CorsLayer`), and never allow any origin while also allowing credentials.
            - Default deny: a new route should require authentication unless someone decided otherwise.

            ## Common mistakes
            - Checking that the caller is signed in, but not that the resource is theirs.
            - 403 that reveals the existence of other users' data.
            - 403 for bad tokens, so clients don't know to re-authenticate.
            - Secrets in code, config files in the repository, or logs.
            - Authorization logic copied into each handler with small differences.

            ## Try it locally
            1. Remove the ownership check, then read another user's note by changing the id. That's the whole attack.
            2. Add the `tower-http` trace layer and confirm the `Authorization` header doesn't appear in logs.
            3. Replace the token table with signed JWTs using `jsonwebtoken`, including expiry, and test an expired token.
            """,
        Example = """
            async fn read_note(principal: Principal, State(state): State<Arc<AppState>>, Path(id): Path<u64>) -> Response {
                let notes = state.notes.lock().unwrap();
                match notes.get(&id) {
                    Some(note) if note.owner == principal.user || principal.admin => Json(note.clone()).into_response(),
                    _ => StatusCode::NOT_FOUND.into_response(), // not yours looks the same as not there
                }
            }
            """,
        Exercise = new Exercise(
            ExerciseKind.Code,
            "Authenticate with an extractor, authorize every note",
            "Protect a notes API. The grader provides the types and state below, and calls your router as different users. axum and serde are available. Don't write a `main` function or redefine the provided types.",
            [
                "The grader provides `struct Principal { user: String, admin: bool }` (Clone), `struct Note { id: u64, owner: String, text: String }` (Clone, Serialize) and `struct AppState { tokens: HashMap<String, Principal>, notes: Mutex<HashMap<u64, Note>> }`",
                "Implement `FromRequestParts<Arc<AppState>>` for `Principal`: read `Authorization: Bearer <token>` and look the token up; a missing header, another scheme or an unknown token is 401 with `WWW-Authenticate: Bearer`",
                "`GET /notes/{id}`: the owner or an admin gets the note as JSON; anyone else, or a missing note, gets 404",
                "`DELETE /notes/{id}`: the owner gets 204 and the note is removed; an admin who isn't the owner gets 403; anyone else, or a missing note, gets 404. `fn app(state: Arc<AppState>) -> Router` wires both",
            ],
            """
            use std::sync::Arc;

            use axum::extract::{FromRequestParts, Path, State};
            use axum::http::request::Parts;
            use axum::http::{StatusCode, header};
            use axum::response::{IntoResponse, Response};
            use axum::routing::get;
            use axum::{Json, Router};

            // The grader provides Principal, Note and AppState (see the requirements).

            impl FromRequestParts<Arc<AppState>> for Principal {
                type Rejection = Response;

                async fn from_request_parts(parts: &mut Parts, state: &Arc<AppState>) -> Result<Self, Self::Rejection> {
                    todo!()
                }
            }

            fn app(state: Arc<AppState>) -> Router {
                todo!()
            }
            """,
            null,
            [],
            "Build the 401 as `(StatusCode::UNAUTHORIZED, [(header::WWW_AUTHENTICATE, \"Bearer\")], \"authentication required\").into_response()`. Use `strip_prefix(\"Bearer \")` on the header value. In each handler take `principal: Principal` as a parameter, lock `state.notes`, and decide with a `match` that covers owner, admin and everyone else. One route: `get(read_note).delete(delete_note)`.",
            ["Missing and bad credentials are 401 with a challenge", "Only owners and admins can read a note", "Other users' notes look like missing ones", "Delete distinguishes 204, 403 and 404"]),
    };
}
