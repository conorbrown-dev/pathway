use std::io::{self, BufRead, Write};

fn count<R: BufRead>(mut reader: R) -> io::Result<(usize, usize, usize)> {
    let (mut lines, mut words, mut bytes) = (0, 0, 0);
    let mut line = String::new();
    loop {
        line.clear();
        let read = reader.read_line(&mut line)?;
        if read == 0 {
            break;
        }
        lines += 1;
        bytes += read;
        words += line.split_whitespace().count();
    }
    Ok((lines, words, bytes))
}

// Throws write errors away, so a closed pipe looks like success.
fn number_lines<R: BufRead, W: Write>(reader: R, mut out: W) -> io::Result<()> {
    for (index, line) in reader.lines().enumerate() {
        let line = line?;
        let _ = writeln!(out, "{:>4}  {}", index + 1, line);
    }
    let _ = out.flush();
    Ok(())
}
