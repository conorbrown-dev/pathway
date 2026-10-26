use std::collections::HashSet;

fn backoff_ms(attempt: u32, base_ms: u64, cap_ms: u64, jitter: f64) -> u64 {
    // 2^attempt overflows (or panics) for large attempts instead of saturating.
    let exponential = base_ms * 2u64.pow(attempt);
    let ceiling = exponential.min(cap_ms);
    (ceiling as f64 * jitter.clamp(0.0, 1.0)) as u64
}

#[derive(Debug, PartialEq, Clone, Copy)]
enum BreakerState {
    Closed,
    Open,
    HalfOpen,
}

struct CircuitBreaker {
    threshold: u32,
    cooldown_ms: u64,
    failures: u32,
    state: BreakerState,
    opened_at: u64,
}

impl CircuitBreaker {
    fn new(threshold: u32, cooldown_ms: u64) -> CircuitBreaker {
        CircuitBreaker { threshold, cooldown_ms, failures: 0, state: BreakerState::Closed, opened_at: 0 }
    }

    fn state(&self) -> BreakerState {
        self.state
    }

    fn allow(&mut self, now_ms: u64) -> bool {
        match self.state {
            BreakerState::Closed => true,
            BreakerState::HalfOpen => false, // one trial call is already in flight
            BreakerState::Open if now_ms.saturating_sub(self.opened_at) >= self.cooldown_ms => {
                self.state = BreakerState::HalfOpen;
                true
            }
            BreakerState::Open => false,
        }
    }

    fn record_success(&mut self) {
        self.failures = 0;
        self.state = BreakerState::Closed;
    }

    fn record_failure(&mut self, now_ms: u64) {
        self.failures += 1;
        if self.state == BreakerState::HalfOpen || self.failures >= self.threshold {
            self.state = BreakerState::Open;
            self.opened_at = now_ms;
        }
    }
}

#[derive(Debug, PartialEq)]
enum Outcome {
    Delivered { attempts: u32 },
    DeadLettered { attempts: u32, last_error: String },
    Duplicate,
}

struct Consumer {
    seen: HashSet<String>,
    max_attempts: u32,
    dead_letters: Vec<String>,
}

impl Consumer {
    fn new(max_attempts: u32) -> Consumer {
        Consumer { seen: HashSet::new(), max_attempts, dead_letters: Vec::new() }
    }

    fn deliver(&mut self, id: &str, handler: &mut dyn FnMut() -> Result<(), String>) -> Outcome {
        if self.seen.contains(id) {
            return Outcome::Duplicate;
        }
        let mut last_error = String::new();
        for attempt in 1..=self.max_attempts {
            match handler() {
                Ok(()) => {
                    self.seen.insert(id.to_string());
                    return Outcome::Delivered { attempts: attempt };
                }
                Err(error) => last_error = error,
            }
        }
        self.dead_letters.push(id.to_string());
        Outcome::DeadLettered { attempts: self.max_attempts, last_error }
    }
}
