/// Returns true for leap years in the Gregorian calendar.
fn is_leap_year(year: u32) -> bool {
    (year % 4 == 0 && year % 100 != 0) || year % 400 == 0
}

/// Number of days in `month` (1–12) of `year`, or None for a month that doesn't exist.
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
    fn centuries_are_not_leap_years() {
        assert!(!is_leap_year(1900), "1900 is divisible by 100 but not 400");
    }

    #[test]
    fn every_fourth_century_is_a_leap_year() {
        assert!(is_leap_year(2000));
    }

    #[test]
    fn february_follows_the_leap_rule() {
        assert_eq!(days_in_month(2024, 2), Some(29));
        assert_eq!(days_in_month(2023, 2), Some(28));
    }

    #[test]
    fn month_thirteen_does_not_exist() {
        assert_eq!(days_in_month(2024, 13), None);
    }
}
