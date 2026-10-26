macro_rules! square {
    ($value:expr) => {{
        let value = $value;
        value * value
    }};
}

// No `$(,)?`: a trailing comma, or an empty map, doesn't match any rule.
macro_rules! hashmap {
    ($($key:expr => $value:expr),+) => {{
        let mut map = ::std::collections::HashMap::new();
        $(map.insert($key, $value);)+
        map
    }};
}

macro_rules! max_of {
    ($only:expr) => { $only };
    ($first:expr, $($rest:expr),+) => {{
        let first = $first;
        let rest = max_of!($($rest),+);
        if first > rest { first } else { rest }
    }};
}
