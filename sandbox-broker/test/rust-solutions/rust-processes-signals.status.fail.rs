use std::io::{self, Write};
use std::process::{Command, Stdio};

// Reports only success or failure, losing the actual exit code.
fn run_capture(program: &str, args: &[&str]) -> io::Result<(i32, String)> {
    let output = Command::new(program).args(args).output()?;
    let code = if output.status.success() { 0 } else { 1 };
    Ok((code, String::from_utf8_lossy(&output.stdout).into_owned()))
}

fn sort_lines(input: &str) -> io::Result<String> {
    let mut child = Command::new("sort")
        .env("LC_ALL", "C")
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .spawn()?;
    child.stdin.take().unwrap().write_all(input.as_bytes())?;
    let output = child.wait_with_output()?;
    Ok(String::from_utf8_lossy(&output.stdout).into_owned())
}

fn file_size(path: &str) -> io::Result<String> {
    let output = Command::new("wc").arg("-c").arg(path).output()?;
    if !output.status.success() {
        return Err(io::Error::other("wc failed"));
    }
    Ok(String::from_utf8_lossy(&output.stdout).split_whitespace().next().unwrap_or("0").to_string())
}
