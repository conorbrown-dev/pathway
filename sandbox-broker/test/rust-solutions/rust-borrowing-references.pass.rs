fn total_len(names: &[String]) -> usize {
    names.iter().map(|name| name.len()).sum()
}

fn add_suffix(names: &mut [String], suffix: &str) {
    for name in names.iter_mut() {
        name.push_str(suffix);
    }
}

fn first_and_push(values: &mut Vec<i32>, extra: i32) -> i32 {
    let first = values[0]; // copy the i32 out; the borrow ends here
    values.push(extra);
    first
}

fn longest_word(text: &str) -> &str {
    let mut best = "";
    for word in text.split_whitespace() {
        if word.len() > best.len() {
            best = word;
        }
    }
    best
}
