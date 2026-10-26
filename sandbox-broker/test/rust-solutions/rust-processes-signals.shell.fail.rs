use std::io::{self, Write};
use std::process::{Command, Stdio};

fn run_capture(program: &str, args: &[&str]) -> io::Result<(i32, String)> {
    let output = Command::new(program).args(args).output()?;
    let code = output.status.code().unwrap_or(-1);
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

// Builds a shell command from text, so a file name can run commands of its own.
fn file_size(path: &str) -> io::Result<String> {
    let output = Command::new("sh").arg("-c").arg(format!("wc -c {path}")).output()?;
    let text = String::from_utf8_lossy(&output.stdout);
    Ok(text.split_whitespace().next().unwrap_or("0").to_string())
}
