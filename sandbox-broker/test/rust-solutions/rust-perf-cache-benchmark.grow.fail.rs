// Correct results, but every push and format! may allocate.
fn join_words(words: &[&str], separator: &str) -> String {
    let mut joined = String::new();
    for (index, word) in words.iter().enumerate() {
        if index > 0 {
            joined = format!("{joined}{separator}");
        }
        joined = format!("{joined}{word}");
    }
    joined
}

fn parse_numbers(line: &str) -> Vec<u32> {
    let parts: Vec<String> = line.split(',').map(|part| part.trim().to_string()).collect();
    parts.iter().filter_map(|part| part.parse().ok()).collect()
}

fn dedup_sorted(values: &mut Vec<u32>) {
    let mut unique = Vec::new();
    for &value in values.iter() {
        if unique.last() != Some(&value) {
            unique.push(value);
        }
    }
    *values = unique;
}
