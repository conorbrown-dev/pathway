// Interpolates by truncating the index, so the 99th percentile of 100 samples is the 100th value
// only by luck, and small sample sets report the wrong value.
fn percentile(samples: &mut [u64], p: f64) -> Option<u64> {
    if samples.is_empty() {
        return None;
    }
    samples.sort_unstable();
    let index = ((p / 100.0) * (samples.len() as f64)) as usize;
    samples.get(index.min(samples.len() - 1)).copied()
}

fn required_workers(requests_per_second: f64, latency_ms: f64) -> u64 {
    (requests_per_second * latency_ms / 1000.0) as u64
}

fn load_titles(ids: &[u64], db: &dyn CourseDb) -> Vec<String> {
    let found: std::collections::HashMap<u64, String> = db.get_many(ids).into_iter().collect();
    ids.iter().filter_map(|id| found.get(id).cloned()).collect()
}
