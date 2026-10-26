// Plain + panics in a debug build when the sum passes 255.
fn add_points(current: u8, earned: u8) -> u8 {
    current + earned
}

fn checked_total(prices: &[u32]) -> Option<u32> {
    Some(prices.iter().sum())
}

fn to_byte(value: i32) -> Option<u8> {
    u8::try_from(value).ok()
}

fn next_tick(tick: u16) -> u16 {
    tick.wrapping_add(1)
}
