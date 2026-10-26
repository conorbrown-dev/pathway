mod inventory {
    pub struct Item {
        pub name: String,
        quantity: u32,
    }

    impl Item {
        pub fn new(name: &str, quantity: u32) -> Item {
            Item { name: name.to_string(), quantity }
        }

        pub fn quantity(&self) -> u32 {
            self.quantity
        }

        pub fn remove(&mut self, amount: u32) -> Result<(), String> {
            if amount > self.quantity {
                return Err(format!("only {} left", self.quantity));
            }
            self.quantity -= amount;
            Ok(())
        }
    }

    pub mod report {
        use super::Item;

        pub fn describe(item: &Item) -> String {
            format!("{} x{}", item.name, item.quantity())
        }
    }
}

pub use inventory::report::describe;
