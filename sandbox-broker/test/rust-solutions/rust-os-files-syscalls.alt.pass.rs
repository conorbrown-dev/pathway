use std::fs;
use std::io::{self, Write};
use std::path::Path;

fn append_line(path: &Path, line: &str) -> io::Result<()> {
    let mut file = fs::OpenOptions::new().append(true).create(true).open(path)?;
    file.write_all(line.as_bytes())?;
    file.write_all(b"\n")
}

fn tail(path: &Path, n: usize) -> io::Result<Vec<String>> {
    let text = fs::read_to_string(path)?;
    let lines: Vec<String> = text.lines().map(String::from).collect();
    let start = lines.len().saturating_sub(n);
    Ok(lines[start..].to_vec())
}

fn write_atomically(path: &Path, contents: &str) -> io::Result<()> {
    let temporary = path.with_extension("tmp");
    {
        let mut file = fs::File::create(&temporary)?;
        file.write_all(contents.as_bytes())?;
        file.sync_all()?;
    }
    fs::rename(temporary, path)
}
