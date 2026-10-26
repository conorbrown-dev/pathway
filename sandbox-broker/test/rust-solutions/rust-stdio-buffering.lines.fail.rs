use std::io::{self, BufRead, Write};

// lines() strips the newline, so the byte count comes up short.
fn count<R: BufRead>(reader: R) -> io::Result<(usize, usize, usize)> {
    let (mut lines, mut words, mut bytes) = (0, 0, 0);
    for line in reader.lines() {
        let line = line?;
        lines += 1;
        bytes += line.len();
        words += line.split_whitespace().count();
    }
    Ok((lines, words, bytes))
}

fn number_lines<R: BufRead, W: Write>(reader: R, mut out: W) -> io::Result<()> {
    for (index, line) in reader.lines().enumerate() {
        writeln!(out, "{:>4}  {}", index + 1, line?)?;
    }
    out.flush()
}
