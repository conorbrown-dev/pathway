use std::sync::Arc;
use std::sync::atomic::AtomicUsize;
use tokio::sync::{mpsc, watch};

// tokio::spawn handles are detached: returning early leaves the other jobs running.
async fn run_all(ids: Vec<u32>, done: Arc<AtomicUsize>) -> Result<Vec<u32>, String> {
    let handles: Vec<_> = ids.into_iter().map(|id| tokio::spawn(job(id, Arc::clone(&done)))).collect();
    let mut results = Vec::new();
    for handle in handles {
        results.push(handle.await.map_err(|e| e.to_string())??);
    }
    Ok(results)
}

async fn worker(mut jobs: mpsc::Receiver<u32>, mut shutdown: watch::Receiver<bool>) -> u32 {
    let mut total = 0;
    loop {
        tokio::select! {
            _ = shutdown.changed() => return total,
            job = jobs.recv() => match job {
                Some(value) => total += value,
                None => return total,
            },
        }
    }
}
