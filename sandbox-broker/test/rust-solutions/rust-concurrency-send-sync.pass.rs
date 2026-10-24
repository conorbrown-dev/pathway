use std::sync::atomic::{AtomicU64, Ordering};
use std::sync::mpsc;
use std::thread;

fn parallel_sum(numbers: &[u64], workers: usize) -> u64 {
    if numbers.is_empty() {
        return 0;
    }
    let chunk_size = numbers.len().div_ceil(workers.max(1));
    thread::scope(|scope| {
        let handles: Vec<_> = numbers
            .chunks(chunk_size)
            .map(|chunk| scope.spawn(move || chunk.iter().sum::<u64>()))
            .collect();
        handles.into_iter().map(|handle| handle.join().unwrap()).sum()
    })
}

fn squares_in_order(jobs: Vec<u64>) -> Vec<u64> {
    let (sender, receiver) = mpsc::channel();
    let count = jobs.len();
    for (index, job) in jobs.into_iter().enumerate() {
        let sender = sender.clone();
        thread::spawn(move || {
            sender.send((index, job * job)).unwrap();
        });
    }
    drop(sender);
    let mut results = vec![0; count];
    for (index, square) in receiver {
        results[index] = square;
    }
    results
}

struct HitCounter {
    hits: AtomicU64,
}

impl HitCounter {
    fn new() -> Self {
        HitCounter { hits: AtomicU64::new(0) }
    }

    fn record(&self) {
        self.hits.fetch_add(1, Ordering::Relaxed);
    }

    fn total(&self) -> u64 {
        self.hits.load(Ordering::Relaxed)
    }
}
