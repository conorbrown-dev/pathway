use std::fmt;

#[derive(Debug, PartialEq)]
enum DurationError {
    Empty,
    NotANumber(String),
    TooLong(u32),
}

impl fmt::Display for DurationError {
    fn fmt(&self, f: &mut fmt::Formatter<'_>) -> fmt::Result {
        match self {
            DurationError::Empty => write!(f, "duration is empty"),
            DurationError::NotANumber(text) => write!(f, "'{text}' is not a number of minutes"),
            DurationError::TooLong(minutes) => write!(f, "{minutes} minutes is longer than the 90-minute limit"),
        }
    }
}

impl std::error::Error for DurationError {}

fn parse_minutes(input: &str) -> Result<u32, DurationError> {
    let trimmed = input.trim();
    if trimmed.is_empty() {
        return Err(DurationError::Empty);
    }
    let minutes: u32 = trimmed
        .parse()
        .map_err(|_| DurationError::NotANumber(trimmed.to_string()))?;
    if minutes > 90 {
        return Err(DurationError::TooLong(minutes));
    }
    Ok(minutes)
}

fn total_minutes(inputs: &[&str]) -> Result<u32, DurationError> {
    let mut total = 0;
    for input in inputs {
        total += parse_minutes(input)?;
    }
    Ok(total)
}
