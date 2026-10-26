fn find_setting(config: &str, key: &str) -> Option<String> {
    config
        .lines()
        .map(str::trim)
        .filter(|line| !line.starts_with('#'))
        .filter_map(|line| line.split_once('='))
        .find(|(name, _)| name.trim() == key)
        .map(|(_, value)| value.trim().to_string())
}

fn port(config: &str) -> Result<u16, String> {
    match find_setting(config, "port") {
        None => Err("missing port".to_string()),
        Some(raw) => match raw.parse() {
            Ok(number) => Ok(number),
            Err(_) => Err(format!("invalid port: {raw}")),
        },
    }
}

fn parse_all(inputs: &[&str]) -> Result<Vec<i32>, String> {
    let mut numbers = Vec::new();
    for input in inputs {
        let trimmed = input.trim();
        match trimmed.parse::<i32>() {
            Ok(n) => numbers.push(n),
            Err(_) => return Err(format!("bad number: {trimmed}")),
        }
    }
    Ok(numbers)
}
