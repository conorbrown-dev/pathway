use std::collections::BTreeMap;
use std::sync::{Arc, Mutex};
use std::time::Instant;

use axum::extract::{MatchedPath, Request, State};
use axum::middleware::Next;
use axum::response::Response;

const BUCKETS_MS: [u64; 5] = [5, 25, 100, 500, 2500];

#[derive(Default)]
struct Metrics {
    counts: Mutex<BTreeMap<(String, u16), u64>>,
    buckets: Mutex<[u64; 5]>,
    total: Mutex<(u64, u64)>, // (count, sum of milliseconds)
}

impl Metrics {
    fn record(&self, route: &str, status: u16, duration_ms: u64) {
        *self.counts.lock().unwrap().entry((route.to_string(), status)).or_insert(0) += 1;
        let mut buckets = self.buckets.lock().unwrap();
        for (index, bound) in BUCKETS_MS.iter().enumerate() {
            // Counts each request in one bucket only: Prometheus buckets are cumulative.
            if duration_ms <= *bound {
                buckets[index] += 1;
                break;
            }
        }
        let mut total = self.total.lock().unwrap();
        total.0 += 1;
        total.1 += duration_ms;
    }

    fn render(&self) -> String {
        let mut out = String::from("# TYPE http_requests_total counter\n");
        for ((route, status), count) in self.counts.lock().unwrap().iter() {
            out.push_str(&format!("http_requests_total{{route=\"{route}\",status=\"{status}\"}} {count}\n"));
        }
        out.push_str("# TYPE http_request_duration_ms histogram\n");
        let buckets = self.buckets.lock().unwrap();
        let (count, sum) = *self.total.lock().unwrap();
        for (index, bound) in BUCKETS_MS.iter().enumerate() {
            out.push_str(&format!("http_request_duration_ms_bucket{{le=\"{bound}\"}} {}\n", buckets[index]));
        }
        out.push_str(&format!("http_request_duration_ms_bucket{{le=\"+Inf\"}} {count}\n"));
        out.push_str(&format!("http_request_duration_ms_sum {sum}\n"));
        out.push_str(&format!("http_request_duration_ms_count {count}\n"));
        out
    }
}

async fn track(State(metrics): State<Arc<Metrics>>, request: Request, next: Next) -> Response {
    let route = request
        .extensions()
        .get::<MatchedPath>()
        .map(|path| path.as_str().to_string())
        .unwrap_or_else(|| String::from("unmatched"));
    let started = Instant::now();
    let response = next.run(request).await;
    metrics.record(&route, response.status().as_u16(), started.elapsed().as_millis() as u64);
    response
}
