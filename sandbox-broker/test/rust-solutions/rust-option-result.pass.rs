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

fn port(config: &str) -> Result<u16, String> {
    let raw = find_setting(config, "port").ok_or_else(|| String::from("missing port"))?;
    raw.parse::<u16>().map_err(|_| format!("invalid port: {raw}"))
}

fn parse_all(inputs: &[&str]) -> Result<Vec<i32>, String> {
    inputs
        .iter()
        .map(|input| input.trim().parse::<i32>().map_err(|_| format!("bad number: {}", input.trim())))
        .collect()
}
