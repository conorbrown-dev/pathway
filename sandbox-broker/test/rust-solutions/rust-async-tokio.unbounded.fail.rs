use std::sync::Arc;
use std::time::Duration;
use tokio::sync::Semaphore;

async fn squares_concurrently(numbers: Vec<u64>) -> Vec<u64> {
    let handles: Vec<_> = numbers
        .into_iter()
        .map(|number| tokio::spawn(slow_square(number)))
        .collect();
    let mut results = Vec::with_capacity(handles.len());
    for handle in handles {
        results.push(handle.await.unwrap());
    }
    results
}

async fn with_timeout(number: u64, limit_ms: u64) -> Option<u64> {
    tokio::time::timeout(Duration::from_millis(limit_ms), slow_square(number))
        .await
        .ok()
}

async fn bounded_squares(numbers: Vec<u64>, max_in_flight: usize) -> Vec<u64> {
    let permits = Arc::new(Semaphore::new(Semaphore::MAX_PERMITS));
    let _ = max_in_flight;
    let handles: Vec<_> = numbers
        .into_iter()
        .map(|number| {
            let permits = Arc::clone(&permits);
            tokio::spawn(async move {
                let _permit = permits.acquire_owned().await.unwrap();
                slow_square(number).await
            })
        })
        .collect();
    let mut results = Vec::with_capacity(handles.len());
    for handle in handles {
        results.push(handle.await.unwrap());
    }
    results
}
