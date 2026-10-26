#[derive(Debug, PartialEq)]
struct Config {
    pattern: String,
    paths: Vec<String>,
    ignore_case: bool,
    max_count: Option<usize>,
}

// Assumes options always come first and ignores `--`, so a pattern that starts with '-' is rejected.
fn parse_args(args: &[String]) -> Result<Config, String> {
    let mut ignore_case = false;
    let mut max_count = None;
    let mut index = 0;
    while index < args.len() && args[index].starts_with('-') {
        match args[index].as_str() {
            "-i" | "--ignore-case" => ignore_case = true,
            "-m" | "--max-count" => {
                index += 1;
                let value = args.get(index).ok_or_else(|| format!("missing value for {}", args[index - 1]))?;
                max_count = Some(value.parse().map_err(|_| format!("invalid number for {}: {value}", args[index - 1]))?);
            }
            other => return Err(format!("unknown option: {other}")),
        }
        index += 1;
    }
    let pattern = args.get(index).cloned().ok_or_else(|| String::from("missing pattern"))?;
    Ok(Config { pattern, paths: args[index + 1..].to_vec(), ignore_case, max_count })
}

fn color_enabled(no_color: Option<&str>, term: Option<&str>) -> bool {
    if no_color.is_some_and(|value| !value.is_empty()) {
        return false;
    }
    term != Some("dumb")
}

fn exit_code(result: &Result<usize, String>) -> i32 {
    match result {
        Ok(0) => 1,
        Ok(_) => 0,
        Err(_) => 2,
    }
}
