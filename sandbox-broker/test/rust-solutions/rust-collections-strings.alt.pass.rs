use std::collections::BTreeMap;

fn char_count(text: &str) -> usize {
    let mut count = 0;
    for _ in text.chars() {
        count += 1;
    }
    count
}

fn initials(name: &str) -> String {
    let mut result = String::new();
    for word in name.split_whitespace() {
        if let Some(first) = word.chars().next() {
            for upper in first.to_uppercase() {
                result.push(upper);
            }
        }
    }
    result
}

fn word_frequencies(text: &str) -> Vec<(String, usize)> {
    // A BTreeMap keeps words in alphabetical order, so a stable sort by count is enough.
    let mut counts = BTreeMap::new();
    for raw in text.split_whitespace() {
        let word: String = raw.trim_matches(|c: char| !c.is_alphanumeric()).to_lowercase();
        if !word.is_empty() {
            *counts.entry(word).or_insert(0usize) += 1;
        }
    }
    let mut pairs: Vec<(String, usize)> = counts.into_iter().collect();
    pairs.sort_by(|a, b| b.1.cmp(&a.1));
    pairs
}
