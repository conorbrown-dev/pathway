use std::fs;
use std::io::{self, Write};
use std::path::Path;

// Opens without append: each call overwrites the file instead of adding to it.
fn append_line(path: &Path, line: &str) -> io::Result<()> {
    let mut file = fs::File::create(path)?;
    writeln!(file, "{line}")
}

fn tail(path: &Path, n: usize) -> io::Result<Vec<String>> {
    let text = fs::read_to_string(path)?;
    let lines: Vec<String> = text.lines().map(String::from).collect();
    let start = lines.len().saturating_sub(n);
    Ok(lines[start..].to_vec())
}

fn write_atomically(path: &Path, contents: &str) -> io::Result<()> {
    fs::write(path, contents)
}
