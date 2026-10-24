use std::{cell::RefCell, rc::Rc, sync::{Arc, Mutex}};
enum Path { End, Step(String, Box<Path>) }
fn path_len(path: &Path) -> usize {
    let mut n = 0; let mut cur = path;
    while let Path::Step(_, next) = cur { n += 1; cur = next; }
    n
}
fn new_roster() -> Rc<RefCell<Vec<String>>> { Rc::default() }
fn enroll(roster: &Rc<RefCell<Vec<String>>>, name: &str) -> bool {
    if roster.borrow().contains(&name.to_string()) { return false; }
    roster.borrow_mut().push(name.into());
    true
}
fn roster_size(roster: &Rc<RefCell<Vec<String>>>) -> usize { roster.borrow().len() }
fn count_in_threads(threads: usize, per_thread: usize) -> usize {
    let counter = Arc::new(Mutex::new(0usize));
    std::thread::scope(|s| {
        for _ in 0..threads {
            let c = Arc::clone(&counter);
            s.spawn(move || { let mut g = c.lock().unwrap(); *g += per_thread; });
        }
    });
    let n = *counter.lock().unwrap();
    n
}
