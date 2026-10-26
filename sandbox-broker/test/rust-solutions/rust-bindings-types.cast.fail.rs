fn add_points(current: u8, earned: u8) -> u8 {
    current.saturating_add(earned)
}

fn checked_total(prices: &[u32]) -> Option<u32> {
    let mut total: u32 = 0;
    for &price in prices {
        total = total.checked_add(price)?;
    }
    Some(total)
}

// `as` silently truncates: 300 becomes 44 instead of None.
fn to_byte(value: i32) -> Option<u8> {
    Some(value as u8)
}

fn next_tick(tick: u16) -> u16 {
    tick.wrapping_add(1)
}
