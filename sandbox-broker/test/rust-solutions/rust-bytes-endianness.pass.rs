fn read_u32_le(bytes: &[u8]) -> Option<u32> {
    let first: [u8; 4] = bytes.get(..4)?.try_into().ok()?;
    Some(u32::from_le_bytes(first))
}

fn set_bit(flags: u8, bit: u32, on: bool) -> u8 {
    if bit >= 8 {
        return flags;
    }
    let mask = 1u8 << bit;
    if on { flags | mask } else { flags & !mask }
}

fn pack_rgb565(red: u8, green: u8, blue: u8) -> u16 {
    ((red as u16 >> 3) << 11) | ((green as u16 >> 2) << 5) | (blue as u16 >> 3)
}

fn unpack_rgb565(color: u16) -> (u8, u8, u8) {
    let red = ((color >> 11) & 0x1f) as u8;
    let green = ((color >> 5) & 0x3f) as u8;
    let blue = (color & 0x1f) as u8;
    (red << 3, green << 2, blue << 3)
}
