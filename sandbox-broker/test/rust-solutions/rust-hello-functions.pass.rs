fn greet(name: &str) -> String {
    format!("Hello, {name}!")
}

fn rectangle_area(width: u32, height: u32) -> u32 {
    width * height
}

fn format_price(item: &str, cents: u32) -> String {
    let dollars = cents / 100;
    let remainder = cents % 100;
    format!("{item}: ${dollars}.{remainder:02}")
}
