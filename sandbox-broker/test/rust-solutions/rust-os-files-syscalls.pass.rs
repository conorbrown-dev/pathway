use std::fs::{self, File, OpenOptions};
use std::io::{self, BufRead, BufReader, Write};
use std::path::Path;

fn append_line(path: &Path, line: &str) -> io::Result<()> {
    let mut file = OpenOptions::new().create(true).append(true).open(path)?;
    writeln!(file, "{line}")
}

fn tail(path: &Path, n: usize) -> io::Result<Vec<String>> {
    let reader = BufReader::new(File::open(path)?);
    let mut last = std::collections::VecDeque::with_capacity(n);
    for line in reader.lines() {
        let line = line?;
        if n == 0 {
            continue;
        }
        if last.len() == n {
            last.pop_front();
        }
        last.push_back(line);
    }
    Ok(last.into_iter().collect())
}

fn write_atomically(path: &Path, contents: &str) -> io::Result<()> {
    let temporary = path.with_extension("tmp");
    let mut file = File::create(&temporary)?;
    file.write_all(contents.as_bytes())?;
    file.sync_all()?;
    drop(file);
    fs::rename(&temporary, path)
}
