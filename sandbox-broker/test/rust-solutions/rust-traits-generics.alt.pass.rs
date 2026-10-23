trait Billable { fn monthly_cents(&self) -> u32; }
struct Seat { learners: u32 }
struct Workspace { flat_cents: u32 }
impl Billable for Seat { fn monthly_cents(&self) -> u32 { 900 * self.learners } }
impl Billable for Workspace { fn monthly_cents(&self) -> u32 { self.flat_cents } }
fn total_cents<T>(items: &[T]) -> u32 where T: Billable { let mut t = 0; for i in items { t += i.monthly_cents(); } t }
fn total_mixed(items: &[Box<dyn Billable>]) -> u32 { items.iter().map(|b| b.monthly_cents()).sum() }
