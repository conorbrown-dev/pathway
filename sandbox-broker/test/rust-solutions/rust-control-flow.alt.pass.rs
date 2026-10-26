fn http_class(status: u16) -> &'static str {
    if (100..200).contains(&status) {
        "informational"
    } else if (200..300).contains(&status) {
        "success"
    } else if (300..400).contains(&status) {
        "redirect"
    } else if (400..500).contains(&status) {
        "client error"
    } else if (500..600).contains(&status) {
        "server error"
    } else {
        "invalid"
    }
}

fn fizzbuzz(n: u32) -> String {
    if n % 15 == 0 {
        "FizzBuzz".to_string()
    } else if n % 3 == 0 {
        "Fizz".to_string()
    } else if n % 5 == 0 {
        "Buzz".to_string()
    } else {
        format!("{n}")
    }
}

fn collatz_steps(start: u64) -> u32 {
    let mut n = start;
    let mut steps = 0;
    loop {
        if n == 1 {
            break steps;
        }
        n = if n % 2 == 0 { n / 2 } else { 3 * n + 1 };
        steps += 1;
    }
}
