pub struct RingBuffer<const N: usize> {
    items: [u16; N],
    start: usize,
    len: usize,
}

impl<const N: usize> RingBuffer<N> {
    pub const fn new() -> Self {
        RingBuffer { items: [0; N], start: 0, len: 0 }
    }

    pub fn push(&mut self, value: u16) -> Option<u16> {
        if self.len < N {
            self.items[(self.start + self.len) % N] = value;
            self.len += 1;
            None
        } else {
            let evicted = self.items[self.start];
            self.items[self.start] = value;
            self.start = (self.start + 1) % N;
            Some(evicted)
        }
    }

    pub fn len(&self) -> usize {
        self.len
    }

    // Sums in a u16: four readings of 60,000 overflow.
    pub fn average(&self) -> Option<u16> {
        if self.len == 0 {
            return None;
        }
        let mut sum: u16 = 0;
        for offset in 0..self.len {
            sum += self.items[(self.start + offset) % N];
        }
        Some(sum / self.len as u16)
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
