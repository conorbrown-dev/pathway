use std::cell::RefCell;
use std::rc::Rc;
use std::sync::{Arc, Mutex};
use std::thread;

enum Path {
    End,
    Step(String, Box<Path>),
}

fn path_len(path: &Path) -> usize {
    match path {
        Path::End => 0,
        Path::Step(_, rest) => 1 + path_len(rest),
    }
}

fn new_roster() -> Rc<RefCell<Vec<String>>> {
    Rc::new(RefCell::new(Vec::new()))
}

fn enroll(roster: &Rc<RefCell<Vec<String>>>, name: &str) -> bool {
    let mut learners = roster.borrow().clone();
    if learners.iter().any(|learner| learner == name) {
        return false;
    }
    learners.push(name.to_string());
    true
}

fn roster_size(roster: &Rc<RefCell<Vec<String>>>) -> usize {
    roster.borrow().len()
}

fn count_in_threads(threads: usize, per_thread: usize) -> usize {
    let counter = Arc::new(Mutex::new(0));
    let handles: Vec<_> = (0..threads)
        .map(|_| {
            let counter = Arc::clone(&counter);
            thread::spawn(move || {
                for _ in 0..per_thread {
                    *counter.lock().unwrap() += 1;
                }
            })
        })
        .collect();
    for handle in handles {
        handle.join().unwrap();
    }
    let total = *counter.lock().unwrap();
    total
}
