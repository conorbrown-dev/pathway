use std::sync::Arc;
use std::sync::atomic::AtomicUsize;
use tokio::sync::{mpsc, watch};
use tokio::task::JoinSet;

async fn run_all(ids: Vec<u32>, done: Arc<AtomicUsize>) -> Result<Vec<u32>, String> {
    let mut set = JoinSet::new();
    for (index, id) in ids.iter().copied().enumerate() {
        let done = Arc::clone(&done);
        set.spawn(async move { (index, job(id, done).await) });
    }
    let mut results = vec![0; ids.len()];
    while let Some(joined) = set.join_next().await {
        let (index, outcome) = joined.map_err(|error| error.to_string())?;
        results[index] = outcome?;
    }
    Ok(results)
}

// Never looks at the shutdown signal: it only stops when every sender is gone.
async fn worker(mut jobs: mpsc::Receiver<u32>, _shutdown: watch::Receiver<bool>) -> u32 {
    let mut total = 0;
    while let Some(value) = jobs.recv().await {
        total += value;
    }
    total
}
