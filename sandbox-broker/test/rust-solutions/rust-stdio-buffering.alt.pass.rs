use std::io::{self, BufRead, BufWriter, Write};

fn count<R: BufRead>(mut reader: R) -> io::Result<(usize, usize, usize)> {
    let mut totals = (0, 0, 0);
    let mut buffer = Vec::new();
    loop {
        buffer.clear();
        let read = reader.read_until(b'\n', &mut buffer)?;
        if read == 0 {
            return Ok(totals);
        }
        let text = std::str::from_utf8(&buffer).map_err(|e| io::Error::new(io::ErrorKind::InvalidData, e))?;
        totals.0 += 1;
        totals.1 += text.split_whitespace().count();
        totals.2 += read;
    }
}

fn number_lines<R: BufRead, W: Write>(reader: R, out: W) -> io::Result<()> {
    let mut out = BufWriter::new(out);
    let mut number = 0;
    for line in reader.lines() {
        number += 1;
        writeln!(out, "{number:>4}  {}", line?)?;
    }
    out.flush()?;
    Ok(())
}
