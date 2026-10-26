fn read_u32_le(bytes: &[u8]) -> Option<u32> {
    let first: [u8; 4] = bytes.get(..4)?.try_into().ok()?;
    Some(u32::from_le_bytes(first))
}

// Clearing with XOR toggles instead: a bit that was already off gets turned on.
fn set_bit(flags: u8, bit: u32, on: bool) -> u8 {
    if bit >= 8 {
        return flags;
    }
    if on { flags | (1 << bit) } else { flags ^ (1 << bit) }
}

fn pack_rgb565(red: u8, green: u8, blue: u8) -> u16 {
    ((red as u16 >> 3) << 11) | ((green as u16 >> 2) << 5) | (blue as u16 >> 3)
}

fn unpack_rgb565(color: u16) -> (u8, u8, u8) {
    (((color >> 11) as u8) << 3, (((color >> 5) & 0x3f) as u8) << 2, ((color & 0x1f) as u8) << 3)
}
