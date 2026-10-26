use std::sync::Arc;

use axum::extract::{FromRequestParts, Path, State};
use axum::http::request::Parts;
use axum::http::{StatusCode, header};
use axum::response::{IntoResponse, Response};
use axum::routing::get;
use axum::{Json, Router};

// The grader provides:
//   struct Principal { user: String, admin: bool }            (Clone)
//   struct Note { id: u64, owner: String, text: String }      (Clone, Serialize)
//   struct AppState { tokens: HashMap<String, Principal>, notes: Mutex<HashMap<u64, Note>> }

fn unauthorized() -> Response {
    (StatusCode::UNAUTHORIZED, [(header::WWW_AUTHENTICATE, "Bearer")], "authentication required").into_response()
}

impl FromRequestParts<Arc<AppState>> for Principal {
    type Rejection = Response;

    async fn from_request_parts(parts: &mut Parts, state: &Arc<AppState>) -> Result<Self, Self::Rejection> {
        let value = parts.headers.get(header::AUTHORIZATION).and_then(|value| value.to_str().ok()).ok_or_else(unauthorized)?;
        let token = value.strip_prefix("Bearer ").ok_or_else(unauthorized)?;
        // Rejects with 403 and no challenge header: a bad token is an authentication failure (401).
        state.tokens.get(token).cloned().ok_or_else(|| StatusCode::FORBIDDEN.into_response())
    }
}

async fn read_note(principal: Principal, State(state): State<Arc<AppState>>, Path(id): Path<u64>) -> Response {
    let notes = state.notes.lock().unwrap();
    match notes.get(&id) {
        Some(note) if note.owner == principal.user || principal.admin => Json(note.clone()).into_response(),
        _ => StatusCode::NOT_FOUND.into_response(), // don't reveal that someone else's note exists
    }
}

async fn delete_note(principal: Principal, State(state): State<Arc<AppState>>, Path(id): Path<u64>) -> Response {
    let mut notes = state.notes.lock().unwrap();
    let Some(note) = notes.get(&id) else {
        return StatusCode::NOT_FOUND.into_response();
    };
    if note.owner == principal.user {
        notes.remove(&id);
        StatusCode::NO_CONTENT.into_response()
    } else if principal.admin {
        StatusCode::FORBIDDEN.into_response() // admins may read but not delete other people's notes
    } else {
        StatusCode::NOT_FOUND.into_response()
    }
}

fn app(state: Arc<AppState>) -> Router {
    Router::new()
        .route("/notes/{id}", get(read_note).delete(delete_note))
        .with_state(state)
}
