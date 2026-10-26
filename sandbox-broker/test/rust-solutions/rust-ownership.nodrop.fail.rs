struct Lease {
    name: String,
}

// No Drop implementation: nothing is recorded when a Lease is cleaned up.

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
