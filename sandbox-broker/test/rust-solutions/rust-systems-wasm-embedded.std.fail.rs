// Uses Vec, which needs an allocator: this doesn't compile under #![no_std].
pub struct RingBuffer<const N: usize> {
    items: Vec<u16>,
}

impl<const N: usize> RingBuffer<N> {
    pub fn new() -> Self {
        RingBuffer { items: Vec::with_capacity(N) }
    }

    pub fn push(&mut self, value: u16) -> Option<u16> {
        self.items.push(value);
        if self.items.len() > N { Some(self.items.remove(0)) } else { None }
    }

    pub fn len(&self) -> usize {
        self.items.len()
    }

    pub fn average(&self) -> Option<u16> {
        if self.items.is_empty() {
            return None;
        }
        Some((self.items.iter().map(|&v| u32::from(v)).sum::<u32>() / self.items.len() as u32) as u16)
    }
}

pub fn fletcher16(data: &[u8]) -> u16 {
    let (mut low, mut high) = (0u16, 0u16);
    for &byte in data {
        low = (low + u16::from(byte)) % 255;
        high = (high + low) % 255;
    }
    (high << 8) | low
}
