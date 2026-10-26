fn longer<'a>(a: &'a str, b: &'a str) -> &'a str {
    if b.len() > a.len() { b } else { a }
}

fn first_line(text: &str) -> &str {
    text.lines().next().unwrap_or("")
}

struct Words<'a> {
    text: &'a str,
    pos: usize,
}

impl<'a> Words<'a> {
    fn new(text: &'a str) -> Words<'a> {
        Words { text, pos: 0 }
    }

    // Elision ties the returned word to `&mut self`, so a caller can't keep one word
    // while asking for the next, or use words after the Words value is gone.
    fn next_word(&mut self) -> Option<&str> {
        let rest = &self.text[self.pos..];
        let start = rest.find(|c: char| !c.is_whitespace())?;
        let after = &rest[start..];
        let len = after.find(char::is_whitespace).unwrap_or(after.len());
        self.pos += start + len;
        Some(&after[..len])
    }
}
