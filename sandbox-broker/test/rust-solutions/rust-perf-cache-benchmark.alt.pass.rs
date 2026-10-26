fn join_words(words: &[&str], separator: &str) -> String {
    words.join(separator)
}

fn parse_numbers(line: &str) -> Vec<u32> {
    let mut numbers = Vec::with_capacity(line.bytes().filter(|&b| b == b',').count() + 1);
    for part in line.split(',') {
        if let Ok(number) = part.trim().parse() {
            numbers.push(number);
        }
    }
    numbers
}

fn dedup_sorted(values: &mut Vec<u32>) {
    let mut write = 0;
    for read in 0..values.len() {
        if write == 0 || values[read] != values[write - 1] {
            values[write] = values[read];
            write += 1;
        }
    }
    values.truncate(write);
}
