// Image smoke test: uses every prebuilt crate the way an exercise would, with no network.
use axum::{Json, Router, body::Body, http::Request, routing::get};
use http_body_util::BodyExt;
use serde::{Deserialize, Serialize};
use tower::ServiceExt;

#[derive(Serialize, Deserialize)]
struct Health {
    status: String,
}

#[derive(Debug, thiserror::Error)]
#[error("probe failed: {0}")]
struct ProbeError(String);

async fn health() -> Json<Health> {
    Json(Health { status: String::from("ok") })
}

async fn run() -> anyhow::Result<()> {
    let app = Router::new().route("/health", get(health));
    let response = app.oneshot(Request::builder().uri("/health").body(Body::empty())?).await?;
    if response.status() != 200 {
        return Err(ProbeError(format!("status {}", response.status())).into());
    }
    let bytes = response.into_body().collect().await?.to_bytes();
    let body: Health = serde_json::from_slice(&bytes)?;
    if body.status != "ok" {
        return Err(ProbeError(body.status).into());
    }
    let (sender, mut receiver) = tokio::sync::mpsc::channel(1);
    tokio::spawn(async move { sender.send(42u32).await });
    let value = tokio::time::timeout(std::time::Duration::from_secs(2), receiver.recv()).await?;
    if value != Some(42) {
        return Err(ProbeError(String::from("channel")).into());
    }
    Ok(())
}

#[tokio::main(flavor = "current_thread")]
async fn main() -> anyhow::Result<()> {
    run().await?;
    println!("PATHWAY_TEST_PASS");
    Ok(())
}
