struct Lease {
    name: String,
}

impl Drop for Lease {
    fn drop(&mut self) {
        let name = self.name.clone();
        record_drop(&name);
    }
}

fn hand_over(lease: Lease) -> usize {
    let length = lease.name.len();
    drop(lease);
    length
}

fn keep_longest(a: Lease, b: Lease) -> Lease {
    if b.name.len() > a.name.len() {
        drop(a);
        b
    } else {
        a
    }
}

fn shout(name: String) -> String {
    let mut owned = name; // moving into a new binding does not copy the text
    owned.make_ascii_uppercase();
    owned
}
