use std::sync::Mutex;
use std::sync::atomic::{AtomicBool, AtomicU64, Ordering};

struct Counter {
    value: AtomicU64,
}

impl Counter {
    fn new() -> Counter {
        Counter { value: AtomicU64::new(0) }
    }

    // Load then store is two steps: another thread's increment in between is lost.
    fn increment(&self) {
        let current = self.value.load(Ordering::Relaxed);
        std::thread::yield_now();
        self.value.store(current + 1, Ordering::Relaxed);
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

    // Check-then-set: two threads can both see false before either sets true.
    fn try_claim(&self) -> bool {
        if self.claimed.load(Ordering::Acquire) {
            return false;
        }
        std::thread::yield_now();
        self.claimed.store(true, Ordering::Release);
        true
    }
}

fn transfer(from: &Mutex<i64>, to: &Mutex<i64>, amount: i64) {
    let mut a = from.lock().unwrap();
    let mut b = to.lock().unwrap();
    *a -= amount;
    *b += amount;
}
