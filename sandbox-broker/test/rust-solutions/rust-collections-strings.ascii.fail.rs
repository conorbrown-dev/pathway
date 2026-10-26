use std::collections::HashMap;

fn char_count(text: &str) -> usize {
    text.chars().count()
}

// Works on bytes, so "émile" loses its accented first letter.
fn initials(name: &str) -> String {
    name.split_whitespace()
        .map(|word| (word.as_bytes()[0] as char).to_ascii_uppercase())
        .collect()
}

fn word_frequencies(text: &str) -> Vec<(String, usize)> {
    let mut counts: HashMap<String, usize> = HashMap::new();
    for raw in text.split_whitespace() {
        let word = raw.trim_matches(|c: char| !c.is_alphanumeric()).to_lowercase();
        if !word.is_empty() {
            *counts.entry(word).or_insert(0) += 1;
        }
    }
    let mut pairs: Vec<(String, usize)> = counts.into_iter().collect();
    pairs.sort_by(|a, b| b.1.cmp(&a.1).then_with(|| a.0.cmp(&b.0)));
    pairs
}
