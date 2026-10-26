use serde::{Deserialize, Serialize};

fn default_port() -> u16 {
    8080
}

fn default_log_level() -> String {
    "info".to_string()
}

#[derive(Debug, PartialEq, Serialize, Deserialize)]
#[serde(deny_unknown_fields)]
struct ServiceConfig {
    name: String,
    #[serde(default = "default_port")]
    port: u16,
    #[serde(default)]
    tags: Vec<String>,
    #[serde(rename = "logLevel", default = "default_log_level")]
    log_level: String,
}

fn parse_config(json: &str) -> Result<ServiceConfig, String> {
    match serde_json::from_str::<ServiceConfig>(json) {
        Ok(config) => Ok(config),
        Err(error) => Err(format!("{error}")),
    }
}

fn to_pretty(config: &ServiceConfig) -> String {
    serde_json::to_string_pretty(config).unwrap()
}
