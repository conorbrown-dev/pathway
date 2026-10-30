use std::collections::HashMap;
use std::sync::{Arc, Mutex};

use axum::extract::{Path, Query, State};
use axum::http::{HeaderMap, StatusCode, header};
use axum::response::{IntoResponse, Response};
use axum::routing::get;
use axum::{Json, Router};
use serde::{Deserialize, Serialize};

#[derive(Clone, Serialize)]
struct Enrollment {
    id: u64,
    learner: String,
    course: String,
}

#[derive(Deserialize)]
struct NewEnrollment {
    learner: String,
    course: String,
}

#[derive(Default)]
struct Store {
    enrollments: Vec<Enrollment>,
    by_key: HashMap<String, u64>,
}

type Db = Arc<Mutex<Store>>;

#[derive(Deserialize)]
struct Page {
    limit: Option<usize>,
    after: Option<u64>,
}

#[derive(Serialize)]
struct PageOf {
    items: Vec<Enrollment>,
    next: Option<u64>,
}

fn problem(status: StatusCode, detail: &str) -> Response {
    let body = serde_json::json!({
        "type": "about:blank",
        "title": status.canonical_reason().unwrap_or("Error"),
        "status": status.as_u16(),
        "detail": detail,
    });
    (status, [(header::CONTENT_TYPE, "application/problem+json")], body.to_string()).into_response()
}

fn created(enrollment: &Enrollment) -> Response {
    let location = format!("/v1/enrollments/{}", enrollment.id);
    (StatusCode::CREATED, [(header::LOCATION, location)], Json(enrollment.clone())).into_response()
}

async fn create(State(db): State<Db>, headers: HeaderMap, Json(input): Json<NewEnrollment>) -> Response {
    let Some(key) = headers.get("idempotency-key").and_then(|value| value.to_str().ok()).filter(|key| !key.is_empty()) else {
        return problem(StatusCode::BAD_REQUEST, "an Idempotency-Key header is required");
    };
    if input.learner.trim().is_empty() || input.course.trim().is_empty() {
        return problem(StatusCode::UNPROCESSABLE_ENTITY, "learner and course must not be empty");
    }
    let mut store = db.lock().unwrap();
    if let Some(&id) = store.by_key.get(key) {
        let existing = store.enrollments.iter().find(|e| e.id == id).unwrap().clone();
        return created(&existing);
    }
    let enrollment = Enrollment { id: store.enrollments.len() as u64 + 1, learner: input.learner, course: input.course };
    store.by_key.insert(key.to_string(), enrollment.id);
    store.enrollments.push(enrollment.clone());
    created(&enrollment)
}

async fn list(State(db): State<Db>, Query(page): Query<Page>) -> Response {
    let limit = page.limit.unwrap_or(20);
    if limit == 0 {
        return problem(StatusCode::UNPROCESSABLE_ENTITY, "limit must be at least 1");
    }
    let limit = limit.min(50);
    let store = db.lock().unwrap();
    let after = page.after.unwrap_or(0);
    let remaining: Vec<&Enrollment> = store.enrollments.iter().filter(|e| e.id > after).collect();
    let items: Vec<Enrollment> = remaining.iter().take(limit).map(|e| (*e).clone()).collect();
    let next = if remaining.len() > limit { items.last().map(|e| e.id) } else { None };
    Json(PageOf { items, next }).into_response()
}

async fn fetch(State(db): State<Db>, Path(id): Path<u64>) -> Response {
    let store = db.lock().unwrap();
    match store.enrollments.iter().find(|e| e.id == id) {
        Some(enrollment) => Json(enrollment.clone()).into_response(),
        None => problem(StatusCode::NOT_FOUND, "no enrollment with that id"),
    }
}

fn app(db: Db) -> Router {
    Router::new()
        .route("/v1/enrollments", get(list).post(create))
        .route("/v1/enrollments/{id}", get(fetch))
        .with_state(db)
}
