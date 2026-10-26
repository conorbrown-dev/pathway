use axum::extract::Path;
use axum::routing::{get, post};
use axum::{Json, Router};

async fn health() -> &'static str {
    "ok"
}

// Returns 200 with null for an unknown course instead of 404.
async fn get_course(Path(id): Path<String>) -> Json<Option<Course>> {
    Json(catalog().into_iter().find(|course| course.id == id))
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
