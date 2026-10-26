// On a tie this returns the second string, not the first.
fn longer<'a>(a: &'a str, b: &'a str) -> &'a str {
    if a.len() > b.len() { a } else { b }
}

fn first_line(text: &str) -> &str {
    text.lines().next().unwrap_or("")
}

struct Words<'a> {
    rest: std::str::SplitWhitespace<'a>,
}

impl<'a> Words<'a> {
    fn new(text: &'a str) -> Words<'a> {
        Words { rest: text.split_whitespace() }
    }

    fn next_word(&mut self) -> Option<&'a str> {
        self.rest.next()
    }
}
