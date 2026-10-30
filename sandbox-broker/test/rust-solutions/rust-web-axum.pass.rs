use axum::extract::Path;
use axum::http::StatusCode;
use axum::routing::{get, post};
use axum::{Json, Router};

// The grader provides: struct Course { id: String, title: String } (Serialize, Clone) and fn catalog() -> Vec<Course>

async fn health() -> &'static str {
    "ok"
}

async fn get_course(Path(id): Path<String>) -> Result<Json<Course>, StatusCode> {
    catalog().into_iter().find(|course| course.id == id).map(Json).ok_or(StatusCode::NOT_FOUND)
}

async fn echo(Json(value): Json<serde_json::Value>) -> Json<serde_json::Value> {
    Json(value)
}

fn app() -> Router {
    Router::new()
        .route("/health", get(health))
        .route("/courses/{id}", get(get_course))
        .route("/echo", post(echo))
}
