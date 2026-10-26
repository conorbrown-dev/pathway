fn find_setting(config: &str, key: &str) -> Option<String> {
    for line in config.lines() {
        let line = line.trim();
        if line.starts_with('#') {
            continue;
        }
        let Some((name, value)) = line.split_once('=') else { continue };
        if name.trim() == key {
            return Some(value.trim().to_string());
        }
    }
    None
}

// unwrap() turns a missing or invalid port into a crash instead of an Err.
fn port(config: &str) -> Result<u16, String> {
    Ok(find_setting(config, "port").unwrap().parse().unwrap())
}

fn parse_all(inputs: &[&str]) -> Result<Vec<i32>, String> {
    inputs
        .iter()
        .map(|input| input.trim().parse::<i32>().map_err(|_| format!("bad number: {}", input.trim())))
        .collect()
}
