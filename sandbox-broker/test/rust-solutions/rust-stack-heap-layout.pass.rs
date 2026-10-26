#[repr(C)]
struct SensorReading {
    id: u64,
    timestamp: u64,
    celsius: f32,
    active: bool,
    flags: u8,
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
