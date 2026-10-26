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

// to_uppercase builds a brand-new String, so the caller's allocation is thrown away.
fn shout(name: String) -> String {
    name.to_uppercase()
}
