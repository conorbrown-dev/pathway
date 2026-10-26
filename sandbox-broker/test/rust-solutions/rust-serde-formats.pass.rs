use serde::{Deserialize, Serialize};

#[derive(Debug, PartialEq, Serialize, Deserialize)]
#[serde(rename_all = "camelCase", deny_unknown_fields)]
struct ServiceConfig {
    name: String,
    #[serde(default = "default_port")]
    port: u16,
    #[serde(default)]
    tags: Vec<String>,
    #[serde(default = "default_log_level")]
    log_level: String,
}

fn default_port() -> u16 {
    8080
}

fn default_log_level() -> String {
    String::from("info")
}

fn parse_config(json: &str) -> Result<ServiceConfig, String> {
    serde_json::from_str(json).map_err(|error| error.to_string())
}

fn to_pretty(config: &ServiceConfig) -> String {
    serde_json::to_string_pretty(config).expect("a ServiceConfig always serializes")
}
