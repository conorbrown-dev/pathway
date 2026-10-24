use std::collections::BTreeMap;
struct Attempt { learner: String, score: u32 }
fn passing_learners<'a>(attempts: &'a [Attempt], pass_mark: u32) -> Vec<&'a str> {
    let mut names = Vec::new();
    for a in attempts { if a.score >= pass_mark { names.push(&a.learner[..]); } }
    names
}
fn average_score(attempts: &[Attempt]) -> Option<f64> {
    let count = attempts.len();
    (count > 0).then(|| attempts.iter().map(|a| a.score as f64).sum::<f64>() / count as f64)
}
fn best_by_learner(attempts: &[Attempt]) -> BTreeMap<String, u32> {
    attempts.iter().fold(BTreeMap::new(), |mut map, a| {
        map.entry(a.learner.clone()).and_modify(|s: &mut u32| *s = (*s).max(a.score)).or_insert(a.score);
        map
    })
}
fn make_grader(pass_mark: u32) -> Box<dyn Fn(u32) -> bool> { Box::new(move |s| s >= pass_mark) }
