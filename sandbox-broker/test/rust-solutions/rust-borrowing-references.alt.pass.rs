fn total_len(names: &[String]) -> usize {
    let mut total = 0;
    for name in names {
        total += name.len();
    }
    total
}

fn add_suffix(names: &mut [String], suffix: &str) {
    for name in names {
        *name = format!("{name}{suffix}");
    }
}

fn first_and_push(values: &mut Vec<i32>, extra: i32) -> i32 {
    values.push(extra);
    values[0]
}

fn longest_word(text: &str) -> &str {
    text.split_whitespace().fold("", |best, word| if word.len() > best.len() { word } else { best })
}
