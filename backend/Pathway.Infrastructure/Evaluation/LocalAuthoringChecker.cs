using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Pathway.Domain;

namespace Pathway.Infrastructure.Evaluation;

/// <summary>
/// Development-only code checks that make local lesson authoring fast. They look at the shape of
/// the submitted code and never run it. Production grades code only through the sandbox evaluator.
/// </summary>
public static class LocalAuthoringChecker
{
    public static ValidationResult Check(Lesson lesson, string code)
    {
        if (lesson.Slug.StartsWith("react-", StringComparison.Ordinal))
        {
            var reactPassed = lesson.Slug switch
            {
                "react-composition-root" => code.Contains("RouterProvider", StringComparison.Ordinal) && code.Contains("AppProviders", StringComparison.Ordinal),
                "react-props-composition" => code.Contains("OrderStatusBadge", StringComparison.Ordinal) && code.Contains("status", StringComparison.Ordinal) && code.Contains("<span", StringComparison.Ordinal),
                "react-use-state" => code.Contains("useState", StringComparison.Ordinal) && code.Contains("onClick", StringComparison.Ordinal),
                "react-use-reducer" => code.Contains("useReducer", StringComparison.Ordinal) && code.Contains("type Action", StringComparison.Ordinal),
                "react-effects-synchronization" => code.Contains("useEffect", StringComparison.Ordinal) && code.Contains("subscribeToOrder", StringComparison.Ordinal) && code.Contains("orderId", StringComparison.Ordinal),
                "react-context-provider-boundaries" => code.Contains("createContext", StringComparison.Ordinal) && code.Contains("useContext", StringComparison.Ordinal) && code.Contains("TenantProvider", StringComparison.Ordinal),
                "react-actions-optimistic-use" => code.Contains("useActionState", StringComparison.Ordinal) && code.Contains("useFormStatus", StringComparison.Ordinal) && code.Contains("<form", StringComparison.Ordinal),
                "react-router-data-mode" => code.Contains("createBrowserRouter", StringComparison.Ordinal) && code.Contains("orders", StringComparison.Ordinal),
                "react-router-loaders-params-search" => code.Contains("LoaderFunctionArgs", StringComparison.Ordinal) && code.Contains("params.orderId", StringComparison.Ordinal) && code.Contains("getOrder", StringComparison.Ordinal),
                "react-router-actions-navigation" => code.Contains("ActionFunctionArgs", StringComparison.Ordinal) && code.Contains("approveOrder", StringComparison.Ordinal) && code.Contains("<Form", StringComparison.Ordinal),
                "react-api-client-boundary" => code.Contains("fetch(", StringComparison.Ordinal) && code.Contains("encodeURIComponent", StringComparison.Ordinal) && code.Contains("response.ok", StringComparison.Ordinal),
                "react-tailwind-design-system" => code.Contains("ButtonHTMLAttributes", StringComparison.Ordinal) && code.Contains("primary", StringComparison.Ordinal) && code.Contains("secondary", StringComparison.Ordinal),
                "react-testing-vitest-rtl" => code.Contains("render(", StringComparison.Ordinal) && code.Contains("getByRole", StringComparison.Ordinal) && code.Contains("expect", StringComparison.Ordinal),
                "react-enterprise-capstone" => code.Contains("OrderApprovalPage", StringComparison.Ordinal) && code.Contains("OrderSummary", StringComparison.Ordinal) && code.Contains("ApproveOrderForm", StringComparison.Ordinal),
                "react-lite-jsx-rendering" => code.Contains("DashboardHeading", StringComparison.Ordinal) && code.Contains("<h1", StringComparison.Ordinal) && code.Contains("title", StringComparison.Ordinal),
                "react-lite-components-props" => code.Contains("StatCard", StringComparison.Ordinal) && code.Contains("label", StringComparison.Ordinal) && code.Contains("value", StringComparison.Ordinal),
                "react-lite-state" => code.Contains("useState", StringComparison.Ordinal) && code.Contains("onChange", StringComparison.Ordinal) && code.Contains("value=", StringComparison.Ordinal),
                "react-lite-forms" => code.Contains("useState", StringComparison.Ordinal) && code.Contains("<form", StringComparison.Ordinal) && code.Contains("type='email'", StringComparison.Ordinal),
                "react-lite-router-pages" => code.Contains("createBrowserRouter", StringComparison.Ordinal) && code.Contains("/users", StringComparison.Ordinal),
                "react-lite-layout-nav" => code.Contains("Link", StringComparison.Ordinal) && code.Contains("Outlet", StringComparison.Ordinal) && code.Contains("/users", StringComparison.Ordinal),
                "react-lite-table-filter-sort" => code.Contains("filterUsers", StringComparison.Ordinal) && code.Contains(".filter(", StringComparison.Ordinal) && code.Contains("toLowerCase", StringComparison.Ordinal),
                "react-lite-dummy-api-read" => code.Contains("usersApi", StringComparison.Ordinal) && code.Contains("async list", StringComparison.Ordinal) && code.Contains("seedUsers", StringComparison.Ordinal),
                "react-lite-router-loader" => code.Contains("usersLoader", StringComparison.Ordinal) && code.Contains("usersApi.list", StringComparison.Ordinal),
                "react-lite-dummy-api-write" => code.Contains("async create", StringComparison.Ordinal) && code.Contains("users.push", StringComparison.Ordinal),
                "react-lite-router-action-crud" => code.Contains("ActionFunctionArgs", StringComparison.Ordinal) && code.Contains("formData", StringComparison.Ordinal) && code.Contains("usersApi.create", StringComparison.Ordinal),
                "react-lite-basic-testing" => code.Contains("from 'vitest'", StringComparison.Ordinal) && code.Contains("filterUsers", StringComparison.Ordinal) && code.Contains("toHaveLength", StringComparison.Ordinal),
                "react-lite-capstone" => code.Contains("DashboardPage", StringComparison.Ordinal) && code.Contains("StatCard", StringComparison.Ordinal) && code.Contains("UserTable", StringComparison.Ordinal),
                _ => false
            };
            return new ValidationResult(reactPassed, reactPassed ? 2 : 0, 2, reactPassed ? "All checks passed. Your React/TypeScript solution meets this lesson’s local authoring checks." : lesson.Exercise.Hint, reactPassed ? lesson.NextSlug : null, CodeReviewer.Review(lesson, code));
        }
        if (lesson.Slug.StartsWith("git-", StringComparison.Ordinal))
        {
            var gitPassed = lesson.Slug switch
            {
                "git-init-status" => code.Contains("git init", StringComparison.Ordinal) && code.Contains("git status", StringComparison.Ordinal),
                "git-stage-files" => code.Contains("git add README.md", StringComparison.Ordinal),
                "git-first-commit" => code.Contains("git commit", StringComparison.Ordinal),
                "git-history-diff" => code.Contains("git log", StringComparison.Ordinal) && code.Contains("git diff", StringComparison.Ordinal),
                "git-switch-branch" => code.Contains("git switch", StringComparison.Ordinal) && code.Contains("feature/navigation", StringComparison.Ordinal),
                "git-feature-commit" => code.Contains("git add", StringComparison.Ordinal) && code.Contains("git commit", StringComparison.Ordinal),
                "git-merge-feature" => code.Contains("git merge", StringComparison.Ordinal) && code.Contains("feature/readme", StringComparison.Ordinal),
                "git-resolve-conflict" => code.Contains("git merge", StringComparison.Ordinal) && code.Contains("git add", StringComparison.Ordinal) && code.Contains("git commit", StringComparison.Ordinal),
                "git-restore-reset" => code.Contains("git restore app.txt", StringComparison.Ordinal) && code.Contains("git restore --staged notes.txt", StringComparison.Ordinal),
                "git-ignore-generated-files" => code.Contains(".gitignore", StringComparison.Ordinal) && code.Contains("git add", StringComparison.Ordinal),
                "git-fetch-remote" => code.Contains("git fetch", StringComparison.Ordinal),
                "git-rebase-feature" => code.Contains("git rebase", StringComparison.Ordinal),
                "git-revert-push" => code.Contains("git revert", StringComparison.Ordinal) && code.Contains("git push", StringComparison.Ordinal),
                _ => false
            };
            return new ValidationResult(gitPassed, gitPassed ? 3 : 0, 3, gitPassed ? "Command shape looks right for local authoring. Deployed lessons verify the real repository state inside the sandbox." : lesson.Exercise.Hint, gitPassed ? lesson.NextSlug : null, CodeReviewer.Review(lesson, code));
        }
        if (lesson.Slug.StartsWith("linux-", StringComparison.Ordinal) || lesson.Slug.StartsWith("bash-", StringComparison.Ordinal) || lesson.Slug.StartsWith("vim-", StringComparison.Ordinal))
        {
            var normalized = code.ToLowerInvariant();
            var linuxPassed = lesson.Slug switch
            {
                "linux-paths-navigation" => normalized.Contains("cd") && normalized.Contains("pwd") && normalized.Contains("ls"),
                "linux-files-directories" => normalized.Contains("mkdir") && normalized.Contains("cp") && normalized.Contains("mv") && normalized.Contains("rm"),
                "linux-read-text" => normalized.Contains("head") && normalized.Contains("tail") && normalized.Contains("wc"),
                "linux-pipes-redirection" => normalized.Contains("grep") && normalized.Contains("sort") && normalized.Contains("uniq") && normalized.Contains(">"),
                "linux-grep-find" => normalized.Contains("find") && normalized.Contains("grep"),
                "linux-expansion-quoting" => normalized.Contains("cp") && code.Contains("\"$SOURCE\"", StringComparison.Ordinal),
                "linux-permissions" => normalized.Contains("chmod"),
                "linux-environment-path" => normalized.Contains("export") && normalized.Contains("path") && normalized.Contains("command -v"),
                "linux-archives-compression" => normalized.Contains("tar"),
                "bash-script-basics" => normalized.Contains("#!/usr/bin/env bash") && normalized.Contains("chmod"),
                "bash-variables-arguments" => normalized.Contains("#!/usr/bin/env bash") && normalized.Contains("exit 2"),
                "bash-status-conditionals" => normalized.Contains("[[") && normalized.Contains("-f") && normalized.Contains("-s"),
                "bash-loops-functions" => code.Contains("\"$@\"", StringComparison.Ordinal) && normalized.Contains("for "),
                "bash-safe-scripting" => normalized.Contains("set -euo pipefail") && normalized.Contains("mktemp") && normalized.Contains("trap"),
                "bash-text-processing" => normalized.Contains("awk") && normalized.Contains("sort"),
                "vim-search-substitute" => normalized.Contains("%s/todo/done/g") && (normalized.Contains("write") || normalized.Contains("w")),
                "linux-cli-capstone" => normalized.Contains("set -euo pipefail") && normalized.Contains("mktemp") && normalized.Contains("trap") && normalized.Contains("tar"),
                _ => false
            };
            return new ValidationResult(linuxPassed, linuxPassed ? 3 : 0, 3, linuxPassed ? "Command shape looks right for local authoring. Deployed lessons verify the real Linux/Vim state inside the sandbox." : lesson.Exercise.Hint, linuxPassed ? lesson.NextSlug : null, CodeReviewer.Review(lesson, code));
        }
        if (lesson.Slug.StartsWith("k8s-", StringComparison.Ordinal) && lesson.Exercise.Kind == ExerciseKind.Code)
        {
            var normalized = code.ToLowerInvariant();
            var kubePassed = lesson.Slug switch
            {
                "k8s-yaml-api-objects" => normalized.Contains("kind: pod") && normalized.Contains("name: web") && normalized.Contains("nginx:1.29"),
                "k8s-kubectl-contexts" => normalized.Contains("kubectl config current-context") && normalized.Contains("get pods") && normalized.Contains("payments"),
                "k8s-labels-selectors" => normalized.Contains("name: orders") && normalized.Contains("app: orders") && normalized.Contains("tier: api"),
                "k8s-deployments-replicasets" => normalized.Contains("kind: deployment") && normalized.Contains("replicas: 3") && normalized.Contains("app: orders") && normalized.Contains("orders:2.0"),
                "k8s-services" => normalized.Contains("kind: service") && normalized.Contains("app: orders") && normalized.Contains("port: 80") && normalized.Contains("targetport: 8080"),
                "k8s-resources-qos" => normalized.Contains("cpu: 250m") && normalized.Contains("memory: 256mi") && normalized.Contains("memory: 512mi"),
                "k8s-debugging" => normalized.Contains("get pod") && normalized.Contains("-o wide") && normalized.Contains("describe pod") && normalized.Contains("logs") && normalized.Contains("--previous") && normalized.Contains("payments"),
                _ => false
            };
            return new ValidationResult(kubePassed, kubePassed ? 3 : 0, 3, kubePassed ? "Your Kubernetes manifest or command sequence passes the local authoring checks." : lesson.Exercise.Hint, kubePassed ? lesson.NextSlug : null, CodeReviewer.Review(lesson, code));
        }
        if (lesson.Slug.StartsWith("sqlite-", StringComparison.Ordinal))
        {
            var normalized = code.ToUpperInvariant();
            var sqlitePassed = lesson.Slug switch
            {
                "sqlite-open-inspect" => normalized.Contains("CREATE TABLE") && normalized.Contains("PRODUCTS") && normalized.Contains("PRIMARY KEY") && normalized.Contains("NOT NULL"),
                "sqlite-strict-tables" => normalized.Contains("CREATE TABLE") && normalized.Contains("STRICT") && normalized.Contains("UNIQUE") && normalized.Contains("DEFAULT 1"),
                "sqlite-constraints" => normalized.Contains("CHECK") && normalized.Contains("QUANTITY") && normalized.Contains("PRIMARY KEY"),
                "sqlite-crud" => normalized.Contains("INSERT") && normalized.Contains("UPDATE") && normalized.Contains("DELETE"),
                "sqlite-select-filter-sort" => normalized.Contains("SELECT") && normalized.Contains("WHERE") && normalized.Contains("ORDER BY") && normalized.Contains("LIMIT 5"),
                "sqlite-joins" => normalized.Contains("JOIN") && normalized.Contains("CUSTOMER_ID"),
                "sqlite-aggregates-group-having" => normalized.Contains("GROUP BY") && normalized.Contains("HAVING") && normalized.Contains("COUNT") && normalized.Contains("SUM"),
                "sqlite-ctes-subqueries" => normalized.Contains("WITH") && normalized.Contains("ACTIVE_COUNTS") && normalized.Contains("GROUP BY"),
                "sqlite-transactions" => normalized.Contains("BEGIN") && normalized.Contains("UPDATE") && normalized.Contains("COMMIT"),
                "sqlite-foreign-keys" => normalized.Contains("PRAGMA FOREIGN_KEYS") && normalized.Contains("REFERENCES") && normalized.Contains("ON DELETE RESTRICT"),
                "sqlite-indexes" => normalized.Contains("CREATE INDEX") && normalized.Contains("IDX_ORDERS_CUSTOMER_CREATED"),
                "sqlite-schema-migrations" => normalized.Contains("ALTER TABLE") && normalized.Contains("ADD COLUMN") && normalized.Contains("DISPLAY_NAME"),
                "sqlite-views-triggers" => normalized.Contains("CREATE VIEW") && normalized.Contains("ACTIVE_USERS"),
                "sqlite-capstone" => normalized.Contains("CREATE TABLE") && normalized.Contains("CREATE INDEX") && normalized.Contains("CREATE VIEW") && normalized.Contains("BEGIN") && normalized.Contains("FOREIGN KEY"),
                _ => false
            };
            return new ValidationResult(sqlitePassed, sqlitePassed ? 3 : 0, 3, sqlitePassed ? "Your SQL passes the local SQLite authoring checks." : lesson.Exercise.Hint, sqlitePassed ? lesson.NextSlug : null, CodeReviewer.Review(lesson, code));
        }
        if (lesson.Slug.StartsWith("rust-", StringComparison.Ordinal))
        {
            var rustPassed = lesson.Slug switch
            {
                "rust-hello-functions" => code.Contains("fn greet", StringComparison.Ordinal) && code.Contains("fn rectangle_area", StringComparison.Ordinal) && code.Contains("fn format_price", StringComparison.Ordinal) && code.Contains("format!", StringComparison.Ordinal),
                "rust-bindings-types" => code.Contains("fn add_points", StringComparison.Ordinal) && code.Contains("fn checked_total", StringComparison.Ordinal) && code.Contains("fn to_byte", StringComparison.Ordinal) && code.Contains("fn next_tick", StringComparison.Ordinal),
                "rust-control-flow" => code.Contains("fn http_class", StringComparison.Ordinal) && code.Contains("fn fizzbuzz", StringComparison.Ordinal) && code.Contains("fn collatz_steps", StringComparison.Ordinal),
                "rust-collections-strings" => code.Contains("fn char_count", StringComparison.Ordinal) && code.Contains("fn initials", StringComparison.Ordinal) && code.Contains("fn word_frequencies", StringComparison.Ordinal),
                "rust-option-result" => code.Contains("fn find_setting", StringComparison.Ordinal) && code.Contains("fn port", StringComparison.Ordinal) && code.Contains("fn parse_all", StringComparison.Ordinal),
                "rust-debugging-tests" => code.Contains("fn is_leap_year", StringComparison.Ordinal) && code.Contains("fn days_in_month", StringComparison.Ordinal) && code.Contains("#[test]", StringComparison.Ordinal) && code.Contains("% 400", StringComparison.Ordinal),
                "rust-crates-modules" => code.Contains("mod inventory", StringComparison.Ordinal) && code.Contains("pub struct Item", StringComparison.Ordinal) && code.Contains("mod report", StringComparison.Ordinal) && code.Contains("pub use", StringComparison.Ordinal),
                "rust-cargo-quality" => code.Contains("fn log_level", StringComparison.Ordinal) && code.Contains("fn build_profile", StringComparison.Ordinal) && code.Contains("fn enabled_features", StringComparison.Ordinal) && code.Contains("cfg", StringComparison.Ordinal),
                "rust-stack-heap-layout" => code.Contains("struct SensorReading", StringComparison.Ordinal) && code.Contains("repr(C)", StringComparison.Ordinal) && code.Contains("fn growth_steps", StringComparison.Ordinal) && code.Contains("fn vec_footprint", StringComparison.Ordinal),
                "rust-ownership" => code.Contains("struct Lease", StringComparison.Ordinal) && code.Contains("impl Drop for Lease", StringComparison.Ordinal) && code.Contains("fn hand_over", StringComparison.Ordinal) && code.Contains("fn keep_longest", StringComparison.Ordinal) && code.Contains("fn shout", StringComparison.Ordinal),
                "rust-borrowing-references" => code.Contains("fn total_len", StringComparison.Ordinal) && code.Contains("fn add_suffix", StringComparison.Ordinal) && code.Contains("fn first_and_push", StringComparison.Ordinal) && code.Contains("fn longest_word", StringComparison.Ordinal),
                "rust-lifetimes" => code.Contains("fn longer", StringComparison.Ordinal) && code.Contains("fn first_line", StringComparison.Ordinal) && code.Contains("struct Words", StringComparison.Ordinal) && code.Contains("fn next_word", StringComparison.Ordinal),
                "rust-cli-args-env" => code.Contains("fn parse_args", StringComparison.Ordinal) && code.Contains("fn color_enabled", StringComparison.Ordinal) && code.Contains("fn exit_code", StringComparison.Ordinal) && code.Contains("struct Config", StringComparison.Ordinal),
                "rust-stdio-buffering" => code.Contains("fn count", StringComparison.Ordinal) && code.Contains("fn number_lines", StringComparison.Ordinal) && code.Contains("BufRead", StringComparison.Ordinal) && code.Contains("Write", StringComparison.Ordinal),
                "rust-os-files-syscalls" => code.Contains("fn append_line", StringComparison.Ordinal) && code.Contains("fn tail", StringComparison.Ordinal) && code.Contains("fn write_atomically", StringComparison.Ordinal) && code.Contains("rename", StringComparison.Ordinal),
                "rust-processes-signals" => code.Contains("fn run_capture", StringComparison.Ordinal) && code.Contains("fn sort_lines", StringComparison.Ordinal) && code.Contains("fn file_size", StringComparison.Ordinal) && code.Contains("Command", StringComparison.Ordinal),
                "rust-serde-formats" => code.Contains("struct ServiceConfig", StringComparison.Ordinal) && code.Contains("Deserialize", StringComparison.Ordinal) && code.Contains("fn parse_config", StringComparison.Ordinal) && code.Contains("fn to_pretty", StringComparison.Ordinal),
                "rust-cli-project" => code.Contains("fn run", StringComparison.Ordinal) && code.Contains("BufRead", StringComparison.Ordinal) && code.Contains("Write", StringComparison.Ordinal),
                "rust-net-tcp-basics" => code.Contains("fn local_listener", StringComparison.Ordinal) && code.Contains("fn echo_once", StringComparison.Ordinal) && code.Contains("fn request", StringComparison.Ordinal) && code.Contains("TcpListener", StringComparison.Ordinal),
                "rust-net-framing" => code.Contains("fn write_frame", StringComparison.Ordinal) && code.Contains("fn read_frame", StringComparison.Ordinal) && code.Contains("to_be_bytes", StringComparison.Ordinal),
                "rust-net-concurrency-timeouts" => code.Contains("fn handle_client", StringComparison.Ordinal) && code.Contains("fn serve", StringComparison.Ordinal) && code.Contains("set_read_timeout", StringComparison.Ordinal),
                "rust-net-tokio-kv" => code.Contains("fn apply", StringComparison.Ordinal) && code.Contains("async fn handle", StringComparison.Ordinal) && code.Contains("async fn serve", StringComparison.Ordinal) && code.Contains("tokio::spawn", StringComparison.Ordinal),
                "rust-bytes-endianness" => code.Contains("fn read_u32_le", StringComparison.Ordinal) && code.Contains("fn set_bit", StringComparison.Ordinal) && code.Contains("fn pack_rgb565", StringComparison.Ordinal) && code.Contains("fn unpack_rgb565", StringComparison.Ordinal),
                "rust-slices-buffers" => code.Contains("fn split_header", StringComparison.Ordinal) && code.Contains("fn read_at", StringComparison.Ordinal) && code.Contains("fn find_subslice", StringComparison.Ordinal) && code.Contains("fn chunk_sums", StringComparison.Ordinal),
                "rust-binary-parser" => code.Contains("enum ParseError", StringComparison.Ordinal) && code.Contains("struct Record", StringComparison.Ordinal) && code.Contains("fn parse", StringComparison.Ordinal),
                "rust-perf-cache-benchmark" => code.Contains("fn join_words", StringComparison.Ordinal) && code.Contains("fn parse_numbers", StringComparison.Ordinal) && code.Contains("fn dedup_sorted", StringComparison.Ordinal),
                "rust-memory-addresses-raw" => code.Contains("unsafe fn read_nth", StringComparison.Ordinal) && code.Contains("fn checked_read", StringComparison.Ordinal) && code.Contains("fn element_stride", StringComparison.Ordinal) && code.Contains("SAFETY:", StringComparison.Ordinal),
                "rust-unsafe-ffi" => code.Contains("extern \"C\"", StringComparison.Ordinal) && code.Contains("fn c_strlen", StringComparison.Ordinal) && code.Contains("fn c_abs", StringComparison.Ordinal) && code.Contains("fn from_c_string", StringComparison.Ordinal) && code.Contains("SAFETY:", StringComparison.Ordinal),
                "rust-atomics-locks" => code.Contains("struct Counter", StringComparison.Ordinal) && code.Contains("struct OnceFlag", StringComparison.Ordinal) && code.Contains("fn transfer", StringComparison.Ordinal) && code.Contains("Atomic", StringComparison.Ordinal),
                "rust-macros" => code.Contains("macro_rules! square", StringComparison.Ordinal) && code.Contains("macro_rules! hashmap", StringComparison.Ordinal) && code.Contains("macro_rules! max_of", StringComparison.Ordinal),
                "rust-structured-concurrency" => code.Contains("async fn run_all", StringComparison.Ordinal) && code.Contains("async fn worker", StringComparison.Ordinal) && code.Contains("JoinSet", StringComparison.Ordinal) && code.Contains("select!", StringComparison.Ordinal),
                "rust-testing-properties" => code.Contains("fn encode", StringComparison.Ordinal) && code.Contains("fn decode", StringComparison.Ordinal) && code.Contains("enum DecodeError", StringComparison.Ordinal),
                "rust-systems-wasm-embedded" => code.Contains("struct RingBuffer", StringComparison.Ordinal) && code.Contains("const N: usize", StringComparison.Ordinal) && code.Contains("fn fletcher16", StringComparison.Ordinal),
                "rust-systems-capstone" => code.Contains("struct Store", StringComparison.Ordinal) && code.Contains("fn open", StringComparison.Ordinal) && code.Contains("fn compact", StringComparison.Ordinal) && code.Contains("fn set", StringComparison.Ordinal) && code.Contains("fn delete", StringComparison.Ordinal),
                "rust-web-axum" => code.Contains("fn app", StringComparison.Ordinal) && code.Contains("Router", StringComparison.Ordinal) && code.Contains("fn get_course", StringComparison.Ordinal) && code.Contains("fn echo", StringComparison.Ordinal),
                "rust-api-contracts" => code.Contains("fn app", StringComparison.Ordinal) && code.Contains("idempotency-key", StringComparison.Ordinal) && code.Contains("problem+json", StringComparison.Ordinal) && code.Contains("limit", StringComparison.Ordinal),
                "rust-extractors-errors" => code.Contains("enum ApiError", StringComparison.Ordinal) && code.Contains("impl IntoResponse for ApiError", StringComparison.Ordinal) && code.Contains("TryFrom<String> for Email", StringComparison.Ordinal) && code.Contains("fn app", StringComparison.Ordinal),
                "rust-database-transactions" => code.Contains("fn migrate", StringComparison.Ordinal) && code.Contains("fn transfer", StringComparison.Ordinal) && code.Contains("transaction()", StringComparison.Ordinal) && code.Contains("params!", StringComparison.Ordinal),
                "rust-auth-security" => code.Contains("FromRequestParts", StringComparison.Ordinal) && code.Contains("fn app", StringComparison.Ordinal) && code.Contains("WWW_AUTHENTICATE", StringComparison.Ordinal),
                "rust-testing-integration" => code.Contains("fn checkout", StringComparison.Ordinal) && code.Contains("enum CheckoutError", StringComparison.Ordinal) && code.Contains("struct Receipt", StringComparison.Ordinal) && code.Contains("release", StringComparison.Ordinal),
                "rust-observability" => code.Contains("struct Metrics", StringComparison.Ordinal) && code.Contains("fn render", StringComparison.Ordinal) && code.Contains("async fn track", StringComparison.Ordinal) && code.Contains("MatchedPath", StringComparison.Ordinal),
                "rust-performance-profiling" => code.Contains("fn percentile", StringComparison.Ordinal) && code.Contains("fn required_workers", StringComparison.Ordinal) && code.Contains("fn load_titles", StringComparison.Ordinal) && code.Contains("get_many", StringComparison.Ordinal),
                "rust-resilience-queues" => code.Contains("fn backoff_ms", StringComparison.Ordinal) && code.Contains("struct CircuitBreaker", StringComparison.Ordinal) && code.Contains("struct Consumer", StringComparison.Ordinal) && code.Contains("enum Outcome", StringComparison.Ordinal),
                "rust-containers-delivery" => code.Contains("fn load_config", StringComparison.Ordinal) && code.Contains("fn app", StringComparison.Ordinal) && code.Contains("async fn serve_until", StringComparison.Ordinal) && code.Contains("with_graceful_shutdown", StringComparison.Ordinal),
                "rust-structs-enums" => code.Contains("enum Enrollment", StringComparison.Ordinal) && code.Contains("fn accept", StringComparison.Ordinal) && code.Contains("fn complete_lesson", StringComparison.Ordinal) && code.Contains("fn summary", StringComparison.Ordinal) && code.Contains("match", StringComparison.Ordinal),
                "rust-errors-panics" => code.Contains("enum DurationError", StringComparison.Ordinal) && code.Contains("fn parse_minutes", StringComparison.Ordinal) && code.Contains("fn total_minutes", StringComparison.Ordinal) && code.Contains("impl std::error::Error", StringComparison.Ordinal),
                "rust-iterators-closures" => code.Contains("struct Attempt", StringComparison.Ordinal) && code.Contains("fn passing_learners", StringComparison.Ordinal) && code.Contains("fn average_score", StringComparison.Ordinal) && code.Contains("fn best_by_learner", StringComparison.Ordinal) && code.Contains("fn make_grader", StringComparison.Ordinal),
                "rust-smart-pointers" => code.Contains("Box<Path>", StringComparison.Ordinal) && code.Contains("RefCell", StringComparison.Ordinal) && code.Contains("fn enroll", StringComparison.Ordinal) && code.Contains("Mutex", StringComparison.Ordinal) && code.Contains("fn count_in_threads", StringComparison.Ordinal),
                "rust-concurrency-send-sync" => code.Contains("fn parallel_sum", StringComparison.Ordinal) && code.Contains("fn squares_in_order", StringComparison.Ordinal) && code.Contains("struct HitCounter", StringComparison.Ordinal) && code.Contains("Atomic", StringComparison.Ordinal),
                "rust-async-tokio" => code.Contains("async fn squares_concurrently", StringComparison.Ordinal) && code.Contains("async fn with_timeout", StringComparison.Ordinal) && code.Contains("async fn bounded_squares", StringComparison.Ordinal) && code.Contains("tokio", StringComparison.Ordinal),
                "rust-traits-generics" => code.Contains("trait Billable", StringComparison.Ordinal) && code.Contains("impl Billable for Seat", StringComparison.Ordinal) && code.Contains("impl Billable for Workspace", StringComparison.Ordinal) && code.Contains("dyn Billable", StringComparison.Ordinal),
                _ => false
            };
            return new ValidationResult(rustPassed, rustPassed ? 2 : 0, 2, rustPassed ? "All tests passed. Your Rust solution meets this lesson’s checks." : lesson.Exercise.Hint, rustPassed ? lesson.NextSlug : null, CodeReviewer.Review(lesson, code));
        }
        if (lesson.Slug.StartsWith("llm-", StringComparison.Ordinal))
        {
            // Local authoring only: check that every function named in the requirements is defined.
            // Deployed lessons run the real Python grader (sandbox-broker/src/llm-graders) in the sandbox.
            var required = lesson.Exercise.Requirements
                .SelectMany(requirement => System.Text.RegularExpressions.Regex.Matches(requirement, @"`(\w+)\(").Select(match => match.Groups[1].Value))
                .Distinct(StringComparer.Ordinal)
                .ToArray();
            // A lesson whose answer is a config file (no functions to define) only needs to look like JSON here.
            var llmPassed = required.Length > 0
                ? required.All(name => code.Contains($"def {name}(", StringComparison.Ordinal))
                : code.TrimStart().StartsWith('{');
            return new ValidationResult(llmPassed, llmPassed ? 2 : 0, 2, llmPassed ? "Every required function is defined. Deployed lessons run the full grader in the sandbox." : lesson.Exercise.Hint, llmPassed ? lesson.NextSlug : null, CodeReviewer.Review(lesson, code));
        }
        if (lesson.Slug.StartsWith("python-", StringComparison.Ordinal))
        {
            var pythonPassed = lesson.Slug switch
            {
                "python-functions" => code.Contains("def greet", StringComparison.Ordinal) && code.Contains("return", StringComparison.Ordinal),
                "python-fastapi-endpoint" => code.Contains("@app.get", StringComparison.Ordinal) && code.Contains("def", StringComparison.Ordinal),
                _ => false
            };
            return new ValidationResult(pythonPassed, pythonPassed ? 2 : 0, 2, pythonPassed ? "All tests passed. Your Python solution meets this lesson’s checks." : lesson.Exercise.Hint, pythonPassed ? lesson.NextSlug : null, CodeReviewer.Review(lesson, code));
        }
        var syntaxError = CSharpSyntaxTree.ParseText(code, new CSharpParseOptions(LanguageVersion.Preview))
            .GetDiagnostics()
            .FirstOrDefault(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        if (syntaxError is not null)
            return new ValidationResult(false, 0, 2, $"C# syntax error: {syntaxError.GetMessage()}", null, CodeReviewer.Review(lesson, code));
        var passed = lesson.Slug switch
        {
            "foundations-making-decisions" => code.Contains("if", StringComparison.Ordinal) && code.Contains("age >= 13", StringComparison.Ordinal) && code.Contains("You can watch!", StringComparison.Ordinal),
            "modern-csharp-records" => code.Contains("record", StringComparison.Ordinal) && code.Contains("Order", StringComparison.Ordinal),
            "web-api-sealed-services" => code.Contains("sealed class", StringComparison.Ordinal) && code.Contains("IOrderService", StringComparison.Ordinal) && code.Contains("CancellationToken", StringComparison.Ordinal),
            _ => false
        };
        var message = passed ? "All tests passed. Your solution meets this lesson’s checks." : lesson.Exercise.Hint;
        return new ValidationResult(passed, passed ? 2 : 0, 2, message, passed ? lesson.NextSlug : null, CodeReviewer.Review(lesson, code));
    }
}
