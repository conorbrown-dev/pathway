#[derive(Debug, PartialEq)]
enum DurationError { Empty, NotANumber(String), TooLong(u32) }
impl std::fmt::Display for DurationError {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result {
        match self {
            Self::Empty => write!(f, "duration is empty"),
            Self::NotANumber(t) => write!(f, "'{t}' is not a number of minutes"),
            Self::TooLong(m) => write!(f, "{m} minutes is longer than the 90-minute limit"),
        }
    }
}
impl std::error::Error for DurationError {}
fn parse_minutes(input: &str) -> Result<u32, DurationError> {
    let t = input.trim();
    if t.is_empty() { return Err(DurationError::Empty); }
    let m: u32 = t.parse().map_err(|_| DurationError::NotANumber(t.to_string()))?;
    if m > 90 { return Err(DurationError::TooLong(m)); }
    Ok(m)
}
fn total_minutes(inputs: &[&str]) -> Result<u32, DurationError> {
    Ok(inputs.iter().filter_map(|i| parse_minutes(i).ok()).sum())
}
