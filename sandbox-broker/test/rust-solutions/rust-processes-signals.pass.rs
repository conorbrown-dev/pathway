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
    {
        let mut stdin = child.stdin.take().expect("stdin was piped");
        stdin.write_all(input.as_bytes())?;
    } // dropping stdin closes the pipe, so sort sees end of input
    let output = child.wait_with_output()?;
    if !output.status.success() {
        return Err(io::Error::other(format!("sort failed: {}", output.status)));
    }
    Ok(String::from_utf8_lossy(&output.stdout).into_owned())
}

fn file_size(path: &str) -> io::Result<String> {
    let output = Command::new("wc").arg("-c").arg(path).output()?;
    if !output.status.success() {
        return Err(io::Error::other(String::from_utf8_lossy(&output.stderr).trim().to_string()));
    }
    let text = String::from_utf8_lossy(&output.stdout);
    Ok(text.split_whitespace().next().unwrap_or("0").to_string())
}
