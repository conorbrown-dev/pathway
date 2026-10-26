fn greet(name: &str) -> String {
    format!("Hello, {name}!")
}

fn rectangle_area(width: u32, height: u32) -> u32 {
    width * height
}

// Forgets to pad the cents, so 305 prints as $3.5.
fn format_price(item: &str, cents: u32) -> String {
    format!("{item}: ${}.{}", cents / 100, cents % 100)
}
