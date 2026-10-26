use std::fs;
use std::io::{self, Write};
use std::path::Path;

fn append_line(path: &Path, line: &str) -> io::Result<()> {
    let mut file = fs::OpenOptions::new().append(true).create(true).open(path)?;
    writeln!(file, "{line}")
}

// Hides a missing file as an empty result instead of reporting NotFound.
fn tail(path: &Path, n: usize) -> io::Result<Vec<String>> {
    let text = fs::read_to_string(path).unwrap_or_default();
    let lines: Vec<String> = text.lines().map(String::from).collect();
    let start = lines.len().saturating_sub(n);
    Ok(lines[start..].to_vec())
}

// Copies instead of renaming, leaving the temporary file behind.
fn write_atomically(path: &Path, contents: &str) -> io::Result<()> {
    let temporary = path.with_extension("tmp");
    fs::write(&temporary, contents)?;
    fs::copy(&temporary, path)?;
    Ok(())
}
