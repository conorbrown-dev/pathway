fn read_u32_le(bytes: &[u8]) -> Option<u32> {
    if bytes.len() < 4 {
        return None;
    }
    Some(bytes[0] as u32 | (bytes[1] as u32) << 8 | (bytes[2] as u32) << 16 | (bytes[3] as u32) << 24)
}

fn set_bit(flags: u8, bit: u32, on: bool) -> u8 {
    match 1u8.checked_shl(bit) {
        Some(mask) if on => flags | mask,
        Some(mask) => flags & !mask,
        None => flags,
    }
}

fn pack_rgb565(red: u8, green: u8, blue: u8) -> u16 {
    u16::from(red & 0xf8) << 8 | u16::from(green & 0xfc) << 3 | u16::from(blue) >> 3
}

fn unpack_rgb565(color: u16) -> (u8, u8, u8) {
    (((color >> 8) & 0xf8) as u8, ((color >> 3) & 0xfc) as u8, ((color << 3) & 0xf8) as u8)
}
