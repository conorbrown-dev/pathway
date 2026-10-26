fn total_len(names: &[String]) -> usize {
    names.iter().map(|name| name.len()).sum()
}

fn add_suffix(names: &mut [String], suffix: &str) {
    for name in names.iter_mut() {
        name.push_str(suffix);
    }
}

// Sidesteps the borrow error by returning the wrong element.
fn first_and_push(values: &mut Vec<i32>, extra: i32) -> i32 {
    values.push(extra);
    extra
}

// max_by_key returns the LAST of equally long words.
fn longest_word(text: &str) -> &str {
    text.split_whitespace().max_by_key(|word| word.len()).unwrap_or("")
}
