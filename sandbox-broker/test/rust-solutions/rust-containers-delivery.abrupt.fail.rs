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

fn load_config(env: &HashMap<String, String>) -> Result<Config, Vec<String>> {
    let mut problems = Vec::new();
    let port = match env.get("PORT").map(|value| value.parse::<u16>()) {
        Some(Ok(port)) if port > 0 => Some(port),
        Some(_) => {
            problems.push(String::from("PORT must be a number from 1 to 65535"));
            None
        }
        None => {
            problems.push(String::from("PORT is required"));
            None
        }
    };
    let database_url = env.get("DATABASE_URL").filter(|value| !value.is_empty()).cloned();
    if database_url.is_none() {
        // Never echo the value: connection strings contain passwords.
        problems.push(String::from("DATABASE_URL is required"));
    }
    let log_level = env.get("LOG_LEVEL").cloned().unwrap_or_else(|| String::from("info"));
    if !["error", "warn", "info", "debug", "trace"].contains(&log_level.as_str()) {
        problems.push(String::from("LOG_LEVEL must be one of error, warn, info, debug, trace"));
    }
    match (port, database_url) {
        (Some(port), Some(database_url)) if problems.is_empty() => Ok(Config { port, database_url, log_level }),
        _ => Err(problems),
    }
}

struct Health {
    ready: AtomicBool,
    draining: AtomicBool,
}

async fn livez() -> StatusCode {
    StatusCode::OK
}

async fn readyz(State(health): State<Arc<Health>>) -> StatusCode {
    if health.ready.load(Ordering::SeqCst) && !health.draining.load(Ordering::SeqCst) {
        StatusCode::OK
    } else {
        StatusCode::SERVICE_UNAVAILABLE
    }
}

fn app(health: Arc<Health>) -> Router {
    Router::new().route("/livez", get(livez)).route("/readyz", get(readyz)).with_state(health)
}

async fn serve_until(listener: tokio::net::TcpListener, app: Router, health: Arc<Health>, shutdown: tokio::sync::oneshot::Receiver<()>) -> std::io::Result<()> {
    // Returns as soon as the signal arrives, without waiting for in-flight requests;
    // main then exits and the runtime cuts them off.
    tokio::select! {
        result = axum::serve(listener, app) => result,
        _ = shutdown => {
            health.draining.store(true, Ordering::SeqCst);
            Ok(())
        }
    }
}
