fn join_words(words: &[&str], separator: &str) -> String {
    if words.is_empty() {
        return String::new();
    }
    let total = words.iter().map(|word| word.len()).sum::<usize>() + separator.len() * (words.len() - 1);
    let mut joined = String::with_capacity(total);
    for (index, word) in words.iter().enumerate() {
        if index > 0 {
            joined.push_str(separator);
        }
        joined.push_str(word);
    }
    joined
}

fn parse_numbers(line: &str) -> Vec<u32> {
    let count = line.split(',').filter(|part| part.trim().parse::<u32>().is_ok()).count();
    let mut numbers = Vec::with_capacity(count);
    numbers.extend(line.split(',').filter_map(|part| part.trim().parse::<u32>().ok()));
    numbers
}

fn dedup_sorted(values: &mut Vec<u32>) {
    values.dedup();
}
