fn percentile(samples: &mut [u64], p: f64) -> Option<u64> {
    if samples.is_empty() || !(0.0..=100.0).contains(&p) {
        return None;
    }
    samples.sort_unstable();
    let rank = ((p / 100.0) * samples.len() as f64).ceil() as usize;
    Some(samples[rank.max(1) - 1])
}

fn required_workers(requests_per_second: f64, latency_ms: f64) -> u64 {
    (requests_per_second * latency_ms / 1000.0).ceil() as u64
}

// One query per id: the classic N+1.
fn load_titles(ids: &[u64], db: &dyn CourseDb) -> Vec<String> {
    ids.iter().filter_map(|id| db.get(*id)).collect()
}
