fn add_points(current: u8, earned: u8) -> u8 {
    let sum = current as u16 + earned as u16;
    if sum > 255 { 255 } else { sum as u8 }
}

fn checked_total(prices: &[u32]) -> Option<u32> {
    prices.iter().try_fold(0u32, |total, &price| total.checked_add(price))
}

fn to_byte(value: i32) -> Option<u8> {
    if (0..=255).contains(&value) { Some(value as u8) } else { None }
}

fn next_tick(tick: u16) -> u16 {
    if tick == u16::MAX { 0 } else { tick + 1 }
}
