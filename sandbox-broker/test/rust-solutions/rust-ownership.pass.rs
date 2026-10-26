struct Lease {
    name: String,
}

impl Drop for Lease {
    fn drop(&mut self) {
        record_drop(&self.name);
    }
}

fn hand_over(lease: Lease) -> usize {
    lease.name.len()
}

fn keep_longest(a: Lease, b: Lease) -> Lease {
    if b.name.len() > a.name.len() { b } else { a }
}

fn shout(mut name: String) -> String {
    name.make_ascii_uppercase();
    name
}
