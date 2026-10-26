use std::sync::Mutex;
use std::sync::atomic::{AtomicBool, AtomicU64, Ordering};

struct Counter {
    value: AtomicU64,
}

impl Counter {
    fn new() -> Counter {
        Counter { value: AtomicU64::new(0) }
    }

    fn increment(&self) {
        self.value.fetch_add(1, Ordering::Relaxed);
    }

    fn get(&self) -> u64 {
        self.value.load(Ordering::Relaxed)
    }
}

struct OnceFlag {
    claimed: AtomicBool,
}

impl OnceFlag {
    fn new() -> OnceFlag {
        OnceFlag { claimed: AtomicBool::new(false) }
    }

    fn try_claim(&self) -> bool {
        !self.claimed.swap(true, Ordering::AcqRel)
    }
}

// Locks in argument order: transfers in opposite directions can deadlock.
fn transfer(from: &Mutex<i64>, to: &Mutex<i64>, amount: i64) {
    let mut a = from.lock().unwrap();
    std::thread::yield_now();
    let mut b = to.lock().unwrap();
    *a -= amount;
    *b += amount;
}
