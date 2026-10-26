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
        self.claimed.compare_exchange(false, true, Ordering::AcqRel, Ordering::Acquire).is_ok()
    }
}

fn transfer(from: &Mutex<i64>, to: &Mutex<i64>, amount: i64) {
    if std::ptr::eq(from, to) {
        return;
    }
    // Always lock the lower address first, so two opposite transfers can't each hold one lock
    // while waiting for the other.
    let from_first = (from as *const Mutex<i64>) < (to as *const Mutex<i64>);
    let (mut from_guard, mut to_guard) = if from_first {
        let a = from.lock().unwrap();
        let b = to.lock().unwrap();
        (a, b)
    } else {
        let b = to.lock().unwrap();
        let a = from.lock().unwrap();
        (a, b)
    };
    *from_guard -= amount;
    *to_guard += amount;
}
