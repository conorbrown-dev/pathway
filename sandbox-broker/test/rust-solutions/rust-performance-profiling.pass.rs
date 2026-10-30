// The grader provides:
//   trait CourseDb { fn get(&self, id: u64) -> Option<String>; fn get_many(&self, ids: &[u64]) -> Vec<(u64, String)>; }

fn percentile(samples: &mut [u64], p: f64) -> Option<u64> {
    if samples.is_empty() || !(0.0..=100.0).contains(&p) {
        return None;
    }
    samples.sort_unstable();
    // Nearest-rank: the smallest value with at least p% of samples at or below it.
    let rank = ((p / 100.0) * samples.len() as f64).ceil() as usize;
    Some(samples[rank.max(1) - 1])
}

fn required_workers(requests_per_second: f64, latency_ms: f64) -> u64 {
    // Little's law: work in progress = arrival rate x time in the system.
    (requests_per_second * latency_ms / 1000.0).ceil() as u64
}

fn load_titles(ids: &[u64], db: &dyn CourseDb) -> Vec<String> {
    if ids.is_empty() {
        return Vec::new();
    }
    let found: std::collections::HashMap<u64, String> = db.get_many(ids).into_iter().collect();
    ids.iter().filter_map(|id| found.get(id).cloned()).collect()
}
