use std::fs::File;
use std::io::{self, BufRead, BufReader, Write};

const USAGE: &str = "usage: minigrep [-i] [-n] [-c] PATTERN [FILE...]";

struct Options {
    ignore_case: bool,
    line_numbers: bool,
    count_only: bool,
    pattern: String,
    files: Vec<String>,
}

fn parse(args: &[String]) -> Result<Options, String> {
    let mut options = Options { ignore_case: false, line_numbers: false, count_only: false, pattern: String::new(), files: Vec::new() };
    let mut positional = Vec::new();
    for arg in args {
        match arg.as_str() {
            "-i" => options.ignore_case = true,
            "-n" => options.line_numbers = true,
            "-c" => options.count_only = true,
            flag if flag.starts_with('-') && flag.len() > 1 => return Err(format!("unknown option: {flag}")),
            _ => positional.push(arg.clone()),
        }
    }
    let mut positional = positional.into_iter();
    options.pattern = positional.next().ok_or_else(|| String::from("missing pattern"))?;
    options.files = positional.collect();
    Ok(options)
}

fn search<R: BufRead>(reader: R, label: Option<&str>, options: &Options, stdout: &mut dyn Write) -> io::Result<usize> {
    let needle = if options.ignore_case { options.pattern.to_lowercase() } else { options.pattern.clone() };
    let mut matches = 0;
    for (index, line) in reader.lines().enumerate() {
        let line = line?;
        let haystack = if options.ignore_case { line.to_lowercase() } else { line.clone() };
        if !haystack.contains(&needle) {
            continue;
        }
        matches += 1;
        if options.count_only {
            continue;
        }
        if let Some(label) = label {
            write!(stdout, "{label}:")?;
        }
        if options.line_numbers {
            write!(stdout, "{}:", index + 1)?;
        }
        writeln!(stdout, "{line}")?;
    }
    if options.count_only {
        match label {
            Some(label) => writeln!(stdout, "{label}:{matches}")?,
            None => writeln!(stdout, "{matches}")?,
        }
    }
    Ok(matches)
}

fn run(args: &[String], stdin: &mut dyn BufRead, stdout: &mut dyn Write, stderr: &mut dyn Write) -> i32 {
    let options = match parse(args) {
        Ok(options) => options,
        Err(message) => {
            let _ = writeln!(stderr, "minigrep: {message}\n{USAGE}");
            return 2;
        }
    };
    let mut total = 0;
    let mut had_error = false;
    if options.files.is_empty() {
        match search(stdin, None, &options, stdout) {
            Ok(found) => total += found,
            Err(error) => {
                let _ = writeln!(stderr, "minigrep: (standard input): {error}");
                had_error = true;
            }
        }
    }
    let label_files = options.files.len() > 1;
    for path in &options.files {
        let result = File::open(path).and_then(|file| search(BufReader::new(file), label_files.then_some(path.as_str()), &options, stdout));
        match result {
            Ok(found) => total += found,
            Err(error) => {
                let _ = writeln!(stderr, "minigrep: {path}: {error}");
                had_error = true;
            }
        }
    }
    if had_error {
        2
    } else if total > 0 {
        0
    } else {
        1
    }
}
