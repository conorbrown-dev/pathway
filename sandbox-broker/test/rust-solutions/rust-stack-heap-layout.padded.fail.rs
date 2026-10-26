// Declaration order leaves gaps: 1 + 7 padding + 8 + 1 + 7 padding + 8 + 4 + 4 padding = 40 bytes.
#[repr(C)]
struct SensorReading {
    active: bool,
    id: u64,
    flags: u8,
    timestamp: u64,
    celsius: f32,
}

fn growth_steps(n: usize) -> Vec<usize> {
    let mut values: Vec<u64> = Vec::new();
    let mut capacities = Vec::new();
    let mut last = values.capacity();
    for i in 0..n {
        values.push(i as u64);
        if values.capacity() != last {
            last = values.capacity();
            capacities.push(last);
        }
    }
    capacities
}

fn vec_footprint(values: &Vec<u64>) -> (usize, usize) {
    (std::mem::size_of::<Vec<u64>>(), values.capacity() * std::mem::size_of::<u64>())
}
