use std::collections::BTreeMap;

struct Attempt {
    learner: String,
    score: u32,
}

fn passing_learners(attempts: &[Attempt], pass_mark: u32) -> Vec<&str> {
    attempts
        .iter()
        .filter(|attempt| attempt.score >= pass_mark)
        .map(|attempt| attempt.learner.as_str())
        .collect()
}

fn average_score(attempts: &[Attempt]) -> Option<f64> {
    if attempts.is_empty() {
        return None;
    }
    let total: u32 = attempts.iter().map(|attempt| attempt.score).sum();
    Some(f64::from(total / attempts.len() as u32))
}

fn best_by_learner(attempts: &[Attempt]) -> BTreeMap<String, u32> {
    let mut best = BTreeMap::new();
    for attempt in attempts {
        let entry = best.entry(attempt.learner.clone()).or_insert(attempt.score);
        *entry = (*entry).max(attempt.score);
    }
    best
}

fn make_grader(pass_mark: u32) -> impl Fn(u32) -> bool {
    move |score| score >= pass_mark
}
