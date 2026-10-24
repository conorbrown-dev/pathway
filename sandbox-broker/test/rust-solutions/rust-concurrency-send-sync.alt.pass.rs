use std::sync::{atomic::{AtomicU64, Ordering}, mpsc, Arc};
fn parallel_sum(numbers: &[u64], workers: usize) -> u64 {
    let workers = workers.max(1);
    let (tx, rx) = mpsc::channel();
    std::thread::scope(|s| {
        for w in 0..workers {
            let tx = tx.clone();
            s.spawn(move || {
                let part: u64 = numbers.iter().skip(w).step_by(workers).sum();
                tx.send(part).unwrap();
            });
        }
    });
    drop(tx);
    rx.iter().sum()
}
fn squares_in_order(jobs: Vec<u64>) -> Vec<u64> {
    let handles: Vec<_> = jobs.into_iter().map(|j| std::thread::spawn(move || j * j)).collect();
    handles.into_iter().map(|h| h.join().unwrap()).collect()
}
struct HitCounter { hits: AtomicU64 }
impl HitCounter {
    fn new() -> Self { Self { hits: AtomicU64::new(0) } }
    fn record(&self) { self.hits.fetch_add(1, Ordering::SeqCst); }
    fn total(&self) -> u64 { self.hits.load(Ordering::SeqCst) }
}
#[allow(dead_code)] fn _unused(_: Arc<u8>) {}
