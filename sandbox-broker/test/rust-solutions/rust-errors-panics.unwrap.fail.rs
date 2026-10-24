#[derive(Debug, PartialEq)]
enum DurationError { Empty, NotANumber(String), TooLong(u32) }
impl std::fmt::Display for DurationError {
    fn fmt(&self, f: &mut std::fmt::Formatter) -> std::fmt::Result { write!(f, "{:?}", self) }
}
impl std::error::Error for DurationError {}
fn parse_minutes(input: &str) -> Result<u32, DurationError> {
    let m: u32 = input.trim().parse().unwrap();
    if m > 90 { return Err(DurationError::TooLong(m)); }
    Ok(m)
}
fn total_minutes(inputs: &[&str]) -> Result<u32, DurationError> {
    let mut t = 0; for i in inputs { t += parse_minutes(i)?; } Ok(t)
}
