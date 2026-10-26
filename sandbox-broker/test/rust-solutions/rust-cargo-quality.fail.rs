// Decides at runtime from an environment variable instead of at compile time,
// so the same binary reports "info" whatever features it was built with.
fn log_level() -> &'static str {
    if std::env::var("VERBOSE").is_ok() { "debug" } else { "info" }
}

fn build_profile() -> &'static str {
    "debug"
}

fn enabled_features() -> Vec<&'static str> {
    Vec::new()
}
