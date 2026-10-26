fn log_level() -> &'static str {
    if cfg!(feature = "verbose") { "debug" } else { "info" }
}

fn build_profile() -> &'static str {
    if cfg!(debug_assertions) { "debug" } else { "release" }
}

fn enabled_features() -> Vec<&'static str> {
    let mut features = Vec::new();
    if cfg!(feature = "verbose") {
        features.push("verbose");
    }
    if cfg!(feature = "metrics") {
        features.push("metrics");
    }
    features
}
