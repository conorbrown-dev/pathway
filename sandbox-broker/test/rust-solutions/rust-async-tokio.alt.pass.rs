use tokio::task::JoinSet;

async fn squares_concurrently(numbers: Vec<u64>) -> Vec<u64> {
    let mut set = JoinSet::new();
    for (index, number) in numbers.iter().copied().enumerate() {
        set.spawn(async move { (index, slow_square(number).await) });
    }
    let mut results = vec![0; numbers.len()];
    while let Some(joined) = set.join_next().await {
        let (index, square) = joined.expect("task panicked");
        results[index] = square;
    }
    results
}

async fn with_timeout(number: u64, limit_ms: u64) -> Option<u64> {
    tokio::select! {
        square = slow_square(number) => Some(square),
        _ = tokio::time::sleep(std::time::Duration::from_millis(limit_ms)) => None,
    }
}

async fn bounded_squares(numbers: Vec<u64>, max_in_flight: usize) -> Vec<u64> {
    let mut results = Vec::with_capacity(numbers.len());
    for batch in numbers.chunks(max_in_flight.max(1)) {
        results.extend(squares_concurrently(batch.to_vec()).await);
    }
    results
}
