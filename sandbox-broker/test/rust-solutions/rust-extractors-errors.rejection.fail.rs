use std::sync::{Arc, Mutex};

use axum::extract::rejection::PathRejection;
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

impl IntoResponse for ApiError {
    fn into_response(self) -> Response {
        let (status, detail) = match self {
            ApiError::BadRequest(detail) => (StatusCode::BAD_REQUEST, detail),
            ApiError::NotFound(detail) => (StatusCode::NOT_FOUND, detail),
            ApiError::Conflict(detail) => (StatusCode::CONFLICT, detail),
            ApiError::Validation(detail) => (StatusCode::UNPROCESSABLE_ENTITY, detail),
        };
        let body = serde_json::json!({
            "title": status.canonical_reason().unwrap_or("Error"),
            "status": status.as_u16(),
            "detail": detail,
        });
        (status, [(header::CONTENT_TYPE, "application/problem+json")], body.to_string()).into_response()
    }
}

#[derive(Debug, Clone, PartialEq, Serialize)]
struct Email(String);

impl TryFrom<String> for Email {
    type Error = ApiError;

    fn try_from(raw: String) -> Result<Self, Self::Error> {
        let email = raw.trim().to_lowercase();
        let valid = email.len() <= 254
            && email.matches('@').count() == 1
            && email.split_once('@').is_some_and(|(local, domain)| !local.is_empty() && domain.contains('.') && !domain.starts_with('.') && !domain.ends_with('.'));
        if valid { Ok(Email(email)) } else { Err(ApiError::Validation(format!("'{}' is not a valid email address", raw.trim()))) }
    }
}

#[derive(Deserialize)]
struct NewUser {
    email: String,
}

#[derive(Clone, Serialize)]
struct User {
    id: u64,
    email: Email,
}

type Users = Arc<Mutex<Vec<User>>>;

// Uses the plain Json extractor, so malformed bodies get axum's plain-text rejection, not a problem document.
async fn create_user(State(users): State<Users>, Json(input): Json<NewUser>) -> Result<(StatusCode, Json<User>), ApiError> {
    let email = Email::try_from(input.email)?;
    let mut users = users.lock().unwrap();
    if users.iter().any(|user| user.email == email) {
        return Err(ApiError::Conflict(String::from("a user with that email already exists")));
    }
    let user = User { id: users.len() as u64 + 1, email };
    users.push(user.clone());
    Ok((StatusCode::CREATED, Json(user)))
}

async fn get_user(State(users): State<Users>, id: Result<Path<u64>, PathRejection>) -> Result<Json<User>, ApiError> {
    let Path(id) = id.map_err(|rejection| ApiError::BadRequest(rejection.body_text()))?;
    let users = users.lock().unwrap();
    users.iter().find(|user| user.id == id).cloned().map(Json).ok_or_else(|| ApiError::NotFound(format!("no user {id}")))
}

fn app(users: Users) -> Router {
    Router::new()
        .route("/users", post(create_user))
        .route("/users/{id}", get(get_user))
        .with_state(users)
}
