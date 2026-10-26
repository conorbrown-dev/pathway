#[cfg(feature = "verbose")]
fn log_level() -> &'static str {
    "debug"
}

#[cfg(not(feature = "verbose"))]
fn log_level() -> &'static str {
    "info"
}

#[cfg(debug_assertions)]
fn build_profile() -> &'static str {
    "debug"
}

#[cfg(not(debug_assertions))]
fn build_profile() -> &'static str {
    "release"
}

fn enabled_features() -> Vec<&'static str> {
    let mut features = vec![];
    #[cfg(feature = "verbose")]
    features.push("verbose");
    #[cfg(feature = "metrics")]
    features.push("metrics");
    features
}
