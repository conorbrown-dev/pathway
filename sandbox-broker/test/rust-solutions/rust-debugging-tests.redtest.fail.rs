/// The code is right, but one of the learner's own tests asserts the wrong thing.
fn is_leap_year(year: u32) -> bool {
    (year % 4 == 0 && year % 100 != 0) || year % 400 == 0
}

fn days_in_month(year: u32, month: u32) -> Option<u32> {
    match month {
        1 | 3 | 5 | 7 | 8 | 10 | 12 => Some(31),
        4 | 6 | 9 | 11 => Some(30),
        2 if is_leap_year(year) => Some(29),
        2 => Some(28),
        _ => None,
    }
}

#[cfg(test)]
mod tests {
    use super::*;

    #[test]
    fn a_typical_leap_year() {
        assert!(is_leap_year(2024));
    }

    #[test]
    fn wrong_expectation() {
        assert!(is_leap_year(1900));
    }

    #[test]
    fn april_has_thirty_days() {
        assert_eq!(days_in_month(2023, 4), Some(30));
    }
}
