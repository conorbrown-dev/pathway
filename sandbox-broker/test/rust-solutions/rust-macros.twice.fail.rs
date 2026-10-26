// Pastes the expression twice, so square!(next()) calls next() two times.
macro_rules! square {
    ($value:expr) => {
        $value * $value
    };
}

macro_rules! hashmap {
    () => {
        ::std::collections::HashMap::new()
    };
    ($($key:expr => $value:expr),+ $(,)?) => {{
        let mut map = ::std::collections::HashMap::new();
        $(map.insert($key, $value);)+
        map
    }};
}

macro_rules! max_of {
    ($only:expr) => { $only };
    ($first:expr, $($rest:expr),+ $(,)?) => {{
        let first = $first;
        let rest = max_of!($($rest),+);
        if first > rest { first } else { rest }
    }};
}
