#[derive(Debug, PartialEq)]
struct Config {
    pattern: String,
    paths: Vec<String>,
    ignore_case: bool,
    max_count: Option<usize>,
}

fn parse_args(args: &[String]) -> Result<Config, String> {
    let mut ignore_case = false;
    let mut max_count = None;
    let mut positional = Vec::new();
    let mut options_done = false;
    let mut iter = args.iter();
    while let Some(arg) = iter.next() {
        if options_done || !arg.starts_with('-') || arg == "-" {
            positional.push(arg.clone());
            continue;
        }
        match arg.as_str() {
            "--" => options_done = true,
            "-i" | "--ignore-case" => ignore_case = true,
            "-m" | "--max-count" => {
                let value = iter.next().ok_or_else(|| format!("missing value for {arg}"))?;
                let count = value.parse::<usize>().map_err(|_| format!("invalid number for {arg}: {value}"))?;
                max_count = Some(count);
            }
            other => return Err(format!("unknown option: {other}")),
        }
    }
    let mut positional = positional.into_iter();
    let pattern = positional.next().ok_or_else(|| String::from("missing pattern"))?;
    Ok(Config { pattern, paths: positional.collect(), ignore_case, max_count })
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
