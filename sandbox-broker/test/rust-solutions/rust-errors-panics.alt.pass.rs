#[derive(Debug, PartialEq)]
enum DurationError { Empty, NotANumber(String), TooLong(u32) }

impl std::fmt::Display for DurationError {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        let message = match self {
            Self::Empty => "duration is empty".to_string(),
            Self::NotANumber(t) => format!("'{}' is not a number of minutes", t),
            Self::TooLong(m) => format!("{} minutes is longer than the 90-minute limit", m),
        };
        f.write_str(&message)
    }
}
impl std::error::Error for DurationError {}

fn parse_minutes(input: &str) -> Result<u32, DurationError> {
    match input.trim() {
        "" => Err(DurationError::Empty),
        text => match text.parse::<u32>() {
            Ok(m) if m > 90 => Err(DurationError::TooLong(m)),
            Ok(m) => Ok(m),
            Err(_) => Err(DurationError::NotANumber(text.to_string())),
        },
    }
}

fn total_minutes(inputs: &[&str]) -> Result<u32, DurationError> {
    inputs.iter().map(|i| parse_minutes(i)).sum()
}
