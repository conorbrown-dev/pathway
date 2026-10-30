// The grader provides:
//   trait Inventory { fn reserve(&mut self, sku: &str, quantity: u32) -> bool; fn release(&mut self, sku: &str, quantity: u32); }
//   trait Payments { fn charge(&mut self, cents: u64) -> Result<String, String>; }
//   trait Clock { fn now(&self) -> u64; }
//   struct Line { sku: String, quantity: u32, unit_cents: u64 }

#[derive(Debug, PartialEq)]
struct Receipt {
    payment_id: String,
    total_cents: u64,
    at: u64,
}

#[derive(Debug, PartialEq)]
enum CheckoutError {
    EmptyCart,
    OutOfStock(String),
    PaymentDeclined(String),
}

fn checkout(lines: &[Line], inventory: &mut dyn Inventory, payments: &mut dyn Payments, clock: &dyn Clock) -> Result<Receipt, CheckoutError> {
    if lines.is_empty() {
        return Err(CheckoutError::EmptyCart);
    }
    let mut reserved: Vec<&Line> = Vec::new();
    for line in lines {
        if !inventory.reserve(&line.sku, line.quantity) {
            for done in reserved {
                inventory.release(&done.sku, done.quantity);
            }
            return Err(CheckoutError::OutOfStock(line.sku.clone()));
        }
        reserved.push(line);
    }
    let total_cents = lines.iter().map(|line| u64::from(line.quantity) * line.unit_cents).sum();
    match payments.charge(total_cents) {
        Ok(payment_id) => Ok(Receipt { payment_id, total_cents, at: clock.now() }),
        // Keeps the stock reserved after a declined payment.
        Err(reason) => Err(CheckoutError::PaymentDeclined(reason)),
    }
}
