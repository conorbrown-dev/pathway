import { readdirSync, readFileSync } from 'node:fs'

const MAX_CODE_BYTES = 50_000

// Kubernetes lessons are checked without a cluster: the sandbox parses manifests with PyYAML and
// checks object semantics and kubectl intent (see kube_check.py).
const KUBE_CHECKER = readFileSync(new URL('./kube_check.py', import.meta.url), 'utf8')
const KUBE_TESTS = {
  'k8s-yaml-api-objects': 3,
  'k8s-kubectl-contexts': 2,
  'k8s-labels-selectors': 3,
  'k8s-deployments-replicasets': 4,
  'k8s-services': 3,
  'k8s-resources-qos': 3,
  'k8s-debugging': 4,
}

function kubeFixture(lessonSlug, code) {
  return {
    files: { 'kube_check.py': KUBE_CHECKER, 'submission.txt': code },
    command: ['python3', '-I', '/workspace/kube_check.py', lessonSlug, '/workspace/submission.txt'],
    tests: KUBE_TESTS[lessonSlug],
    runtime: 'kubernetes',
  }
}

// AI & LLM course: each lesson's grader is a Python file in llm-graders/ named after the lesson
// slug. The shared harness is prepended so the script runs standalone under `python3 -I`. Learner
// code is standard-library Python 3.11 (the tools image). A grader declares its test count in a
// `# tests: N` comment.
const LLM_GRADER_DIR = new URL('./llm-graders/', import.meta.url)
const LLM_HARNESS = readFileSync(new URL('_harness.py', LLM_GRADER_DIR), 'utf8')
const LLM_GRADERS = Object.fromEntries(
  readdirSync(LLM_GRADER_DIR)
    .filter(name => name.endsWith('.py') && !name.startsWith('_'))
    .map(name => {
      const source = readFileSync(new URL(name, LLM_GRADER_DIR), 'utf8')
      const tests = Number(/^# tests: (\d+)$/m.exec(source)?.[1] ?? 1)
      return [name.slice(0, -3), { source, tests }]
    }),
)

/** Lesson slugs graded by a Python grader in llm-graders/. */
export const llmGradedSlugs = () => Object.keys(LLM_GRADERS).sort()

function llmFixture(lessonSlug, code) {
  const grader = LLM_GRADERS[lessonSlug]
  return {
    files: { 'grader.py': `${LLM_HARNESS}\n\n# ---- ${lessonSlug} ----\n${grader.source}`, 'submission.py': code },
    command: ['python3', '-I', '-B', '/workspace/grader.py'],
    tests: grader.tests,
    runtime: 'python',
  }
}

// SQL Server course: learner T-SQL is parsed as T-SQL and run on SQLite by the engine in
// sql-graders/_tsql.py (see docs/sqlserver-curriculum.md for the supported subset). Each lesson's
// grader is a Python file named after the slug; the engine is prepended so the script runs
// standalone under `python3 -I` in the tools image. A grader declares its test count in a
// `# tests: N` comment.
const SQL_GRADER_DIR = new URL('./sql-graders/', import.meta.url)
const SQL_ENGINE = readFileSync(new URL('_tsql.py', SQL_GRADER_DIR), 'utf8')
const SQL_GRADERS = Object.fromEntries(
  readdirSync(SQL_GRADER_DIR)
    .filter(name => name.endsWith('.py') && !name.startsWith('_'))
    .map(name => {
      const source = readFileSync(new URL(name, SQL_GRADER_DIR), 'utf8')
      const tests = Number(/^# tests: (\d+)$/m.exec(source)?.[1] ?? 1)
      return [name.slice(0, -3), { source, tests }]
    }),
)

/** Lesson slugs graded by a T-SQL grader in sql-graders/. */
export const sqlGradedSlugs = () => Object.keys(SQL_GRADERS).sort()

function sqlFixture(lessonSlug, code) {
  const grader = SQL_GRADERS[lessonSlug]
  return {
    files: { 'grader.py': `${SQL_ENGINE}\n\n# ---- ${lessonSlug} ----\n${grader.source}`, 'submission.sql': code },
    command: ['python3', '-I', '-B', '/workspace/grader.py'],
    tests: grader.tests,
    runtime: 'python',
  }
}

export function validateRequest(value) {
  if (!value || typeof value.lessonSlug !== 'string' || typeof value.code !== 'string') return 'lessonSlug and code are required.'
  if (value.lessonSlug.length > 200 || value.code.length === 0 || Buffer.byteLength(value.code, 'utf8') > MAX_CODE_BYTES) return 'Code must be between 1 and 50,000 bytes.'
  return null
}

function gitFixture(code, setup, checks, tests = 3) {
  const script = [
    '#!/bin/bash',
    'set -u',
    'export HOME=/workspace',
    'export GIT_PAGER=cat',
    'export PAGER=cat',
    'export GIT_TERMINAL_PROMPT=0',
    'git config --global init.defaultBranch main',
    'git config --global user.name "Pathway Learner"',
    'git config --global user.email "learner@pathway.invalid"',
    'mkdir -p /workspace/repo',
    'cd /workspace/repo',
    setup,
    'set +e',
    '(',
    code,
    ') > /workspace/learner.out 2>&1',
    'set -e',
    'cat /workspace/learner.out',
    checks,
    'echo PATHWAY_TEST_PASS',
    '',
  ].join('\n')
  return { files: { 'exercise.sh': script }, command: ['/bin/bash', '/workspace/exercise.sh'], tests, runtime: 'git' }
}

function linuxFixture(code, setup, checks, tests = 3) {
  const script = [
    '#!/bin/bash',
    'set -euo pipefail',
    'export HOME=/workspace/home/pathway',
    'mkdir -p "$HOME"',
    'cd /workspace',
    setup,
    "cat > /workspace/submission.sh <<'PATHWAY_BASH'",
    code,
    'PATHWAY_BASH',
    'chmod +x /workspace/submission.sh',
    'set +e',
    '/bin/bash /workspace/submission.sh > /workspace/learner.out 2> /workspace/learner.err',
    'status=$?',
    'set -e',
    'cat /workspace/learner.out',
    'cat /workspace/learner.err >&2 || true',
    'export PATHWAY_STATUS=$status',
    checks,
    'echo PATHWAY_TEST_PASS',
    '',
  ].join('\n')
  return { files: { 'exercise.sh': script }, command: ['/bin/bash', '/workspace/exercise.sh'], tests, runtime: 'linux' }
}

function vimFixture(code, setup, checks, tests = 3) {
  const script = [
    '#!/bin/bash',
    'set -euo pipefail',
    'cd /workspace',
    setup,
    "cat > /workspace/commands.vim <<'PATHWAY_VIM'",
    code.replace(/^:/gm, ''),
    'PATHWAY_VIM',
    "printf '\nqa!\n' >> /workspace/commands.vim",
    'vim -Nu NONE -n -es /workspace/notes.txt -S /workspace/commands.vim',
    checks,
    'echo PATHWAY_TEST_PASS',
    '',
  ].join('\n')
  return { files: { 'exercise.sh': script }, command: ['/bin/bash', '/workspace/exercise.sh'], tests, runtime: 'linux' }
}

function sqliteFixture(code, setup, checks, tests = 3) {
  const script = [
    '#!/bin/bash',
    'set -euo pipefail',
    'DB=/workspace/lab.db',
    "sqlite3 -batch -bail \"$DB\" <<'PATHWAY_SETUP'",
    setup,
    'PATHWAY_SETUP',
    "cat > /workspace/submission.sql <<'PATHWAY_SQL'",
    code,
    'PATHWAY_SQL',
    'sqlite3 -batch -bail "$DB" < /workspace/submission.sql > /workspace/learner.out',
    'cat /workspace/learner.out',
    checks,
    'echo PATHWAY_TEST_PASS',
    '',
  ].join('\n')
  return {
    files: { 'exercise.sh': script },
    command: ['/bin/bash', '/workspace/exercise.sh'],
    tests,
    runtime: 'sqlite',
  }
}

function reactFixture(code, checks, tests = 2) {
  const stubs = `
declare namespace React { type ReactNode = any }
declare namespace JSX { interface IntrinsicElements { [element: string]: any } }
declare module 'react' {
  export type ReactNode = any
  export type ButtonHTMLAttributes<T> = { className?: string; [key: string]: any }
  export type Context<T> = { __type?: T }
  export function useState<T>(initial: T): [T, (value: T | ((current: T) => T)) => void]
  export function useReducer<S, A>(reducer: (state: S, action: A) => S, initial: S): [S, (action: A) => void]
  export function useEffect(effect: () => void | (() => void), deps?: readonly unknown[]): void
  export function createContext<T>(value: T): Context<T>
  export function useContext<T>(context: Context<T>): T
  export function useActionState<S>(action: (state: S, data: FormData) => Promise<S>, initial: S): [S, (data: FormData) => void, boolean]
}
declare module 'react-dom' { export function useFormStatus(): { pending: boolean } }
declare module 'react-router/dom' {
  export type Router = unknown
  export function RouterProvider(props: { router: Router }): any
}
declare module 'react-router' {
  export type LoaderFunctionArgs = { params: Record<string, string | undefined>; request: Request }
  export type ActionFunctionArgs = { params: Record<string, string | undefined>; request: Request }
  export function createBrowserRouter(routes: unknown[]): unknown
  export function Form(props: { method?: string; action?: string; children?: any }): any
  export function Link(props: { to: string; children?: any }): any
  export function Outlet(): any
}
declare module '@testing-library/react' {
  export function render(value: any): unknown
  export const screen: { getByRole(role: string, options?: any): any }
}
declare module 'vitest' {
  export function describe(name: string, fn: () => void): void
  export function it(name: string, fn: () => void): void
  export function expect(value: any): any
}
`
  const tsconfig = JSON.stringify({
    compilerOptions: {
      target: 'ES2023',
      module: 'Preserve',
      moduleResolution: 'Bundler',
      jsx: 'preserve',
      strict: true,
      skipLibCheck: true,
      noEmit: true,
      lib: ['ES2023', 'DOM'],
    },
    files: ['/workspace/react-stubs.d.ts', '/workspace/submission.tsx'],
  })
  return {
    files: {
      'submission.tsx': code,
      'react-stubs.d.ts': stubs,
      'tsconfig.json': tsconfig,
    },
    command: ['/bin/sh', '-c', `tsc -p /workspace/tsconfig.json && ${checks} && echo PATHWAY_TEST_PASS`],
    tests,
    runtime: 'react',
  }
}

export function fixtureFor(lessonSlug, code) {
  if (Object.hasOwn(KUBE_TESTS, lessonSlug)) return kubeFixture(lessonSlug, code)
  if (Object.hasOwn(LLM_GRADERS, lessonSlug)) return llmFixture(lessonSlug, code)
  if (Object.hasOwn(SQL_GRADERS, lessonSlug)) return sqlFixture(lessonSlug, code)
  switch (lessonSlug) {
    case 'python-functions':
      return {
        files: { 'main.py': `${code}\n\nresult = greet('Ada')\nassert isinstance(result, str) and result.strip()\nprint('PATHWAY_TEST_PASS')\n` },
        command: ['python3', '/workspace/main.py'],
        tests: 2,
      }
    case 'rust-hello-functions':
      return {
        files: { 'main.rs': `${code}

fn main() {
    assert_eq!(greet("Ada"), "Hello, Ada!", "greet returns exactly Hello, <name>!");
    assert_eq!(greet("Grace Hopper"), "Hello, Grace Hopper!", "greet keeps the whole name");
    assert_eq!(rectangle_area(3, 4), 12, "rectangle_area multiplies width by height");
    assert_eq!(rectangle_area(0, 9), 0, "a zero-width rectangle has no area");
    assert_eq!(format_price("coffee", 305), "coffee: $3.05", "cents below ten keep their leading zero");
    assert_eq!(format_price("tea", 250), "tea: $2.50", "whole tens of cents show two digits");
    assert_eq!(format_price("gum", 5), "gum: $0.05", "prices under a dollar start with $0.");
    assert_eq!(format_price("laptop", 129_999), "laptop: $1299.99", "large prices are not truncated");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-bindings-types':
      return {
        files: { 'main.rs': `${code}

fn main() {
    assert_eq!(add_points(10, 5), 15, "add_points adds normally");
    assert_eq!(add_points(250, 10), 255, "add_points stops at 255 instead of overflowing");
    assert_eq!(add_points(255, 255), 255, "add_points stays at the maximum");
    assert_eq!(checked_total(&[]), Some(0), "no prices total zero");
    assert_eq!(checked_total(&[1_000, 2_500, 99]), Some(3_599), "checked_total adds prices");
    assert_eq!(checked_total(&[u32::MAX, 1]), None, "checked_total reports overflow as None");
    assert_eq!(checked_total(&[u32::MAX - 1, 1]), Some(u32::MAX), "the largest u32 still fits");
    assert_eq!(to_byte(0), Some(0), "0 fits in a u8");
    assert_eq!(to_byte(200), Some(200), "200 fits in a u8");
    assert_eq!(to_byte(255), Some(255), "255 is the largest u8");
    assert_eq!(to_byte(256), None, "256 does not fit in a u8");
    assert_eq!(to_byte(300), None, "300 must be refused, not truncated to 44");
    assert_eq!(to_byte(-1), None, "negative numbers do not fit in a u8");
    assert_eq!(next_tick(41), 42, "next_tick counts up");
    assert_eq!(next_tick(u16::MAX), 0, "next_tick wraps from 65535 to 0");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-control-flow':
      return {
        files: { 'main.rs': `${code}

fn main() {
    assert_eq!(http_class(100), "informational", "100 is informational");
    assert_eq!(http_class(204), "success", "204 is success");
    assert_eq!(http_class(301), "redirect", "301 is a redirect");
    assert_eq!(http_class(404), "client error", "404 is a client error");
    assert_eq!(http_class(499), "client error", "499 is still a client error");
    assert_eq!(http_class(503), "server error", "503 is a server error");
    assert_eq!(http_class(599), "server error", "599 is the last server error");
    assert_eq!(http_class(600), "invalid", "600 is not a valid status");
    assert_eq!(http_class(42), "invalid", "42 is not a valid status");
    let sequence: Vec<String> = (1..=15).map(fizzbuzz).collect();
    assert_eq!(sequence.join(","), "1,2,Fizz,4,Buzz,Fizz,7,8,Fizz,Buzz,11,Fizz,13,14,FizzBuzz", "fizzbuzz from 1 to 15");
    assert_eq!(collatz_steps(1), 0, "1 needs no steps");
    assert_eq!(collatz_steps(6), 8, "6 reaches 1 in 8 steps");
    assert_eq!(collatz_steps(27), 111, "27 reaches 1 in 111 steps");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-collections-strings':
      return {
        files: { 'main.rs': `${code}

fn pairs(items: &[(&str, usize)]) -> Vec<(String, usize)> {
    items.iter().map(|(word, count)| (word.to_string(), *count)).collect()
}

fn main() {
    assert_eq!(char_count(""), 0, "an empty string has no characters");
    assert_eq!(char_count("hello"), 5, "ASCII characters count once each");
    assert_eq!(char_count("héllo"), 5, "é is one character even though it takes two bytes");
    assert_eq!(char_count("日本語"), 3, "each Japanese character counts once");
    assert_eq!(initials("ada lovelace"), "AL", "initials of two words");
    assert_eq!(initials("  grace   brewster  hopper "), "GBH", "extra spaces are ignored");
    assert_eq!(initials("émile zola"), "ÉZ", "accented letters are uppercased, not dropped");
    assert_eq!(initials(""), "", "no words, no initials");
    assert_eq!(word_frequencies("the cat and the hat. The end"), pairs(&[("the", 3), ("and", 1), ("cat", 1), ("end", 1), ("hat", 1)]), "counts ignore case and punctuation; ties sort alphabetically");
    assert_eq!(word_frequencies("Go, go, GO!"), pairs(&[("go", 3)]), "the same word in different cases is one word");
    assert_eq!(word_frequencies("  ... "), pairs(&[]), "punctuation alone is not a word");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-option-result':
      return {
        files: { 'main.rs': `${code}

fn main() {
    let config = "# Pathway service\\nname = learner-api\\nport = 8080\\nbroken line\\n  timeout=30  \\n";
    assert_eq!(find_setting(config, "name"), Some(String::from("learner-api")), "find_setting returns the trimmed value");
    assert_eq!(find_setting(config, "timeout"), Some(String::from("30")), "spaces around the key and value are ignored");
    assert_eq!(find_setting(config, "missing"), None, "a missing key is None");
    assert_eq!(find_setting("# port = 1\\n", "port"), None, "comment lines are ignored");
    assert_eq!(find_setting("port = 1\\nport = 2\\n", "port"), Some(String::from("1")), "the first match wins");
    assert_eq!(port(config), Ok(8080), "port parses the port setting");
    assert_eq!(port("name = x\\n"), Err(String::from("missing port")), "a missing port is an Err, not a panic");
    assert_eq!(port("port = eighty\\n"), Err(String::from("invalid port: eighty")), "a non-numeric port is an Err");
    assert_eq!(port("port = 70000\\n"), Err(String::from("invalid port: 70000")), "a port above 65535 is an Err");
    assert_eq!(parse_all(&["1", " -2 ", "30"]), Ok(vec![1, -2, 30]), "parse_all trims and parses every input");
    assert_eq!(parse_all(&[]), Ok(vec![]), "no inputs parse to an empty list");
    assert_eq!(parse_all(&["4", "x", "y"]), Err(String::from("bad number: x")), "parse_all stops at the first bad input");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-debugging-tests':
      return {
        files: { 'main.rs': `${code}

#[cfg(test)]
mod pathway_grader {
    use super::*;

    #[test]
    fn grader_leap_years() {
        assert!(is_leap_year(2024), "2024 is a leap year");
        assert!(!is_leap_year(2023), "2023 is not a leap year");
        assert!(!is_leap_year(1900), "1900 is not a leap year: divisible by 100 but not 400");
        assert!(is_leap_year(2000), "2000 is a leap year: divisible by 400");
    }

    #[test]
    fn grader_days_in_month() {
        assert_eq!(days_in_month(2024, 1), Some(31), "January has 31 days");
        assert_eq!(days_in_month(2023, 4), Some(30), "April has 30 days");
        assert_eq!(days_in_month(2024, 2), Some(29), "February 2024 has 29 days");
        assert_eq!(days_in_month(1900, 2), Some(28), "February 1900 has 28 days");
        assert_eq!(days_in_month(2023, 0), None, "there is no month 0");
        assert_eq!(days_in_month(2023, 13), None, "there is no month 13");
    }
}
` },
        // Build the test harness, require at least three of the learner's own tests, then run
        // every test: the learner's must pass as well as the grader's.
        command: ['/bin/sh', '-c', 'rustc --edition=2024 --test /workspace/main.rs -o /workspace/main && count=$(/workspace/main --list 2>/dev/null | grep ": test$" | grep -vc "^pathway_grader::") ; if [ "$count" -lt 3 ]; then echo "Write at least three #[test] functions of your own (found $count)."; exit 1; fi && /workspace/main --test-threads=1 && echo PATHWAY_TEST_PASS'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-crates-modules':
      return {
        files: {
          'main.rs': `${code}

fn main() {
    let mut item = inventory::Item::new("widget", 3);
    assert_eq!(item.name, "widget", "name is public");
    assert_eq!(item.quantity(), 3, "quantity() reads the stock level");
    assert_eq!(describe(&item), "widget x3", "describe is re-exported at the crate root");
    assert_eq!(inventory::report::describe(&item), "widget x3", "describe is also reachable through inventory::report");
    assert_eq!(item.remove(2), Ok(()), "removing available stock succeeds");
    assert_eq!(item.quantity(), 1, "remove lowers the quantity");
    assert_eq!(item.remove(5), Err(String::from("only 1 left")), "removing too much is refused");
    assert_eq!(item.quantity(), 1, "a refused removal changes nothing");
    println!("PATHWAY_TEST_PASS");
}
`,
          'probe.rs': `${code}

fn main() {
    let mut item = inventory::Item::new("widget", 3);
    item.quantity = 99;
}
`,
        },
        // probe.rs must NOT compile: code outside the module may not set quantity directly.
        command: ['/bin/sh', '-c', 'if rustc --edition=2024 /workspace/probe.rs -o /workspace/probe >/dev/null 2>&1; then echo "Code outside the inventory module can set item.quantity directly. Keep the quantity field private."; exit 1; fi; rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-cargo-quality':
      return {
        files: { 'main.rs': `${code}

fn main() {
    let build = std::env::args().nth(1).unwrap_or_default();
    let features = enabled_features();
    match build.as_str() {
        "plain" => {
            assert_eq!(log_level(), "info", "without the verbose feature, log_level is info");
            assert_eq!(build_profile(), "debug", "a build without -O is a debug build");
            assert!(features.is_empty(), "no features were enabled, but enabled_features returned {features:?}");
        }
        "metrics" => {
            assert_eq!(log_level(), "info", "the metrics feature alone does not change log_level");
            assert_eq!(features, vec!["metrics"], "only metrics was enabled");
        }
        "full" => {
            assert_eq!(log_level(), "debug", "with the verbose feature, log_level is debug");
            assert_eq!(build_profile(), "release", "a build with -O is a release build");
            assert_eq!(features, vec!["verbose", "metrics"], "both features, verbose first");
            println!("PATHWAY_TEST_PASS");
        }
        other => panic!("unknown build {other}"),
    }
}
` },
        // Compile the same source three ways, the way Cargo would for different feature sets and profiles.
        command: ['/bin/sh', '-c', `rustc --edition=2024 /workspace/main.rs -o /workspace/plain && /workspace/plain plain && rustc --edition=2024 --cfg 'feature="metrics"' /workspace/main.rs -o /workspace/metrics && /workspace/metrics metrics && rustc --edition=2024 -O --cfg 'feature="verbose"' --cfg 'feature="metrics"' /workspace/main.rs -o /workspace/full && /workspace/full full`],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-stack-heap-layout':
      return {
        files: { 'main.rs': `${code}

fn expected_growth(n: usize) -> Vec<usize> {
    let mut values: Vec<u64> = Vec::new();
    let mut seen = Vec::new();
    for i in 0..n {
        let before = values.capacity();
        values.push(i as u64);
        if values.capacity() != before {
            seen.push(values.capacity());
        }
    }
    seen
}

fn main() {
    let reading = SensorReading { id: 7, timestamp: 1_700_000_000, celsius: 21.5, active: true, flags: 0b101 };
    assert!(reading.active && reading.flags == 5 && reading.id == 7 && reading.timestamp > 0 && reading.celsius > 21.0, "SensorReading keeps all five fields");
    assert_eq!(std::mem::align_of::<SensorReading>(), 8, "a struct is aligned like its most-aligned field (u64)");
    assert_eq!(std::mem::size_of::<SensorReading>(), 24, "order the fields so the #[repr(C)] struct takes 24 bytes, not 40");
    assert_eq!(growth_steps(0), Vec::<usize>::new(), "pushing nothing never allocates");
    assert_eq!(growth_steps(1), expected_growth(1), "the first push allocates");
    assert_eq!(growth_steps(100), expected_growth(100), "capacities are recorded each time the Vec reallocates");
    let mut values: Vec<u64> = Vec::with_capacity(10);
    values.extend([1, 2, 3]);
    assert_eq!(vec_footprint(&values), (24, 80), "a Vec<u64> is a 24-byte descriptor plus capacity * 8 bytes on the heap");
    assert_eq!(vec_footprint(&Vec::new()), (24, 0), "an empty Vec has no heap allocation");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-ownership':
      return {
        files: { 'main.rs': `${code}

static DROPS: std::sync::Mutex<Vec<String>> = std::sync::Mutex::new(Vec::new());

fn record_drop(name: &str) {
    DROPS.lock().unwrap().push(name.to_string());
}

fn take_drops() -> Vec<String> {
    std::mem::take(&mut *DROPS.lock().unwrap())
}

fn lease(name: &str) -> Lease {
    Lease { name: String::from(name) }
}

fn main() {
    {
        let _first = lease("alpha");
        let _second = lease("beta");
    }
    assert_eq!(take_drops(), ["beta", "alpha"], "values are dropped at the end of their scope, in reverse order of creation");
    assert_eq!(hand_over(lease("gamma")), 5, "hand_over returns the name's length");
    assert_eq!(take_drops(), ["gamma"], "hand_over owns its Lease, so it is dropped when hand_over returns");
    let kept = keep_longest(lease("short"), lease("lengthy"));
    assert_eq!(take_drops(), ["short"], "keep_longest drops the shorter lease before returning");
    assert_eq!(kept.name, "lengthy", "keep_longest returns the longer lease");
    drop(kept);
    assert_eq!(take_drops(), ["lengthy"], "the returned lease belongs to the caller until the caller drops it");
    let tie = keep_longest(lease("aaa"), lease("bbb"));
    assert_eq!(tie.name, "aaa", "on a tie keep the first lease");
    assert_eq!(take_drops(), ["bbb"], "on a tie the second lease is dropped");
    drop(tie);
    take_drops();
    let original = String::from("quiet please");
    let address = original.as_ptr();
    let loud = shout(original);
    assert_eq!(loud, "QUIET PLEASE", "shout uppercases the text");
    assert_eq!(loud.as_ptr(), address, "shout must reuse the String it was given, not allocate a new one");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-borrowing-references':
      return {
        files: { 'main.rs': `${code}

fn main() {
    let mut names = vec![String::from("ada"), String::from("grace")];
    assert_eq!(total_len(&names), 8, "total_len adds up the lengths");
    assert_eq!(total_len(&[]), 0, "no names, no length");
    add_suffix(&mut names, "!");
    assert_eq!(names, ["ada!", "grace!"], "add_suffix changes each name in place");
    assert_eq!(total_len(&names), 10, "names are still usable after being borrowed");
    let mut values = vec![4, 8];
    assert_eq!(first_and_push(&mut values, 15), 4, "first_and_push returns the first element");
    assert_eq!(values, [4, 8, 15], "first_and_push appends the extra value");
    let text = String::from("borrow the longest word, keep the first");
    let word = longest_word(&text);
    assert_eq!(word, "longest", "longest_word finds the longest word");
    assert_eq!(longest_word("tie two"), "tie", "on a tie the first word wins");
    assert_eq!(longest_word("   "), "", "no words gives an empty slice");
    assert_eq!(word.as_ptr(), text[11..].as_ptr(), "longest_word borrows from the text instead of copying");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-lifetimes':
      return {
        files: { 'main.rs': `${code}

fn main() {
    let outer = String::from("a longer string");
    let result;
    {
        let inner = String::from("short");
        result = longer(&outer, &inner).to_string();
    }
    assert_eq!(result, "a longer string", "longer returns the longer string");
    assert_eq!(longer("abc", "xyz"), "abc", "on a tie longer returns the first");
    assert_eq!(first_line("one\\ntwo"), "one", "first_line stops at the newline");
    assert_eq!(first_line("only"), "only", "a single line is returned whole");
    assert_eq!(first_line(""), "", "an empty text has an empty first line");

    let text = String::from("  alpha beta\\tgamma  ");
    let kept: Vec<&str>;
    {
        let mut words = Words::new(&text);
        let first = words.next_word();
        let second = words.next_word();
        let third = words.next_word();
        assert_eq!((first, second, third), (Some("alpha"), Some("beta"), Some("gamma")), "next_word walks the words in order");
        assert_eq!(words.next_word(), None, "next_word returns None at the end");
        kept = vec![first.unwrap(), second.unwrap()];
    }
    assert_eq!(kept, ["alpha", "beta"], "words borrowed from the text outlive the Words iterator");
    assert_eq!(Words::new("   ").next_word(), None, "whitespace alone has no words");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-cli-args-env':
      return {
        files: { 'main.rs': `${code}

fn args(items: &[&str]) -> Vec<String> {
    items.iter().map(|item| item.to_string()).collect()
}

fn config(pattern: &str, paths: &[&str], ignore_case: bool, max_count: Option<usize>) -> Config {
    Config { pattern: pattern.to_string(), paths: args(paths), ignore_case, max_count }
}

fn main() {
    assert_eq!(parse_args(&args(&["todo", "a.txt", "b.txt"])), Ok(config("todo", &["a.txt", "b.txt"], false, None)), "the first plain argument is the pattern, the rest are paths");
    assert_eq!(parse_args(&args(&["-i", "todo"])), Ok(config("todo", &[], true, None)), "-i turns on ignore_case");
    assert_eq!(parse_args(&args(&["todo", "--ignore-case", "notes.md"])), Ok(config("todo", &["notes.md"], true, None)), "options may come after the pattern");
    assert_eq!(parse_args(&args(&["-m", "3", "fix"])), Ok(config("fix", &[], false, Some(3))), "-m takes the next argument as a number");
    assert_eq!(parse_args(&args(&["--max-count", "10", "fix", "x.rs"])), Ok(config("fix", &["x.rs"], false, Some(10))), "--max-count is the long form");
    assert_eq!(parse_args(&args(&["--", "-i", "file"])), Ok(config("-i", &["file"], false, None)), "after -- everything is a plain argument");
    assert_eq!(parse_args(&args(&["needle", "-"])), Ok(config("needle", &["-"], false, None)), "a lone - is a plain argument (standard input)");
    assert_eq!(parse_args(&args(&[])), Err(String::from("missing pattern")), "no arguments is an error");
    assert_eq!(parse_args(&args(&["-i"])), Err(String::from("missing pattern")), "options alone are not enough");
    assert_eq!(parse_args(&args(&["fix", "-m"])), Err(String::from("missing value for -m")), "-m needs a value");
    assert_eq!(parse_args(&args(&["-m", "lots", "fix"])), Err(String::from("invalid number for -m: lots")), "-m needs a number");
    assert_eq!(parse_args(&args(&["-z", "fix"])), Err(String::from("unknown option: -z")), "unknown options are reported");
    assert!(color_enabled(None, Some("xterm-256color")), "color is on by default");
    assert!(!color_enabled(Some("1"), Some("xterm")), "NO_COLOR turns color off");
    assert!(color_enabled(Some(""), Some("xterm")), "an empty NO_COLOR does not count");
    assert!(!color_enabled(None, Some("dumb")), "TERM=dumb turns color off");
    assert!(color_enabled(None, None), "no TERM at all still allows color");
    assert_eq!(exit_code(&Ok(3)), 0, "matches found means success");
    assert_eq!(exit_code(&Ok(0)), 1, "no matches is exit code 1");
    assert_eq!(exit_code(&Err(String::from("bad"))), 2, "an error is exit code 2");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-stdio-buffering':
      return {
        files: { 'main.rs': `${code}

struct ClosedPipe;

impl std::io::Write for ClosedPipe {
    fn write(&mut self, _: &[u8]) -> std::io::Result<usize> {
        Err(std::io::Error::new(std::io::ErrorKind::BrokenPipe, "reader went away"))
    }
    fn flush(&mut self) -> std::io::Result<()> {
        Ok(())
    }
}

fn main() {
    use std::io::Cursor;
    assert_eq!(count(Cursor::new("")).unwrap(), (0, 0, 0), "empty input");
    assert_eq!(count(Cursor::new("one two\\nthree\\n")).unwrap(), (2, 3, 14), "lines, words and bytes including newlines");
    assert_eq!(count(Cursor::new("no newline at end")).unwrap(), (1, 4, 17), "a last line without a newline still counts");
    assert_eq!(count(Cursor::new("héllo wörld\\n")).unwrap(), (1, 2, 14), "bytes, not characters");
    assert_eq!(count(Cursor::new("\\n\\n")).unwrap(), (2, 0, 2), "blank lines count as lines");
    let invalid = count(Cursor::new(vec![b'o', b'k', b'\\n', 0xff, 0xfe, b'\\n']));
    assert_eq!(invalid.map_err(|e| e.kind()), Err(std::io::ErrorKind::InvalidData), "invalid UTF-8 is returned as an error, not a panic");
    let mut out = Vec::new();
    number_lines(Cursor::new("alpha\\nbeta\\n"), &mut out).unwrap();
    assert_eq!(String::from_utf8(out).unwrap(), "   1  alpha\\n   2  beta\\n", "number_lines right-aligns numbers to width 4 and adds two spaces");
    let mut out = Vec::new();
    number_lines(Cursor::new(""), &mut out).unwrap();
    assert!(out.is_empty(), "no input, no output");
    let result = number_lines(Cursor::new("alpha\\n"), ClosedPipe);
    assert_eq!(result.map_err(|e| e.kind()), Err(std::io::ErrorKind::BrokenPipe), "a failed write is returned to the caller");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-os-files-syscalls':
      return {
        files: { 'main.rs': `${code}

fn main() {
    use std::path::Path;
    let dir = Path::new("pathway-files");
    let _ = std::fs::remove_dir_all(dir);
    std::fs::create_dir_all(dir).unwrap();
    let log = dir.join("events.log");
    append_line(&log, "started").unwrap();
    append_line(&log, "working").unwrap();
    append_line(&log, "stopped").unwrap();
    assert_eq!(std::fs::read_to_string(&log).unwrap(), "started\\nworking\\nstopped\\n", "append_line creates the file and adds one line per call");
    assert_eq!(tail(&log, 2).unwrap(), ["working", "stopped"], "tail returns the last n lines in order");
    assert_eq!(tail(&log, 10).unwrap(), ["started", "working", "stopped"], "asking for more lines than exist returns them all");
    assert_eq!(tail(&log, 0).unwrap(), Vec::<String>::new(), "tail of zero lines is empty");
    let missing = tail(&dir.join("missing.log"), 3);
    assert_eq!(missing.map_err(|e| e.kind()), Err(std::io::ErrorKind::NotFound), "a missing file is reported as NotFound, not hidden");
    let settings = dir.join("settings.json");
    write_atomically(&settings, "{\\"version\\": 1}").unwrap();
    write_atomically(&settings, "{\\"version\\": 2}").unwrap();
    assert_eq!(std::fs::read_to_string(&settings).unwrap(), "{\\"version\\": 2}", "write_atomically replaces the contents");
    let names: Vec<String> = std::fs::read_dir(dir).unwrap().map(|entry| entry.unwrap().file_name().into_string().unwrap()).filter(|name| name.starts_with("settings")).collect();
    assert_eq!(names, ["settings.json"], "no temporary file is left behind");
    std::fs::remove_dir_all(dir).unwrap();
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'cd /workspace && rustc --edition=2024 main.rs -o main && ./main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-processes-signals':
      return {
        files: { 'main.rs': `${code}

fn main() {
    assert_eq!(run_capture("sh", &["-c", "echo hello; exit 3"]).unwrap(), (3, String::from("hello\\n")), "run_capture returns the exit code and standard output");
    assert_eq!(run_capture("true", &[]).unwrap(), (0, String::new()), "a successful program exits with 0");
    assert_eq!(run_capture("sh", &["-c", "kill -9 $$"]).unwrap().0, -1, "a program killed by a signal has no exit code: report -1");
    let missing = run_capture("pathway-no-such-program", &[]);
    assert_eq!(missing.map_err(|e| e.kind()), Err(std::io::ErrorKind::NotFound), "a missing program is an error, not a panic");
    assert_eq!(sort_lines("pear\\napple\\nBanana\\n").unwrap(), "Banana\\napple\\npear\\n", "sort_lines pipes through sort in byte order");
    assert_eq!(sort_lines("").unwrap(), "", "sorting nothing returns nothing");
    let big: String = (0..20_000).rev().map(|n| format!("{n:05}\\n")).collect();
    let sorted = sort_lines(&big).unwrap();
    assert!(sorted.starts_with("00000\\n00001\\n") && sorted.ends_with("19999\\n"), "large input is sorted without deadlocking");
    let dir = std::path::Path::new("pathway-proc");
    let _ = std::fs::remove_dir_all(dir);
    std::fs::create_dir_all(dir).unwrap();
    let spaced = dir.join("my notes.txt");
    std::fs::write(&spaced, "twelve bytes").unwrap();
    assert_eq!(file_size(spaced.to_str().unwrap()).unwrap(), "12", "file names with spaces are passed as one argument");
    let _ = std::fs::remove_file("injected");
    let tricky = dir.join("$(touch injected).txt");
    std::fs::write(&tricky, "abc").unwrap();
    assert_eq!(file_size(tricky.to_str().unwrap()).unwrap(), "3", "a file name is data, never a command");
    assert!(!std::path::Path::new("injected").exists(), "the file name ran as a shell command: pass arguments without a shell");
    assert!(file_size("pathway-proc/missing.txt").is_err(), "a missing file is an error");
    std::fs::remove_dir_all(dir).unwrap();
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'cd /workspace && rustc --edition=2024 main.rs -o main && ./main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-serde-formats':
      return {
        files: { 'main.rs': `${code}

fn main() {
    let full = parse_config(r#"{"name": "learner-api", "port": 9000, "tags": ["edge", "beta"], "logLevel": "debug"}"#).unwrap();
    assert_eq!(full, ServiceConfig { name: String::from("learner-api"), port: 9000, tags: vec![String::from("edge"), String::from("beta")], log_level: String::from("debug") }, "every field is read, with logLevel mapped to log_level");
    let minimal = parse_config(r#"{"name": "worker"}"#).unwrap();
    assert_eq!(minimal, ServiceConfig { name: String::from("worker"), port: 8080, tags: vec![], log_level: String::from("info") }, "missing fields use their defaults");
    let typo = parse_config(r#"{"name": "api", "prot": 9000}"#);
    assert!(typo.as_ref().is_err_and(|message| message.contains("unknown field")), "an unknown field such as a typo is rejected, got {typo:?}");
    let missing = parse_config(r#"{"port": 1}"#);
    assert!(missing.as_ref().is_err_and(|message| message.contains("missing field")), "name is required, got {missing:?}");
    let wrong_type = parse_config(r#"{"name": "api", "port": "eighty"}"#);
    assert!(wrong_type.is_err(), "a port must be a number");
    let too_big = parse_config(r#"{"name": "api", "port": 70000}"#);
    assert!(too_big.is_err(), "a port above 65535 does not fit in a u16");
    let text = to_pretty(&full);
    assert!(text.contains("\\n  \\"logLevel\\": \\"debug\\""), "to_pretty writes indented JSON using the logLevel key, got {text}");
    assert_eq!(parse_config(&text).unwrap(), full, "pretty output parses back to the same config");
    println!("PATHWAY_TEST_PASS");
}
` },
        // pathway-rustc links the image's prebuilt serde and serde_json (see images/rust/Dockerfile).
        command: ['/bin/sh', '-c', 'pathway-rustc /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-cli-project':
      return {
        files: { 'main.rs': `${code}

fn grep(args: &[&str], input: &str) -> (i32, String, String) {
    let args: Vec<String> = args.iter().map(|arg| arg.to_string()).collect();
    let mut stdin = std::io::Cursor::new(input.as_bytes().to_vec());
    let (mut stdout, mut stderr) = (Vec::new(), Vec::new());
    let code = run(&args, &mut stdin, &mut stdout, &mut stderr);
    (code, String::from_utf8(stdout).unwrap(), String::from_utf8(stderr).unwrap())
}

fn main() {
    let dir = std::path::Path::new("pathway-grep");
    let _ = std::fs::remove_dir_all(dir);
    std::fs::create_dir_all(dir).unwrap();
    std::fs::write(dir.join("a.txt"), "TODO: write tests\\ndone\\ntodo later\\n").unwrap();
    std::fs::write(dir.join("b.txt"), "nothing here\\nanother TODO\\n").unwrap();
    let (a, b, missing) = ("pathway-grep/a.txt", "pathway-grep/b.txt", "pathway-grep/missing.txt");

    assert_eq!(grep(&["TODO"], "one TODO\\ntwo\\nTODO three\\n"), (0, String::from("one TODO\\nTODO three\\n"), String::new()), "with no files, read standard input and print matching lines");
    assert_eq!(grep(&["absent"], "one\\ntwo\\n"), (1, String::new(), String::new()), "no matches exits with 1 and prints nothing");
    assert_eq!(grep(&["-i", "todo", a], "").1, "TODO: write tests\\ntodo later\\n", "-i ignores case; one file means no file-name prefix");
    assert_eq!(grep(&["-n", "TODO", a], "").1, "1:TODO: write tests\\n", "-n prefixes line numbers");
    assert_eq!(grep(&["TODO", a, b], "").1, "pathway-grep/a.txt:TODO: write tests\\npathway-grep/b.txt:another TODO\\n", "several files prefix each line with its file name");
    assert_eq!(grep(&["-c", "-i", "todo", a], ""), (0, String::from("2\\n"), String::new()), "-c prints only the count");
    assert_eq!(grep(&["-c", "TODO", a, b], "").1, "pathway-grep/a.txt:1\\npathway-grep/b.txt:1\\n", "-c with several files counts each one");
    let (code, out, err) = grep(&["TODO", missing, b], "");
    assert_eq!(code, 2, "an unreadable file makes the exit code 2");
    assert_eq!(out, "pathway-grep/b.txt:another TODO\\n", "the remaining files are still searched after an error");
    assert!(err.starts_with("minigrep: pathway-grep/missing.txt: "), "errors go to stderr, prefixed with the program and file name, got {err:?}");
    let (code, out, err) = grep(&[], "");
    assert_eq!((code, out.as_str()), (2, ""), "a missing pattern exits with 2");
    assert!(err.contains("usage: minigrep"), "a usage error prints the usage line to stderr, got {err:?}");
    assert_eq!(grep(&["-x", "a"], "").0, 2, "unknown options are usage errors");
    std::fs::remove_dir_all(dir).unwrap();
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'cd /workspace && rustc --edition=2024 main.rs -o main && ./main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-net-tcp-basics':
      return {
        files: { 'main.rs': `${code}

fn main() {
    std::thread::spawn(|| {
        std::thread::sleep(std::time::Duration::from_secs(6));
        eprintln!("The test timed out: something is waiting forever (a missing shutdown, timeout, or concurrent handler?).");
        std::process::exit(1);
    });
    let (listener, address) = local_listener().unwrap();
    assert!(address.ip().is_loopback(), "listen on 127.0.0.1 only, not every interface (got {address})");
    assert_ne!(address.port(), 0, "local_listener returns the port the OS actually chose");
    let message: Vec<u8> = (0..50_000u32).map(|i| (i % 251) as u8).collect();
    std::thread::scope(|scope| {
        let server = scope.spawn(|| echo_once(&listener));
        let reply = request(address, &message).unwrap();
        assert_eq!(reply.len(), message.len(), "every byte sent comes back, however TCP splits it");
        assert!(reply == message, "the echoed bytes match what was sent");
        assert_eq!(server.join().unwrap().unwrap(), 50_000, "echo_once returns how many bytes it echoed");
    });
    std::thread::scope(|scope| {
        let server = scope.spawn(|| echo_once(&listener));
        assert_eq!(request(address, b"").unwrap(), Vec::<u8>::new(), "an empty request gets an empty reply");
        assert_eq!(server.join().unwrap().unwrap(), 0, "nothing was echoed");
    });
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-net-framing':
      return {
        files: { 'main.rs': `${code}

struct Trickle<R>(R);

impl<R: std::io::Read> std::io::Read for Trickle<R> {
    fn read(&mut self, buffer: &mut [u8]) -> std::io::Result<usize> {
        let end = buffer.len().min(1);
        self.0.read(&mut buffer[..end])
    }
}

fn kind<T: std::fmt::Debug>(result: std::io::Result<T>) -> std::io::ErrorKind {
    result.expect_err("expected an error").kind()
}

fn main() {
    use std::io::{Cursor, ErrorKind};
    let mut wire = Vec::new();
    write_frame(&mut wire, b"hello").unwrap();
    write_frame(&mut wire, b"").unwrap();
    write_frame(&mut wire, &[7u8; 300]).unwrap();
    assert_eq!(&wire[..9], &[0, 0, 0, 5, b'h', b'e', b'l', b'l', b'o'], "a frame is a 4-byte big-endian length, then the payload");
    assert_eq!(wire.len(), 9 + 4 + 304, "an empty frame is just its 4-byte header");
    let mut reader = Trickle(Cursor::new(wire));
    assert_eq!(read_frame(&mut reader, 1024).unwrap(), Some(b"hello".to_vec()), "frames are reassembled even when each read returns one byte");
    assert_eq!(read_frame(&mut reader, 1024).unwrap(), Some(Vec::new()), "an empty frame is Some(empty), not the end");
    assert_eq!(read_frame(&mut reader, 1024).unwrap(), Some(vec![7u8; 300]), "a longer payload is read completely");
    assert_eq!(read_frame(&mut reader, 1024).unwrap(), None, "a clean end between frames is Ok(None)");
    assert_eq!(kind(read_frame(&mut Cursor::new(vec![0xff, 0xff, 0xff, 0xff]), 1024)), ErrorKind::InvalidData, "a length above max_len is InvalidData");
    let mut big = Vec::new();
    write_frame(&mut big, &[1u8; 300]).unwrap();
    assert_eq!(kind(read_frame(&mut Cursor::new(big), 299)), ErrorKind::InvalidData, "max_len is enforced exactly");
    assert_eq!(kind(read_frame(&mut Trickle(Cursor::new(vec![0, 0])), 1024)), ErrorKind::UnexpectedEof, "a stream that ends inside a header is UnexpectedEof");
    assert_eq!(kind(read_frame(&mut Trickle(Cursor::new(vec![0, 0, 0, 5, b'h', b'i'])), 1024)), ErrorKind::UnexpectedEof, "a stream that ends inside a payload is UnexpectedEof");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-net-concurrency-timeouts':
      return {
        files: { 'main.rs': `${code}

fn main() {
    use std::io::{Read, Write};
    use std::time::{Duration, Instant};
    std::thread::spawn(|| {
        std::thread::sleep(std::time::Duration::from_secs(6));
        eprintln!("The test timed out: something is waiting forever (a missing shutdown, timeout, or concurrent handler?).");
        std::process::exit(1);
    });
    let listener = std::net::TcpListener::bind("127.0.0.1:0").unwrap();
    let address = listener.local_addr().unwrap();
    std::thread::scope(|scope| {
        let server = scope.spawn(|| serve(&listener, 3, Duration::from_millis(600)));
        let idle = std::net::TcpStream::connect(address).unwrap();
        let mut busy = std::net::TcpStream::connect(address).unwrap();
        let started = Instant::now();
        busy.write_all(b"ADD 2\\nADD 3\\nADD nine\\nQUIT\\n").unwrap();
        let mut reply = String::new();
        busy.read_to_string(&mut reply).unwrap();
        let elapsed = started.elapsed();
        assert_eq!(reply, "TOTAL 2\\nTOTAL 5\\nERR bad command\\nBYE 5\\n", "each command gets its reply, and QUIT closes the connection");
        assert!(elapsed < Duration::from_millis(400), "a second client was served while the first sat idle (took {elapsed:?}): handle clients concurrently");
        let mut closing = std::net::TcpStream::connect(address).unwrap();
        closing.write_all(b"ADD 7\\n").unwrap();
        closing.shutdown(std::net::Shutdown::Write).unwrap();
        let mut reply = String::new();
        closing.read_to_string(&mut reply).unwrap();
        assert_eq!(reply, "TOTAL 7\\n", "a client that closes without QUIT still gets its replies");
        let results = server.join().unwrap();
        drop(idle);
        assert_eq!(results.len(), 3, "serve returns one result per client");
        assert_eq!(results[0].as_ref().map_err(|e| e.kind()), Err(std::io::ErrorKind::TimedOut), "an idle client times out with TimedOut");
        assert_eq!(results[1].as_ref().ok(), Some(&5), "the busy client's total");
        assert_eq!(results[2].as_ref().ok(), Some(&7), "the closing client's total");
    });
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-net-tokio-kv':
      return {
        files: { 'main.rs': `${code}

async fn send(stream: &mut tokio::net::TcpStream, line: &str) -> String {
    use tokio::io::{AsyncReadExt, AsyncWriteExt};
    stream.write_all(format!("{line}\\n").as_bytes()).await.unwrap();
    let mut reply = Vec::new();
    loop {
        let byte = stream.read_u8().await.unwrap();
        if byte == b'\\n' {
            break;
        }
        reply.push(byte);
    }
    String::from_utf8(reply).unwrap()
}

#[tokio::main]
async fn main() {
    std::thread::spawn(|| {
        std::thread::sleep(std::time::Duration::from_secs(6));
        eprintln!("The test timed out: something is waiting forever (a missing shutdown, timeout, or concurrent handler?).");
        std::process::exit(1);
    });
    let store: Store = Arc::new(Mutex::new(HashMap::new()));
    assert_eq!(apply("SET greeting hello world", &store), "OK", "SET stores a value");
    assert_eq!(apply("GET greeting", &store), "VALUE hello world", "values may contain spaces");
    assert_eq!(apply("GET missing", &store), "NOT_FOUND", "a missing key");
    assert_eq!(apply("COUNT", &store), "COUNT 1", "COUNT reports the number of keys");
    assert_eq!(apply("DEL greeting", &store), "DELETED", "DEL removes a key");
    assert_eq!(apply("DEL greeting", &store), "NOT_FOUND", "deleting twice finds nothing");
    for bad in ["FLY away", "GET", "SET onlykey", ""] {
        assert_eq!(apply(bad, &store), "ERR unknown command", "malformed command {bad:?}");
    }
    let listener = tokio::net::TcpListener::bind("127.0.0.1:0").await.unwrap();
    let address = listener.local_addr().unwrap();
    tokio::spawn(serve(listener, Arc::clone(&store)));
    let idle = tokio::net::TcpStream::connect(address).await.unwrap();
    let checks = async {
        let mut writer = tokio::net::TcpStream::connect(address).await.unwrap();
        assert_eq!(send(&mut writer, "SET lang rust").await, "OK");
        let mut reader = tokio::net::TcpStream::connect(address).await.unwrap();
        assert_eq!(send(&mut reader, "GET lang").await, "VALUE rust", "every connection shares one store");
        assert_eq!(send(&mut reader, "COUNT").await, "COUNT 1");
        assert_eq!(send(&mut writer, "NOPE").await, "ERR unknown command");
    };
    tokio::time::timeout(std::time::Duration::from_secs(2), checks).await.expect("clients were served while another connection sat idle: spawn a task per connection");
    drop(idle);
    println!("PATHWAY_TEST_PASS");
}
` },
        // pathway-rustc links the image's prebuilt Tokio (with the net feature).
        command: ['/bin/sh', '-c', 'pathway-rustc /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-bytes-endianness':
      return {
        files: { 'main.rs': `${code}

fn main() {
    assert_eq!(read_u32_le(&[1, 0, 0, 0]), Some(1), "little-endian: the first byte is the least significant");
    assert_eq!(read_u32_le(&[0x78, 0x56, 0x34, 0x12, 0xff]), Some(0x1234_5678), "only the first four bytes are read");
    assert_eq!(read_u32_le(&[0xff, 0xff, 0xff, 0xff]), Some(u32::MAX), "all ones");
    assert_eq!(read_u32_le(&[1, 2, 3]), None, "fewer than four bytes is None, not a panic");
    assert_eq!(set_bit(0b0000_0000, 3, true), 0b0000_1000, "set bit 3");
    assert_eq!(set_bit(0b1111_1111, 0, false), 0b1111_1110, "clear bit 0");
    assert_eq!(set_bit(0b0000_0100, 1, false), 0b0000_0100, "clearing a bit that is already clear changes nothing");
    assert_eq!(set_bit(0b1000_0000, 7, true), 0b1000_0000, "setting a bit that is already set changes nothing");
    assert_eq!(set_bit(0b0101_0101, 8, true), 0b0101_0101, "bit 8 doesn't exist in a u8: unchanged, no panic");
    assert_eq!(set_bit(0b0101_0101, 200, false), 0b0101_0101, "huge bit numbers are ignored too");
    assert_eq!(pack_rgb565(255, 255, 255), 0xffff, "white");
    assert_eq!(pack_rgb565(255, 0, 0), 0xf800, "red occupies the top 5 bits");
    assert_eq!(pack_rgb565(0, 255, 0), 0x07e0, "green occupies the middle 6 bits");
    assert_eq!(pack_rgb565(0, 0, 255), 0x001f, "blue occupies the low 5 bits");
    assert_eq!(pack_rgb565(0x12, 0x34, 0x56), 0x11aa, "low bits of each channel are dropped");
    assert_eq!(unpack_rgb565(0xf800), (0xf8, 0, 0), "unpack red");
    assert_eq!(unpack_rgb565(0x07e0), (0, 0xfc, 0), "unpack green");
    assert_eq!(unpack_rgb565(pack_rgb565(200, 100, 50)), (200 & 0xf8, 100 & 0xfc, 50 & 0xf8), "a round trip keeps the high bits of each channel");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-slices-buffers':
      return {
        files: { 'main.rs': `${code}

fn main() {
    let packet = [1u8, 2, 3, 4, 5, 6];
    let (header, body) = split_header(&packet, 2).unwrap();
    assert_eq!((header, body), (&[1u8, 2][..], &[3u8, 4, 5, 6][..]), "split_header splits at header_len");
    assert!(header.as_ptr() == packet.as_ptr() && body.as_ptr() == packet[2..].as_ptr(), "split_header borrows the packet instead of copying it");
    assert_eq!(split_header(&packet, 6).map(|(h, b)| (h.len(), b.len())), Some((6, 0)), "a header can fill the whole packet");
    assert_eq!(split_header(&packet, 7), None, "a header longer than the packet is None, not a panic");
    assert_eq!(read_at(&packet, 1, 3), Some(&[2u8, 3, 4][..]), "read_at returns len bytes from offset");
    assert_eq!(read_at(&packet, 6, 0), Some(&[][..]), "an empty read at the end is fine");
    assert_eq!(read_at(&packet, 4, 3), None, "reading past the end is None");
    assert_eq!(read_at(&packet, 2, usize::MAX), None, "offset + len overflowing usize is None, not a wrap-around or panic");
    assert_eq!(read_at(&packet, usize::MAX, 2), None, "a huge offset is None");
    assert_eq!(find_subslice(b"GET /index HTTP/1.1", b"HTTP"), Some(11), "find_subslice returns the first match position");
    assert_eq!(find_subslice(b"abcabc", b"bc"), Some(1), "the first of several matches");
    assert_eq!(find_subslice(b"abc", b"abcd"), None, "a needle longer than the haystack is not found");
    assert_eq!(find_subslice(b"abc", b""), Some(0), "an empty needle matches at 0");
    assert_eq!(chunk_sums(&[1, 2, 3, 4, 5], 2), vec![3, 7, 5], "chunk_sums adds each chunk; the last may be short");
    assert_eq!(chunk_sums(&[255, 255, 255], 3), vec![765], "sums don't overflow a u8");
    assert_eq!(chunk_sums(&[1, 2], 0), Vec::<u32>::new(), "a chunk size of 0 gives no chunks instead of panicking");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-binary-parser':
      return {
        files: { 'main.rs': `${code}

fn file(version: u8, records: &[(u32, &[u8])]) -> Vec<u8> {
    let mut bytes = b"PWAY".to_vec();
    bytes.push(version);
    bytes.extend_from_slice(&(records.len() as u16).to_be_bytes());
    for (id, name) in records {
        bytes.extend_from_slice(&id.to_be_bytes());
        bytes.push(name.len() as u8);
        bytes.extend_from_slice(name);
    }
    bytes
}

fn main() {
    let valid = file(1, &[(7, b"ada"), (0x0102_0304, "émile".as_bytes()), (9, b"")]);
    assert_eq!(parse(&valid), Ok(vec![
        Record { id: 7, name: String::from("ada") },
        Record { id: 0x0102_0304, name: String::from("émile") },
        Record { id: 9, name: String::new() },
    ]), "a valid file parses into its records, with big-endian ids");
    assert_eq!(parse(&file(1, &[])), Ok(vec![]), "a file with no records is valid");
    assert_eq!(parse(b"PWAX\\x01\\x00\\x00"), Err(ParseError::BadMagic), "wrong magic bytes");
    assert_eq!(parse(&file(2, &[])), Err(ParseError::UnsupportedVersion(2)), "only version 1 is supported");
    assert_eq!(parse(&file(1, &[(1, &[0xff, 0xfe])])), Err(ParseError::InvalidUtf8), "names must be UTF-8");
    let mut trailing = file(1, &[(1, b"x")]);
    trailing.push(0);
    assert_eq!(parse(&trailing), Err(ParseError::TrailingBytes), "bytes after the last record are an error");
    for end in 0..valid.len() {
        let expected = if end < 4 { ParseError::BadMagic } else { ParseError::Truncated };
        assert_eq!(parse(&valid[..end]), Err(expected), "a file cut off after {end} bytes");
    }
    let mut huge = file(1, &[]);
    huge[5] = 0xff;
    huge[6] = 0xff;
    assert_eq!(parse(&huge), Err(ParseError::Truncated), "a record count with no records behind it is Truncated");
    std::panic::set_hook(Box::new(|_| {}));
    let mut seed: u64 = 0x2545_f491_4f6c_dd1d;
    for round in 0..3_000 {
        let mut input = if round % 2 == 0 { b"PWAY\\x01".to_vec() } else { Vec::new() };
        seed = seed.wrapping_mul(6_364_136_223_846_793_005).wrapping_add(1);
        let length = (seed >> 58) as usize;
        for _ in 0..length {
            seed = seed.wrapping_mul(6_364_136_223_846_793_005).wrapping_add(1);
            input.push((seed >> 56) as u8);
        }
        let outcome = std::panic::catch_unwind(|| parse(&input));
        assert!(outcome.is_ok(), "parse panicked on input {input:?}: return an error instead");
    }
    let _ = std::panic::take_hook();
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-perf-cache-benchmark':
      return {
        files: { 'main.rs': `${code}

struct Counting;
static ALLOCATIONS: std::sync::atomic::AtomicUsize = std::sync::atomic::AtomicUsize::new(0);

unsafe impl std::alloc::GlobalAlloc for Counting {
    unsafe fn alloc(&self, layout: std::alloc::Layout) -> *mut u8 {
        ALLOCATIONS.fetch_add(1, std::sync::atomic::Ordering::Relaxed);
        unsafe { std::alloc::System.alloc(layout) }
    }
    unsafe fn dealloc(&self, pointer: *mut u8, layout: std::alloc::Layout) {
        unsafe { std::alloc::System.dealloc(pointer, layout) }
    }
    unsafe fn realloc(&self, pointer: *mut u8, layout: std::alloc::Layout, size: usize) -> *mut u8 {
        ALLOCATIONS.fetch_add(1, std::sync::atomic::Ordering::Relaxed);
        unsafe { std::alloc::System.realloc(pointer, layout, size) }
    }
}

#[global_allocator]
static GLOBAL: Counting = Counting;

fn measure<T>(work: impl FnOnce() -> T) -> (T, usize) {
    let before = ALLOCATIONS.load(std::sync::atomic::Ordering::Relaxed);
    let result = work();
    (result, ALLOCATIONS.load(std::sync::atomic::Ordering::Relaxed) - before)
}

fn main() {
    let words = ["alpha", "beta", "gamma", "delta"];
    let (joined, allocations) = measure(|| join_words(&words, ", "));
    assert_eq!(joined, "alpha, beta, gamma, delta", "join_words joins with the separator");
    assert_eq!(allocations, 1, "join_words should allocate exactly once (it allocated {allocations} times)");
    let (empty, allocations) = measure(|| join_words(&[], ", "));
    assert_eq!((empty.as_str(), allocations), ("", 0), "joining nothing allocates nothing");
    let line = "4, 8, fifteen, 16 ,23,42, ";
    let (numbers, allocations) = measure(|| parse_numbers(line));
    assert_eq!(numbers, vec![4, 8, 16, 23, 42], "parse_numbers keeps the valid numbers in order");
    assert!(allocations <= 1, "parse_numbers should allocate at most once (it allocated {allocations} times)");
    let mut values = vec![1, 1, 2, 3, 3, 3, 7, 9, 9];
    let ((), allocations) = measure(|| dedup_sorted(&mut values));
    assert_eq!(values, vec![1, 2, 3, 7, 9], "dedup_sorted removes repeats");
    assert_eq!(allocations, 0, "dedup_sorted should work in place without allocating (it allocated {allocations} times)");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-memory-addresses-raw':
      return {
        files: {
          'submission.rs': code,
          'main.rs': `${code}

fn main() {
    let values = [10u32, 20, 30];
    assert_eq!(checked_read(&values, 0), Some(10), "the first element");
    assert_eq!(checked_read(&values, 2), Some(30), "the last element");
    assert_eq!(checked_read(&values, 3), None, "an index past the end is None, never a read past the slice");
    assert_eq!(checked_read(&[], 0), None, "an empty slice has no elements");
    // SAFETY: values has 3 elements, so index 1 is in bounds.
    assert_eq!(unsafe { read_nth(values.as_ptr(), 1) }, 20, "read_nth counts in elements, not bytes");
    assert_eq!(element_stride(&[1u64, 2, 3]), 8, "neighbouring u64s are 8 bytes apart");
    assert_eq!(element_stride(&[1u64]), 0, "fewer than two elements has no stride");
    println!("PATHWAY_TEST_PASS");
}
`,
        },
        command: ['/bin/sh', '-c', 'blocks=$(grep -o "unsafe {" /workspace/submission.rs | wc -l); notes=$(grep -o "SAFETY:" /workspace/submission.rs | wc -l); if [ "$notes" -lt "$blocks" ]; then echo "Every unsafe block needs a // SAFETY: comment explaining why it is sound ($blocks unsafe blocks, $notes SAFETY comments)."; exit 1; fi; rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-unsafe-ffi':
      return {
        files: {
          'submission.rs': code,
          'main.rs': `${code}

fn main() {
    assert_eq!(c_strlen("hello"), Some(5), "strlen counts bytes up to the NUL");
    assert_eq!(c_strlen(""), Some(0), "an empty string");
    assert_eq!(c_strlen("héllo"), Some(6), "C counts bytes, not characters");
    assert_eq!(c_strlen("bad\\0text"), None, "text containing a NUL byte can't become a C string: None, not a panic");
    assert_eq!(c_abs(-42), Some(42), "abs of a negative number");
    assert_eq!(c_abs(7), Some(7), "abs of a positive number");
    assert_eq!(c_abs(i32::MAX), Some(i32::MAX), "the largest int");
    assert_eq!(c_abs(i32::MIN), None, "abs(INT_MIN) is undefined behavior in C, so it must never be called");
    let owned = std::ffi::CString::new("from C").unwrap();
    // SAFETY: owned is a live, NUL-terminated string.
    assert_eq!(unsafe { from_c_string(owned.as_ptr()) }, Some(String::from("from C")), "reads a C string");
    // SAFETY: a null pointer is explicitly allowed by from_c_string's contract.
    assert_eq!(unsafe { from_c_string(std::ptr::null()) }, None, "a null pointer is None, never dereferenced");
    let invalid = std::ffi::CString::new(vec![0xffu8, 0xfe]).unwrap();
    // SAFETY: invalid is a live, NUL-terminated string.
    assert_eq!(unsafe { from_c_string(invalid.as_ptr()) }, None, "bytes that aren't UTF-8 are None, not replaced");
    println!("PATHWAY_TEST_PASS");
}
`,
        },
        command: ['/bin/sh', '-c', 'blocks=$(grep -o "unsafe {" /workspace/submission.rs | wc -l); notes=$(grep -o "SAFETY:" /workspace/submission.rs | wc -l); if [ "$notes" -lt "$blocks" ]; then echo "Every unsafe block needs a // SAFETY: comment explaining why it is sound ($blocks unsafe blocks, $notes SAFETY comments)."; exit 1; fi; rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-atomics-locks':
      return {
        files: { 'main.rs': `${code}

fn main() {
    std::thread::spawn(|| {
        std::thread::sleep(std::time::Duration::from_secs(6));
        eprintln!("The test timed out: something is waiting forever (a deadlock, or a signal that is never checked?).");
        std::process::exit(1);
    });
    let counter = Counter::new();
    std::thread::scope(|scope| {
        for _ in 0..8 {
            scope.spawn(|| {
                for _ in 0..10_000 {
                    counter.increment();
                }
            });
        }
    });
    assert_eq!(counter.get(), 80_000, "no increment may be lost when 8 threads count at once");
    for round in 0..50 {
        let flag = OnceFlag::new();
        let barrier = std::sync::Barrier::new(16);
        let winners = std::sync::atomic::AtomicUsize::new(0);
        std::thread::scope(|scope| {
            for _ in 0..16 {
                scope.spawn(|| {
                    barrier.wait();
                    if flag.try_claim() {
                        winners.fetch_add(1, std::sync::atomic::Ordering::Relaxed);
                    }
                });
            }
        });
        assert_eq!(winners.into_inner(), 1, "exactly one of 16 threads may claim the flag (round {round})");
    }
    let alice = std::sync::Mutex::new(1_000i64);
    let bob = std::sync::Mutex::new(1_000i64);
    std::thread::scope(|scope| {
        scope.spawn(|| for _ in 0..20_000 { transfer(&alice, &bob, 1) });
        scope.spawn(|| for _ in 0..20_000 { transfer(&bob, &alice, 1) });
    });
    assert_eq!((*alice.lock().unwrap(), *bob.lock().unwrap()), (1_000, 1_000), "opposite transfers cancel out, and no money is created or lost");
    transfer(&alice, &alice, 50);
    assert_eq!(*alice.lock().unwrap(), 1_000, "a transfer to the same account changes nothing (and must not deadlock)");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-macros':
      return {
        files: { 'main.rs': `${code}

fn main() {
    assert_eq!(square!(7), 49, "square of a literal");
    assert_eq!(square!(2 + 3), 25, "square of an expression is (2 + 3) squared, not 2 + 3 * 2 + 3");
    let mut calls = 0;
    let mut next = || { calls += 1; 4 };
    assert_eq!(square!(next()), 16, "square of a function call");
    assert_eq!(calls, 1, "the expression passed to square! must be evaluated exactly once");
    let ages: std::collections::HashMap<&str, u32> = hashmap! { "ada" => 36, "grace" => 85 };
    assert_eq!((ages.len(), ages["ada"], ages["grace"]), (2, 36, 85), "hashmap! builds a map");
    let trailing: std::collections::HashMap<u8, char> = hashmap! { 1 => 'a', 2 => 'b', };
    assert_eq!(trailing.len(), 2, "a trailing comma is accepted");
    let empty: std::collections::HashMap<u8, u8> = hashmap! {};
    assert!(empty.is_empty(), "hashmap! with nothing inside is an empty map");
    assert_eq!(max_of!(3), 3, "max_of one value");
    assert_eq!(max_of!(3, 9, 2), 9, "max_of three values");
    assert_eq!(max_of!(-5, -1, -9, -3, -2,), -1, "max_of any number of values, with a trailing comma");
    assert_eq!(max_of!("pear", "apple"), "pear", "max_of works for any comparable type");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-structured-concurrency':
      return {
        files: { 'main.rs': `${code}

async fn job(id: u32, done: std::sync::Arc<std::sync::atomic::AtomicUsize>) -> Result<u32, String> {
    tokio::time::sleep(std::time::Duration::from_millis(u64::from(id) * 10)).await;
    if id == 3 {
        return Err(String::from("job 3 failed"));
    }
    done.fetch_add(1, std::sync::atomic::Ordering::SeqCst);
    Ok(id * 2)
}

#[tokio::main]
async fn main() {
    std::thread::spawn(|| {
        std::thread::sleep(std::time::Duration::from_secs(6));
        eprintln!("The test timed out: something is waiting forever (a deadlock, or a signal that is never checked?).");
        std::process::exit(1);
    });
    use std::sync::Arc;
    use std::sync::atomic::{AtomicUsize, Ordering};
    use std::time::{Duration, Instant};
    let done = Arc::new(AtomicUsize::new(0));
    assert_eq!(run_all(vec![5, 1, 4], Arc::clone(&done)).await, Ok(vec![10, 2, 8]), "results come back in input order");
    let done = Arc::new(AtomicUsize::new(0));
    let started = Instant::now();
    let result = run_all(vec![1, 2, 3, 50, 60], Arc::clone(&done)).await;
    assert_eq!(result, Err(String::from("job 3 failed")), "the first error is returned");
    assert!(started.elapsed() < Duration::from_millis(300), "run_all returns as soon as a job fails, without waiting for slow jobs");
    tokio::time::sleep(Duration::from_millis(800)).await;
    assert_eq!(done.load(Ordering::SeqCst), 2, "jobs still running when one failed are cancelled, not left running in the background");

    let (job_sender, jobs) = tokio::sync::mpsc::channel(8);
    let (stop, shutdown) = tokio::sync::watch::channel(false);
    let handle = tokio::spawn(worker(jobs, shutdown));
    for value in [1, 2, 3] {
        job_sender.send(value).await.unwrap();
    }
    tokio::time::sleep(Duration::from_millis(50)).await;
    stop.send(true).unwrap();
    let total = tokio::time::timeout(Duration::from_secs(1), handle).await.expect("the worker must stop promptly when shutdown is signalled, even though its job channel is still open").unwrap();
    assert_eq!(total, 6, "the worker finished the jobs it received before shutdown");
    drop(job_sender);

    let (job_sender, jobs) = tokio::sync::mpsc::channel(8);
    let (_stop, shutdown) = tokio::sync::watch::channel(false);
    let handle = tokio::spawn(worker(jobs, shutdown));
    job_sender.send(10).await.unwrap();
    drop(job_sender);
    assert_eq!(tokio::time::timeout(Duration::from_secs(1), handle).await.expect("the worker stops when every sender is gone").unwrap(), 10, "closing the channel ends the worker");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'pathway-rustc /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-testing-properties':
      return {
        files: { 'main.rs': `${code}

struct Generator(u64);

impl Generator {
    fn next(&mut self) -> u64 {
        self.0 = self.0.wrapping_mul(6_364_136_223_846_793_005).wrapping_add(1_442_695_040_888_963_407);
        self.0 >> 33
    }

    fn bytes(&mut self) -> Vec<u8> {
        let length = (self.next() % 700) as usize;
        let alphabet = 1 + self.next() % 4;
        (0..length).map(|_| if self.next() % 8 == 0 { 0 } else { (self.next() % alphabet) as u8 + b'a' }).collect()
    }
}

fn main() {
    assert_eq!(encode(b""), Vec::<u8>::new(), "nothing encodes to nothing");
    assert_eq!(encode(b"aaab"), vec![3, b'a', 1, b'b'], "runs become (count, byte) pairs");
    assert_eq!(encode(&[7u8; 300]), vec![255, 7, 45, 7], "a run longer than 255 is split, never wrapped to a count of 0");
    assert_eq!(decode(&[2, b'x', 1, b'y']), Ok(b"xxy".to_vec()), "decode expands pairs");
    assert_eq!(decode(&[2, b'x', 1]), Err(DecodeError::OddLength), "an odd number of bytes is OddLength");
    assert_eq!(decode(&[0, b'x']), Err(DecodeError::ZeroCount), "a count of zero is ZeroCount");
    let mut generator = Generator(42);
    for case in 0..2_000 {
        let data = generator.bytes();
        let encoded = encode(&data);
        assert_eq!(decode(&encoded).as_deref(), Ok(&data[..]), "property: decode(encode(x)) == x failed on case {case} with {} bytes", data.len());
        assert!(encoded.len() <= data.len() * 2, "property: encoding at most doubles the size (case {case})");
        assert!(encoded.chunks(2).all(|pair| pair[0] > 0), "property: no pair has a count of zero (case {case})");
        assert!(encoded.windows(4).step_by(2).all(|w| w[1] != w[3] || w[0] == 255), "property: neighbouring pairs for the same byte only when the first is full (case {case})");
    }
    std::panic::set_hook(Box::new(|_| {}));
    for case in 0..2_000 {
        let junk: Vec<u8> = (0..generator.next() % 40).map(|_| generator.next() as u8).collect();
        assert!(std::panic::catch_unwind(|| decode(&junk)).is_ok(), "property: decode never panics (case {case}, input {junk:?})");
    }
    let _ = std::panic::take_hook();
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-systems-wasm-embedded':
      return {
        files: {
          'submission.rs': code,
          'main.rs': `${code}

fn main() {
    let mut readings: RingBuffer<4> = RingBuffer::new();
    assert_eq!((readings.len(), readings.average()), (0, None), "a new buffer is empty");
    assert_eq!(readings.push(10), None, "pushing into free space evicts nothing");
    assert_eq!(readings.push(20), None, "second reading");
    assert_eq!(readings.average(), Some(15), "average of the readings held");
    readings.push(30);
    readings.push(40);
    assert_eq!(readings.len(), 4, "the buffer is full");
    assert_eq!(readings.push(50), Some(10), "a full buffer evicts its oldest reading");
    assert_eq!(readings.push(60), Some(20), "and then the next oldest");
    assert_eq!((readings.len(), readings.average()), (4, Some(45)), "the four newest readings remain: 30, 40, 50, 60");
    let mut high: RingBuffer<4> = RingBuffer::new();
    for _ in 0..4 {
        high.push(60_000);
    }
    assert_eq!(high.average(), Some(60_000), "the average must not overflow while summing");
    const STARTUP: RingBuffer<8> = RingBuffer::new();
    assert_eq!(STARTUP.len(), 0, "new() is a const fn, usable in a const or static");
    assert_eq!(fletcher16(b""), 0, "Fletcher-16 of nothing");
    assert_eq!(fletcher16(b"abcde"), 0xC8F0, "Fletcher-16 of abcde");
    assert_eq!(fletcher16(b"abcdef"), 0x2057, "Fletcher-16 of abcdef");
    assert_eq!(fletcher16(b"abcdefgh"), 0x0627, "Fletcher-16 of abcdefgh");
    assert_eq!(fletcher16(&[0xff; 2_000]), fletcher16(&[0xff; 2_000]), "long inputs don't overflow");
    println!("PATHWAY_TEST_PASS");
}
`,
        },
        // First compile the submission alone as a #![no_std] library: it must need only `core`.
        command: ['/bin/sh', '-c', 'cd /workspace && { printf "#![no_std]\\n"; cat submission.rs; } > nostd.rs && if ! rustc --edition=2024 --crate-type lib nostd.rs -o libnostd.rlib 2> nostd.err; then echo "Your code must compile without the standard library (#![no_std]): use only core, with no Vec, String, Box or std:: paths."; head -n 20 nostd.err; exit 1; fi && rustc --edition=2024 main.rs -o main && ./main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-systems-capstone':
      return {
        files: { 'main.rs': `${code}

fn main() {
    use std::path::Path;
    let dir = Path::new("pathway-store");
    let _ = std::fs::remove_dir_all(dir);
    std::fs::create_dir_all(dir).unwrap();

    let format = dir.join("format.log");
    let mut store = Store::open(&format).unwrap();
    store.set("k", "v").unwrap();
    drop(store);
    assert_eq!(std::fs::read(&format).unwrap(), vec![1, 0, 0, 0, 1, 0, 0, 0, 1, b'k', b'v'], "a SET record is op 1, key length and value length as big-endian u32, then key and value");

    let log = dir.join("data.log");
    let mut store = Store::open(&log).unwrap();
    assert_eq!(store.len(), 0, "a new store is empty");
    store.set("a", "1").unwrap();
    store.set("b", "2").unwrap();
    store.set("a", "3").unwrap();
    assert_eq!(store.delete("b").unwrap(), true, "deleting an existing key returns true");
    assert_eq!(store.delete("missing").unwrap(), false, "deleting a missing key returns false");
    assert_eq!((store.get("a"), store.get("b"), store.len()), (Some("3"), None, 1), "the latest value wins and deleted keys are gone");
    drop(store);

    let store = Store::open(&log).unwrap();
    assert_eq!((store.get("a"), store.get("b"), store.len()), (Some("3"), None, 1), "reopening replays the log to the same state");
    drop(store);

    let mut torn = std::fs::read(&log).unwrap();
    let valid_length = torn.len();
    torn.extend_from_slice(&[1, 0, 0, 0, 9, 0]);
    std::fs::write(&log, &torn).unwrap();
    let mut store = Store::open(&log).expect("a record cut off by a crash is ignored, not treated as corruption");
    assert_eq!((store.get("a"), store.len()), (Some("3"), 1), "the torn record is ignored");
    store.set("c", "4").unwrap();
    drop(store);
    let store = Store::open(&log).unwrap();
    assert_eq!(store.get("c"), Some("4"), "records written after recovering from a crash are readable: cut the torn tail off first");
    assert!(std::fs::metadata(&log).unwrap().len() as usize > valid_length, "the new record was appended");
    drop(store);

    let mut store = Store::open(&log).unwrap();
    for round in 0..50 {
        store.set("counter", &round.to_string()).unwrap();
    }
    let before = std::fs::metadata(&log).unwrap().len();
    store.compact().unwrap();
    let after = std::fs::metadata(&log).unwrap().len();
    assert!(after < before, "compaction rewrites only live keys ({before} -> {after} bytes)");
    store.set("d", "5").unwrap();
    drop(store);
    let names: Vec<String> = std::fs::read_dir(dir).unwrap().map(|e| e.unwrap().file_name().into_string().unwrap()).filter(|n| n.starts_with("data")).collect();
    assert_eq!(names, ["data.log"], "compaction leaves no temporary file behind");
    let store = Store::open(&log).unwrap();
    assert_eq!((store.get("a"), store.get("c"), store.get("counter"), store.get("d"), store.len()), (Some("3"), Some("4"), Some("49"), Some("5"), 4), "after compaction and another write, every live key survives a reopen");
    drop(store);

    let corrupt = dir.join("corrupt.log");
    std::fs::write(&corrupt, [9, 0, 0, 0, 1, 0, 0, 0, 1, b'k', b'v']).unwrap();
    let error = Store::open(&corrupt).err().expect("an unknown record type is corruption, not a crash tail");
    assert_eq!(error.kind(), std::io::ErrorKind::InvalidData, "corruption is reported as InvalidData");
    std::fs::remove_dir_all(dir).unwrap();
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'cd /workspace && rustc --edition=2024 main.rs -o main && ./main'],
        tests: 5,
        runtime: 'rust',
      }
    case 'rust-web-axum':
      return {
        files: { 'main.rs': `${code}

#[derive(Clone, serde::Serialize)]
struct Course {
    id: String,
    title: String,
}

fn catalog() -> Vec<Course> {
    vec![
        Course { id: String::from("rust"), title: String::from("Rust Systems") },
        Course { id: String::from("sql"), title: String::from("SQLite") },
    ]
}

async fn call(app: &axum::Router, method: &str, uri: &str, headers: &[(&str, &str)], body: &str) -> (u16, axum::http::HeaderMap, String) {
    use http_body_util::BodyExt;
    use tower::ServiceExt;
    let mut request = axum::http::Request::builder().method(method).uri(uri);
    for (name, value) in headers {
        request = request.header(*name, *value);
    }
    let response = app.clone().oneshot(request.body(axum::body::Body::from(body.to_string())).unwrap()).await.unwrap();
    let status = response.status().as_u16();
    let headers = response.headers().clone();
    let bytes = response.into_body().collect().await.unwrap().to_bytes();
    (status, headers, String::from_utf8(bytes.to_vec()).unwrap())
}

fn json(text: &str) -> serde_json::Value {
    serde_json::from_str(text).unwrap_or_else(|_| panic!("expected a JSON body, got {text:?}"))
}

const JSON: (&str, &str) = ("content-type", "application/json");

#[tokio::main(flavor = "current_thread")]
async fn main() {
    let app = app();
    let (status, _, body) = call(&app, "GET", "/health", &[], "").await;
    assert_eq!((status, body.as_str()), (200, "ok"), "GET /health returns 200 with the text ok");
    let (status, headers, body) = call(&app, "GET", "/courses/rust", &[], "").await;
    assert_eq!(status, 200, "a known course is 200");
    assert_eq!(headers.get("content-type").map(|v| v.to_str().unwrap()), Some("application/json"), "courses are returned as JSON");
    assert_eq!(json(&body), serde_json::json!({"id": "rust", "title": "Rust Systems"}), "the course is serialized from the catalog");
    let (status, _, _) = call(&app, "GET", "/courses/cobol", &[], "").await;
    assert_eq!(status, 404, "an unknown course is 404 Not Found");
    let (status, _, body) = call(&app, "POST", "/echo", &[JSON], r#"{"hello": [1, 2, 3]}"#).await;
    assert_eq!((status, json(&body)), (200, serde_json::json!({"hello": [1, 2, 3]})), "POST /echo returns the JSON it received");
    let (status, _, _) = call(&app, "POST", "/echo", &[], r#"{"hello": 1}"#).await;
    assert_eq!(status, 415, "a JSON endpoint rejects a body without a JSON content type (415)");
    let (status, _, _) = call(&app, "POST", "/echo", &[JSON], "{not json").await;
    assert_eq!(status, 400, "malformed JSON is 400");
    let (status, _, _) = call(&app, "POST", "/health", &[], "").await;
    assert_eq!(status, 405, "the wrong method on a known path is 405");
    let (status, _, _) = call(&app, "GET", "/nowhere", &[], "").await;
    assert_eq!(status, 404, "an unknown path is 404");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'pathway-rustc /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-api-contracts':
      return {
        files: { 'main.rs': `${code}

async fn call(app: &axum::Router, method: &str, uri: &str, headers: &[(&str, &str)], body: &str) -> (u16, axum::http::HeaderMap, String) {
    use http_body_util::BodyExt;
    use tower::ServiceExt;
    let mut request = axum::http::Request::builder().method(method).uri(uri);
    for (name, value) in headers {
        request = request.header(*name, *value);
    }
    let response = app.clone().oneshot(request.body(axum::body::Body::from(body.to_string())).unwrap()).await.unwrap();
    let status = response.status().as_u16();
    let headers = response.headers().clone();
    let bytes = response.into_body().collect().await.unwrap().to_bytes();
    (status, headers, String::from_utf8(bytes.to_vec()).unwrap())
}

fn json(text: &str) -> serde_json::Value {
    serde_json::from_str(text).unwrap_or_else(|_| panic!("expected a JSON body, got {text:?}"))
}

const JSON: (&str, &str) = ("content-type", "application/json");

#[tokio::main(flavor = "current_thread")]
async fn main() {
    let app = app(std::sync::Arc::new(std::sync::Mutex::new(Store::default())));
    let new = r#"{"learner": "ada", "course": "rust"}"#;
    let (status, headers, body) = call(&app, "POST", "/v1/enrollments", &[JSON, ("idempotency-key", "k-1")], new).await;
    assert_eq!(status, 201, "creating an enrollment is 201 Created");
    assert_eq!(headers.get("location").map(|v| v.to_str().unwrap()), Some("/v1/enrollments/1"), "201 responses carry a Location header");
    assert_eq!(json(&body), serde_json::json!({"id": 1, "learner": "ada", "course": "rust"}), "the created enrollment is returned");
    let (status, headers, body) = call(&app, "POST", "/v1/enrollments", &[JSON, ("idempotency-key", "k-1")], new).await;
    assert_eq!((status, json(&body)["id"].clone()), (201, serde_json::json!(1)), "retrying with the same Idempotency-Key returns the original result");
    assert_eq!(headers.get("location").map(|v| v.to_str().unwrap()), Some("/v1/enrollments/1"), "the retry points at the same resource");
    let (status, headers, body) = call(&app, "POST", "/v1/enrollments", &[JSON], new).await;
    assert_eq!(status, 400, "a missing Idempotency-Key is 400");
    assert_eq!(headers.get("content-type").map(|v| v.to_str().unwrap()), Some("application/problem+json"), "errors use application/problem+json");
    assert_eq!(json(&body)["status"], serde_json::json!(400), "the problem document repeats the status");
    let (status, _, body) = call(&app, "POST", "/v1/enrollments", &[JSON, ("idempotency-key", "k-2")], r#"{"learner": " ", "course": "rust"}"#).await;
    assert_eq!((status, json(&body)["status"].clone()), (422, serde_json::json!(422)), "a blank learner is 422 with a problem document");
    for n in 2..=60 {
        let body = format!(r#"{{"learner": "l{n}", "course": "rust"}}"#);
        let key = format!("key-{n}");
        call(&app, "POST", "/v1/enrollments", &[JSON, ("idempotency-key", &key)], &body).await;
    }
    let (status, _, body) = call(&app, "GET", "/v1/enrollments?limit=2", &[], "").await;
    let page = json(&body);
    assert_eq!((status, page["items"].as_array().unwrap().len(), page["next"].clone()), (200, 2, serde_json::json!(2)), "limit=2 returns two items and a cursor to continue after");
    let (_, _, body) = call(&app, "GET", "/v1/enrollments?limit=2&after=2", &[], "").await;
    assert_eq!(json(&body)["items"][0]["id"], serde_json::json!(3), "after=2 continues from the next item");
    let (_, _, body) = call(&app, "GET", "/v1/enrollments", &[], "").await;
    assert_eq!(json(&body)["items"].as_array().unwrap().len(), 20, "the default page size is 20");
    let (_, _, body) = call(&app, "GET", "/v1/enrollments?limit=1000", &[], "").await;
    assert_eq!(json(&body)["items"].as_array().unwrap().len(), 50, "page sizes are capped at 50 however many are asked for");
    let (_, _, body) = call(&app, "GET", "/v1/enrollments?limit=50&after=50", &[], "").await;
    assert_eq!((json(&body)["items"].as_array().unwrap().len(), json(&body)["next"].clone()), (10, serde_json::Value::Null), "the last page has no next cursor");
    let (status, _, _) = call(&app, "GET", "/v1/enrollments?limit=0", &[], "").await;
    assert_eq!(status, 422, "limit=0 is rejected");
    let (status, _, body) = call(&app, "GET", "/v1/enrollments/1", &[], "").await;
    assert_eq!((status, json(&body)["learner"].clone()), (200, serde_json::json!("ada")), "an enrollment can be fetched by id");
    let (status, headers, _) = call(&app, "GET", "/v1/enrollments/999", &[], "").await;
    assert_eq!((status, headers.get("content-type").map(|v| v.to_str().unwrap().to_string())), (404, Some(String::from("application/problem+json"))), "an unknown id is 404 with a problem document");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'pathway-rustc /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-extractors-errors':
      return {
        files: { 'main.rs': `${code}

async fn call(app: &axum::Router, method: &str, uri: &str, headers: &[(&str, &str)], body: &str) -> (u16, axum::http::HeaderMap, String) {
    use http_body_util::BodyExt;
    use tower::ServiceExt;
    let mut request = axum::http::Request::builder().method(method).uri(uri);
    for (name, value) in headers {
        request = request.header(*name, *value);
    }
    let response = app.clone().oneshot(request.body(axum::body::Body::from(body.to_string())).unwrap()).await.unwrap();
    let status = response.status().as_u16();
    let headers = response.headers().clone();
    let bytes = response.into_body().collect().await.unwrap().to_bytes();
    (status, headers, String::from_utf8(bytes.to_vec()).unwrap())
}

fn json(text: &str) -> serde_json::Value {
    serde_json::from_str(text).unwrap_or_else(|_| panic!("expected a JSON body, got {text:?}"))
}

const JSON: (&str, &str) = ("content-type", "application/json");

#[tokio::main(flavor = "current_thread")]
async fn main() {
    assert_eq!(Email::try_from(String::from("  Ada@Example.COM ")), Ok(Email(String::from("ada@example.com"))), "emails are trimmed and lowercased");
    for bad in ["", "ada", "@example.com", "ada@", "ada@@example.com", "ada@example", "ada@.com"] {
        assert!(matches!(Email::try_from(bad.to_string()), Err(ApiError::Validation(_))), "{bad:?} is not a valid email");
    }
    let app = app(std::sync::Arc::new(std::sync::Mutex::new(Vec::new())));
    let (status, _, body) = call(&app, "POST", "/users", &[JSON], r#"{"email": "Ada@Example.com"}"#).await;
    assert_eq!((status, json(&body)), (201, serde_json::json!({"id": 1, "email": "ada@example.com"})), "a valid user is created");
    let (status, headers, body) = call(&app, "POST", "/users", &[JSON], r#"{"email": "ada@example.com"}"#).await;
    assert_eq!(status, 409, "a duplicate email is 409 Conflict");
    assert_eq!(headers.get("content-type").map(|v| v.to_str().unwrap()), Some("application/problem+json"), "conflicts are problem documents");
    assert_eq!(json(&body)["status"], serde_json::json!(409), "the problem document repeats the status");
    let (status, _, body) = call(&app, "POST", "/users", &[JSON], r#"{"email": "not-an-email"}"#).await;
    assert_eq!((status, json(&body)["status"].clone()), (422, serde_json::json!(422)), "an invalid email is 422");
    let (status, headers, body) = call(&app, "POST", "/users", &[JSON], "{broken").await;
    assert_eq!(status, 400, "malformed JSON is 400");
    assert_eq!(headers.get("content-type").map(|v| v.to_str().unwrap()), Some("application/problem+json"), "even extractor failures are returned as problem documents");
    assert!(json(&body)["detail"].as_str().is_some_and(|detail| !detail.is_empty()), "the problem detail explains the rejection");
    let (status, headers, _) = call(&app, "GET", "/users/abc", &[], "").await;
    assert_eq!((status, headers.get("content-type").map(|v| v.to_str().unwrap().to_string())), (400, Some(String::from("application/problem+json"))), "a non-numeric id is 400 as a problem document");
    let (status, _, body) = call(&app, "GET", "/users/1", &[], "").await;
    assert_eq!((status, json(&body)["email"].clone()), (200, serde_json::json!("ada@example.com")), "users can be fetched");
    let (status, _, body) = call(&app, "GET", "/users/42", &[], "").await;
    assert_eq!((status, json(&body)["status"].clone()), (404, serde_json::json!(404)), "a missing user is 404");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'pathway-rustc /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-database-transactions':
      return {
        files: { 'main.rs': `${code}

fn count(conn: &rusqlite::Connection, sql: &str) -> i64 {
    conn.query_row(sql, [], |row| row.get(0)).unwrap()
}

fn main() {
    let mut conn = rusqlite::Connection::open_in_memory().unwrap();
    migrate(&conn).unwrap();
    migrate(&conn).expect("running migrations twice is safe");
    assert_eq!(count(&conn, "PRAGMA user_version"), 2, "migrations record the schema version");
    let ada = open_account(&conn, "ada", 100).unwrap();
    let grace = open_account(&conn, "grace", 50).unwrap();
    assert_ne!(ada, grace, "each account gets its own id");
    assert!(open_account(&conn, "ada", 5).is_err(), "owners are unique");
    assert!(open_account(&conn, "debt", -1).is_err(), "the schema refuses negative balances");
    let tricky = "o'brien'); DROP TABLE ledger; --";
    open_account(&conn, tricky, 7).expect("names are passed as parameters, so quotes are just data");
    assert_eq!(balance(&conn, tricky).unwrap(), Some(7), "the tricky name is stored exactly");
    assert_eq!(count(&conn, "SELECT count(*) FROM sqlite_master WHERE name = 'ledger'"), 1, "no SQL from data was executed");
    assert_eq!(balance(&conn, "nobody").unwrap(), None, "a missing owner has no balance");

    assert_eq!(transfer(&mut conn, "ada", "grace", 30), Ok(()), "a valid transfer succeeds");
    assert_eq!((balance(&conn, "ada").unwrap(), balance(&conn, "grace").unwrap()), (Some(70), Some(80)), "both balances change");
    assert_eq!(count(&conn, "SELECT count(*) FROM ledger"), 1, "the transfer is recorded in the ledger");
    assert_eq!(transfer(&mut conn, "ada", "grace", 500), Err(TransferError::InsufficientFunds), "overdrawing is refused");
    assert_eq!(transfer(&mut conn, "ada", "nobody", 10), Err(TransferError::NoSuchAccount), "an unknown recipient is refused");
    assert_eq!(transfer(&mut conn, "nobody", "ada", 10), Err(TransferError::NoSuchAccount), "an unknown sender is refused");
    assert_eq!(transfer(&mut conn, "ada", "grace", 0), Err(TransferError::InvalidAmount), "zero is not a transfer");
    assert_eq!(transfer(&mut conn, "ada", "grace", -5), Err(TransferError::InvalidAmount), "negative amounts are not transfers");
    assert_eq!((balance(&conn, "ada").unwrap(), balance(&conn, "grace").unwrap()), (Some(70), Some(80)), "failed transfers change nothing: all or nothing");
    assert_eq!(count(&conn, "SELECT count(*) FROM ledger"), 1, "failed transfers write no ledger rows");
    assert_eq!(count(&conn, "SELECT sum(balance) FROM accounts"), 157, "money is never created or destroyed");
    println!("PATHWAY_TEST_PASS");
}
` },
        // pathway-rustc links the image's prebuilt rusqlite (with SQLite compiled in).
        command: ['/bin/sh', '-c', 'pathway-rustc /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-auth-security':
      return {
        files: { 'main.rs': `${code}

#[derive(Clone)]
struct Principal {
    user: String,
    admin: bool,
}

#[derive(Clone, serde::Serialize)]
struct Note {
    id: u64,
    owner: String,
    text: String,
}

struct AppState {
    tokens: std::collections::HashMap<String, Principal>,
    notes: std::sync::Mutex<std::collections::HashMap<u64, Note>>,
}

async fn call(app: &axum::Router, method: &str, uri: &str, headers: &[(&str, &str)], body: &str) -> (u16, axum::http::HeaderMap, String) {
    use http_body_util::BodyExt;
    use tower::ServiceExt;
    let mut request = axum::http::Request::builder().method(method).uri(uri);
    for (name, value) in headers {
        request = request.header(*name, *value);
    }
    let response = app.clone().oneshot(request.body(axum::body::Body::from(body.to_string())).unwrap()).await.unwrap();
    let status = response.status().as_u16();
    let headers = response.headers().clone();
    let bytes = response.into_body().collect().await.unwrap().to_bytes();
    (status, headers, String::from_utf8(bytes.to_vec()).unwrap())
}

fn json(text: &str) -> serde_json::Value {
    serde_json::from_str(text).unwrap_or_else(|_| panic!("expected a JSON body, got {text:?}"))
}

const JSON: (&str, &str) = ("content-type", "application/json");

#[tokio::main(flavor = "current_thread")]
async fn main() {
    let tokens = [("t-ada", "ada", false), ("t-lin", "lin", false), ("t-root", "root", true)]
        .into_iter()
        .map(|(token, user, admin)| (token.to_string(), Principal { user: user.to_string(), admin }))
        .collect();
    let notes = [(1, "ada", "ada's plan"), (2, "lin", "lin's draft")]
        .into_iter()
        .map(|(id, owner, text)| (id, Note { id, owner: owner.to_string(), text: text.to_string() }))
        .collect();
    let app = app(std::sync::Arc::new(AppState { tokens, notes: std::sync::Mutex::new(notes) }));
    let bearer = |token: &'static str| ("authorization", match token { "t-ada" => "Bearer t-ada", "t-lin" => "Bearer t-lin", "t-root" => "Bearer t-root", _ => "Bearer forged" });

    let (status, headers, _) = call(&app, "GET", "/notes/1", &[], "").await;
    assert_eq!(status, 401, "no credentials is 401 Unauthorized");
    assert_eq!(headers.get("www-authenticate").map(|v| v.to_str().unwrap()), Some("Bearer"), "a 401 says which scheme to use");
    let (status, headers, _) = call(&app, "GET", "/notes/1", &[bearer("bad")], "").await;
    assert_eq!((status, headers.contains_key("www-authenticate")), (401, true), "an unknown token is 401, not 403");
    let (status, _, _) = call(&app, "GET", "/notes/1", &[("authorization", "Basic YWRhOnB3")], "").await;
    assert_eq!(status, 401, "a different scheme is not accepted");
    let (status, _, body) = call(&app, "GET", "/notes/1", &[bearer("t-ada")], "").await;
    assert_eq!((status, json(&body)["text"].clone()), (200, serde_json::json!("ada's plan")), "owners read their notes");
    let (status, _, _) = call(&app, "GET", "/notes/1", &[bearer("t-lin")], "").await;
    assert_eq!(status, 404, "another user's note is 404, so its existence isn't revealed");
    let (status, _, _) = call(&app, "GET", "/notes/2", &[bearer("t-root")], "").await;
    assert_eq!(status, 200, "an admin can read any note");
    let (status, _, _) = call(&app, "DELETE", "/notes/2", &[bearer("t-root")], "").await;
    assert_eq!(status, 403, "an admin who can see a note but may not delete it gets 403");
    let (status, _, _) = call(&app, "DELETE", "/notes/2", &[bearer("t-ada")], "").await;
    assert_eq!(status, 404, "a user who can't see the note gets 404 on delete too");
    let (status, _, _) = call(&app, "DELETE", "/notes/2", &[], "").await;
    assert_eq!(status, 401, "deleting without credentials is 401");
    let (status, _, _) = call(&app, "DELETE", "/notes/2", &[bearer("t-lin")], "").await;
    assert_eq!(status, 204, "owners delete their own notes");
    let (status, _, _) = call(&app, "GET", "/notes/2", &[bearer("t-lin")], "").await;
    assert_eq!(status, 404, "a deleted note is gone");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'pathway-rustc /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-testing-integration':
      return {
        files: { 'main.rs': `${code}

trait Inventory {
    fn reserve(&mut self, sku: &str, quantity: u32) -> bool;
    fn release(&mut self, sku: &str, quantity: u32);
}

trait Payments {
    fn charge(&mut self, cents: u64) -> Result<String, String>;
}

trait Clock {
    fn now(&self) -> u64;
}

struct Line {
    sku: String,
    quantity: u32,
    unit_cents: u64,
}

#[derive(Default)]
struct FakeInventory {
    stock: std::collections::HashMap<String, u32>,
    log: Vec<String>,
}

impl Inventory for FakeInventory {
    fn reserve(&mut self, sku: &str, quantity: u32) -> bool {
        self.log.push(format!("reserve {sku} {quantity}"));
        match self.stock.get_mut(sku) {
            Some(available) if *available >= quantity => {
                *available -= quantity;
                true
            }
            _ => false,
        }
    }
    fn release(&mut self, sku: &str, quantity: u32) {
        self.log.push(format!("release {sku} {quantity}"));
        *self.stock.entry(sku.to_string()).or_insert(0) += quantity;
    }
}

struct FakePayments {
    decline: bool,
    charged: Vec<u64>,
}

impl Payments for FakePayments {
    fn charge(&mut self, cents: u64) -> Result<String, String> {
        self.charged.push(cents);
        if self.decline { Err(String::from("card declined")) } else { Ok(format!("pay-{}", self.charged.len())) }
    }
}

struct FixedClock(u64);

impl Clock for FixedClock {
    fn now(&self) -> u64 {
        self.0
    }
}

fn line(sku: &str, quantity: u32, unit_cents: u64) -> Line {
    Line { sku: sku.to_string(), quantity, unit_cents }
}

fn inventory(items: &[(&str, u32)]) -> FakeInventory {
    FakeInventory { stock: items.iter().map(|(sku, n)| (sku.to_string(), *n)).collect(), log: Vec::new() }
}

fn main() {
    let clock = FixedClock(1_700_000_000);
    let mut stock = inventory(&[("mug", 5), ("tee", 2)]);
    let mut payments = FakePayments { decline: false, charged: Vec::new() };
    let receipt = checkout(&[line("mug", 2, 1_250), line("tee", 1, 2_000)], &mut stock, &mut payments, &clock);
    assert_eq!(receipt, Ok(Receipt { payment_id: String::from("pay-1"), total_cents: 4_500, at: 1_700_000_000 }), "a successful checkout charges the total and stamps the clock's time");
    assert_eq!((stock.stock["mug"], stock.stock["tee"]), (3, 1), "stock is reserved");

    let mut stock = inventory(&[("mug", 5)]);
    let mut payments = FakePayments { decline: false, charged: Vec::new() };
    assert_eq!(checkout(&[], &mut stock, &mut payments, &clock), Err(CheckoutError::EmptyCart), "an empty cart is refused");
    assert!(payments.charged.is_empty() && stock.log.is_empty(), "an empty cart touches nothing");

    let mut stock = inventory(&[("mug", 5), ("tee", 0)]);
    let mut payments = FakePayments { decline: false, charged: Vec::new() };
    let result = checkout(&[line("mug", 2, 1_250), line("tee", 1, 2_000)], &mut stock, &mut payments, &clock);
    assert_eq!(result, Err(CheckoutError::OutOfStock(String::from("tee"))), "a missing item is reported by sku");
    assert_eq!(stock.stock["mug"], 5, "items reserved before the shortage are released again");
    assert!(payments.charged.is_empty(), "nothing is charged when stock is short");

    let mut stock = inventory(&[("mug", 5)]);
    let mut payments = FakePayments { decline: true, charged: Vec::new() };
    let result = checkout(&[line("mug", 3, 1_000)], &mut stock, &mut payments, &clock);
    assert_eq!(result, Err(CheckoutError::PaymentDeclined(String::from("card declined"))), "a declined payment is reported with its reason");
    assert_eq!(stock.stock["mug"], 5, "a declined payment releases the reserved stock");
    assert_eq!(stock.log, ["reserve mug 3", "release mug 3"], "reserve, charge, then release on failure");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-observability':
      return {
        files: { 'main.rs': `${code}

async fn call(app: &axum::Router, method: &str, uri: &str, headers: &[(&str, &str)], body: &str) -> (u16, axum::http::HeaderMap, String) {
    use http_body_util::BodyExt;
    use tower::ServiceExt;
    let mut request = axum::http::Request::builder().method(method).uri(uri);
    for (name, value) in headers {
        request = request.header(*name, *value);
    }
    let response = app.clone().oneshot(request.body(axum::body::Body::from(body.to_string())).unwrap()).await.unwrap();
    let status = response.status().as_u16();
    let headers = response.headers().clone();
    let bytes = response.into_body().collect().await.unwrap().to_bytes();
    (status, headers, String::from_utf8(bytes.to_vec()).unwrap())
}

fn json(text: &str) -> serde_json::Value {
    serde_json::from_str(text).unwrap_or_else(|_| panic!("expected a JSON body, got {text:?}"))
}

const JSON: (&str, &str) = ("content-type", "application/json");

#[tokio::main(flavor = "current_thread")]
async fn main() {
    let metrics = std::sync::Arc::new(Metrics::default());
    let app = axum::Router::new()
        .route("/courses/{id}", axum::routing::get(|| async { "course" }))
        .route("/health", axum::routing::get(|| async { "ok" }))
        .layer(axum::middleware::from_fn_with_state(metrics.clone(), track));
    for uri in ["/courses/1", "/courses/2", "/courses/3", "/health", "/nowhere"] {
        call(&app, "GET", uri, &[], "").await;
    }
    let text = metrics.render();
    assert!(text.contains("http_requests_total{route=\\"/courses/{id}\\",status=\\"200\\"} 3\\n"), "requests are labelled by route template, not raw URL, so /courses/1, 2 and 3 share one series:\\n{text}");
    assert!(text.contains("http_requests_total{route=\\"/health\\",status=\\"200\\"} 1\\n"), "each route has its own series:\\n{text}");
    assert!(text.contains("route=\\"unmatched\\",status=\\"404\\"} 1"), "unknown paths are counted under one unmatched label:\\n{text}");
    assert!(!text.contains("/courses/1"), "raw ids must never become labels:\\n{text}");
    assert!(text.contains("http_request_duration_ms_count 5\\n"), "every request is timed:\\n{text}");

    let manual = Metrics::default();
    for duration in [3, 20, 20, 400, 9_000] {
        manual.record("/x", 200, duration);
    }
    let text = manual.render();
    for (bound, expected) in [("5", 1), ("25", 3), ("100", 3), ("500", 4), ("2500", 4), ("+Inf", 5)] {
        let line = format!("http_request_duration_ms_bucket{{le=\\"{bound}\\"}} {expected}\\n");
        assert!(text.contains(&line), "histogram buckets are cumulative: expected {line:?} in\\n{text}");
    }
    assert!(text.contains("http_request_duration_ms_sum 9443\\n") && text.contains("http_request_duration_ms_count 5\\n"), "sum and count are reported:\\n{text}");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'pathway-rustc /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-performance-profiling':
      return {
        files: { 'main.rs': `${code}

trait CourseDb {
    fn get(&self, id: u64) -> Option<String>;
    fn get_many(&self, ids: &[u64]) -> Vec<(u64, String)>;
}

struct CountingDb {
    queries: std::cell::Cell<usize>,
}

impl CourseDb for CountingDb {
    fn get(&self, id: u64) -> Option<String> {
        self.queries.set(self.queries.get() + 1);
        (id % 10 != 0).then(|| format!("course {id}"))
    }
    fn get_many(&self, ids: &[u64]) -> Vec<(u64, String)> {
        self.queries.set(self.queries.get() + 1);
        let mut rows: Vec<(u64, String)> = ids.iter().filter(|id| *id % 10 != 0).map(|id| (*id, format!("course {id}"))).collect();
        rows.reverse(); // a database returns rows in whatever order it likes
        rows
    }
}

fn main() {
    let mut samples: Vec<u64> = (1..=100).rev().collect();
    assert_eq!(percentile(&mut samples, 50.0), Some(50), "p50 of 1..=100");
    assert_eq!(percentile(&mut samples, 99.0), Some(99), "p99 of 1..=100");
    assert_eq!(percentile(&mut samples, 100.0), Some(100), "p100 is the maximum");
    let mut small = vec![120, 15, 30, 900, 45];
    assert_eq!(percentile(&mut small, 50.0), Some(45), "nearest rank: the median of five values is the third");
    assert_eq!(percentile(&mut small, 90.0), Some(900), "p90 of five values is the largest");
    assert_eq!(percentile(&mut small, 0.0), Some(15), "p0 is the minimum");
    assert_eq!(percentile(&mut [], 50.0), None, "no samples, no percentile");
    assert_eq!(percentile(&mut [1, 2], 150.0), None, "percentiles above 100 are invalid");
    assert_eq!(required_workers(200.0, 50.0), 10, "200 req/s at 50 ms each keeps 10 requests in flight");
    assert_eq!(required_workers(1_000.0, 120.0), 120, "Little's law");
    assert_eq!(required_workers(30.0, 25.0), 1, "a fraction of a worker rounds up");
    let db = CountingDb { queries: std::cell::Cell::new(0) };
    let ids: Vec<u64> = (1..=25).collect();
    let titles = load_titles(&ids, &db);
    assert_eq!(titles.len(), 23, "ids 10 and 20 don't exist and are skipped");
    assert_eq!((titles[0].as_str(), titles[22].as_str()), ("course 1", "course 25"), "titles come back in the requested order");
    assert_eq!(db.queries.get(), 1, "load_titles must use one batched query, not one per id (it made {} queries)", db.queries.get());
    let db = CountingDb { queries: std::cell::Cell::new(0) };
    assert!(load_titles(&[], &db).is_empty() && db.queries.get() == 0, "no ids, no query");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-resilience-queues':
      return {
        files: { 'main.rs': `${code}

fn main() {
    assert_eq!(backoff_ms(0, 100, 10_000, 1.0), 100, "attempt 0 waits up to the base delay");
    assert_eq!(backoff_ms(3, 100, 10_000, 1.0), 800, "the ceiling doubles each attempt");
    assert_eq!(backoff_ms(3, 100, 10_000, 0.5), 400, "full jitter scales the ceiling by a random fraction");
    assert_eq!(backoff_ms(10, 100, 10_000, 1.0), 10_000, "delays are capped");
    assert_eq!(backoff_ms(70, 100, 10_000, 1.0), 10_000, "huge attempt numbers don't overflow");
    assert_eq!(backoff_ms(60, 1_000, 30_000, 0.25), 7_500, "a shift that overflows u64 still saturates at the cap");

    let mut breaker = CircuitBreaker::new(3, 1_000);
    assert!(breaker.allow(0), "a closed breaker allows calls");
    breaker.record_failure(10);
    breaker.record_failure(20);
    assert_eq!(breaker.state(), BreakerState::Closed, "two failures are under the threshold");
    breaker.record_success();
    breaker.record_failure(30);
    breaker.record_failure(40);
    assert_eq!(breaker.state(), BreakerState::Closed, "a success resets the failure count");
    breaker.record_failure(50);
    assert_eq!(breaker.state(), BreakerState::Open, "three failures in a row open the breaker");
    assert!(!breaker.allow(500), "an open breaker fails fast during the cooldown");
    assert!(breaker.allow(1_050), "after the cooldown one trial call is allowed");
    assert_eq!(breaker.state(), BreakerState::HalfOpen, "the trial happens half-open");
    assert!(!breaker.allow(1_060), "only one trial call at a time");
    breaker.record_failure(1_070);
    assert_eq!(breaker.state(), BreakerState::Open, "a failed trial reopens immediately");
    assert!(!breaker.allow(1_500), "and restarts the cooldown");
    assert!(breaker.allow(2_100), "the next trial after a new cooldown");
    breaker.record_success();
    assert_eq!(breaker.state(), BreakerState::Closed, "a successful trial closes the breaker");

    let mut consumer = Consumer::new(3);
    let mut calls = 0;
    assert_eq!(consumer.deliver("job-1", &mut || { calls += 1; if calls < 2 { Err(String::from("timeout")) } else { Ok(()) } }), Outcome::Delivered { attempts: 2 }, "a transient failure is retried");
    assert_eq!(consumer.deliver("job-1", &mut || panic!("a duplicate must not run the handler")), Outcome::Duplicate, "a redelivered message is recognised by id and skipped");
    let mut attempts = 0;
    let outcome = consumer.deliver("job-2", &mut || { attempts += 1; Err(format!("bad payload {attempts}")) });
    assert_eq!(outcome, Outcome::DeadLettered { attempts: 3, last_error: String::from("bad payload 3") }, "after max attempts the message is dead-lettered with the last error");
    assert_eq!(consumer.dead_letters, ["job-2"], "the dead-letter queue keeps the message for a person to inspect");
    let mut ran = false;
    assert_eq!(consumer.deliver("job-2", &mut || { ran = true; Ok(()) }), Outcome::Delivered { attempts: 1 }, "a dead-lettered job can be replayed once fixed");
    assert!(ran, "the replay ran the handler");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 3,
        runtime: 'rust',
      }
    case 'rust-containers-delivery':
      return {
        files: { 'main.rs': `${code}

async fn call(app: &axum::Router, method: &str, uri: &str, headers: &[(&str, &str)], body: &str) -> (u16, axum::http::HeaderMap, String) {
    use http_body_util::BodyExt;
    use tower::ServiceExt;
    let mut request = axum::http::Request::builder().method(method).uri(uri);
    for (name, value) in headers {
        request = request.header(*name, *value);
    }
    let response = app.clone().oneshot(request.body(axum::body::Body::from(body.to_string())).unwrap()).await.unwrap();
    let status = response.status().as_u16();
    let headers = response.headers().clone();
    let bytes = response.into_body().collect().await.unwrap().to_bytes();
    (status, headers, String::from_utf8(bytes.to_vec()).unwrap())
}

fn json(text: &str) -> serde_json::Value {
    serde_json::from_str(text).unwrap_or_else(|_| panic!("expected a JSON body, got {text:?}"))
}

const JSON: (&str, &str) = ("content-type", "application/json");

fn env(pairs: &[(&str, &str)]) -> std::collections::HashMap<String, String> {
    pairs.iter().map(|(k, v)| (k.to_string(), v.to_string())).collect()
}

#[tokio::main]
async fn main() {
    std::thread::spawn(|| {
        std::thread::sleep(std::time::Duration::from_secs(6));
        eprintln!("The test timed out: the server never finished shutting down.");
        std::process::exit(1);
    });
    use std::sync::atomic::Ordering;
    let secret = "postgres://app:hunter2@db/prod";
    assert_eq!(load_config(&env(&[("PORT", "8080"), ("DATABASE_URL", secret)])), Ok(Config { port: 8080, database_url: secret.to_string(), log_level: String::from("info") }), "valid settings load, with LOG_LEVEL defaulting to info");
    let problems = load_config(&env(&[("PORT", "eighty"), ("LOG_LEVEL", "loud")])).expect_err("bad settings are refused");
    assert_eq!(problems.len(), 3, "every problem is reported at once, not just the first: {problems:?}");
    assert!(problems.iter().any(|p| p.contains("PORT")) && problems.iter().any(|p| p.contains("DATABASE_URL")) && problems.iter().any(|p| p.contains("LOG_LEVEL")), "each problem names its variable: {problems:?}");
    let problems = load_config(&env(&[("PORT", "0"), ("DATABASE_URL", "mysql://root:hunter2@db")])).expect_err("port 0 is refused");
    assert!(problems.iter().all(|p| !p.contains("hunter2")), "error messages never include secret values: {problems:?}");

    let health = std::sync::Arc::new(Health { ready: std::sync::atomic::AtomicBool::new(false), draining: std::sync::atomic::AtomicBool::new(false) });
    let router = app(health.clone());
    assert_eq!(call(&router, "GET", "/livez", &[], "").await.0, 200, "liveness is 200 while the process runs");
    assert_eq!(call(&router, "GET", "/readyz", &[], "").await.0, 503, "not ready until startup work is done");
    health.ready.store(true, Ordering::SeqCst);
    assert_eq!(call(&router, "GET", "/readyz", &[], "").await.0, 200, "ready once startup completes");

    let listener = tokio::net::TcpListener::bind("127.0.0.1:0").await.unwrap();
    let address = listener.local_addr().unwrap();
    let router = app(health.clone()).route("/slow", axum::routing::get(|| async {
        tokio::time::sleep(std::time::Duration::from_millis(300)).await;
        "done"
    }));
    let (stop, shutdown) = tokio::sync::oneshot::channel();
    let server = tokio::spawn(serve_until(listener, router, health.clone(), shutdown));
    let client = tokio::spawn(async move {
        use tokio::io::{AsyncReadExt, AsyncWriteExt};
        let mut stream = tokio::net::TcpStream::connect(address).await.unwrap();
        stream.write_all(b"GET /slow HTTP/1.1\\r\\nHost: test\\r\\nConnection: close\\r\\n\\r\\n").await.unwrap();
        let mut reply = Vec::new();
        let _ = stream.read_to_end(&mut reply).await;
        String::from_utf8_lossy(&reply).into_owned()
    });
    tokio::time::sleep(std::time::Duration::from_millis(80)).await;
    stop.send(()).unwrap();
    let signalled = std::time::Instant::now();
    tokio::time::timeout(std::time::Duration::from_secs(3), server).await.expect("serve_until returns once in-flight requests finish").unwrap().unwrap();
    let waited = signalled.elapsed();
    assert!(waited >= std::time::Duration::from_millis(150), "serve_until returned {waited:?} after the signal, before the in-flight request finished: wait for it, or the process exits and cuts it off");
    let reply = client.await.unwrap();
    assert!(reply.starts_with("HTTP/1.1 200") && reply.ends_with("done"), "a request already in progress at shutdown still completes: got {reply:?}");
    assert!(health.draining.load(Ordering::SeqCst), "shutdown marks the service as draining");
    assert_eq!(call(&app(health.clone()), "GET", "/readyz", &[], "").await.0, 503, "a draining service reports not ready, so load balancers stop sending traffic");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'pathway-rustc /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-structs-enums':
      return {
        files: { 'main.rs': `${code}

fn main() {
    let invited = Enrollment::Invited { email: String::from("ada@example.com") };
    assert_eq!(summary(&invited), "invited ada@example.com", "summary of an invitation");
    let active = accept(invited);
    assert_eq!(summary(&active), "active 0", "accepting an invitation starts at zero lessons");
    let active = complete_lesson(complete_lesson(active));
    assert_eq!(summary(&active), "active 2", "completing lessons counts each one");
    let cancelled = complete_lesson(Enrollment::Cancelled { reason: String::from("moved") });
    assert_eq!(summary(&cancelled), "cancelled moved", "a cancelled enrollment cannot complete lessons");
    let still_cancelled = accept(Enrollment::Cancelled { reason: String::from("duplicate") });
    assert_eq!(summary(&still_cancelled), "cancelled duplicate", "accepting does not revive a cancelled enrollment");
    let still_active = accept(Enrollment::Active { email: String::from("lin@example.com"), completed: 5 });
    assert_eq!(summary(&still_active), "active 5", "accepting an active enrollment leaves it unchanged");
    let invited_again = complete_lesson(Enrollment::Invited { email: String::from("sam@example.com") });
    assert_eq!(summary(&invited_again), "invited sam@example.com", "an invitation cannot complete lessons");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-traits-generics':
      return {
        files: { 'main.rs': `${code}

fn main() {
    assert_eq!(Seat { learners: 1 }.monthly_cents(), 900, "one seat costs 900 cents");
    assert_eq!(Workspace { flat_cents: 125 }.monthly_cents(), 125, "a workspace costs its flat amount");
    let seats = [Seat { learners: 3 }, Seat { learners: 1 }];
    assert_eq!(total_cents(&seats), 3_600, "total_cents adds up one type of item");
    let workspaces = [Workspace { flat_cents: 5_000 }, Workspace { flat_cents: 250 }];
    assert_eq!(total_cents(&workspaces), 5_250, "total_cents works for every Billable type");
    let none: [Workspace; 0] = [];
    assert_eq!(total_cents(&none), 0, "an empty list costs nothing");
    let mixed: Vec<Box<dyn Billable>> = vec![Box::new(Seat { learners: 2 }), Box::new(Workspace { flat_cents: 5_000 })];
    assert_eq!(total_mixed(&mixed), 6_800, "total_mixed adds up different types through dyn Billable");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-errors-panics':
      return {
        files: { 'main.rs': `${code}

fn requires_std_error<E: std::error::Error>(_: &E) {}

fn main() {
    assert_eq!(parse_minutes("45"), Ok(45), "a plain number parses");
    assert_eq!(parse_minutes("  30 \\n"), Ok(30), "surrounding whitespace is ignored");
    assert_eq!(parse_minutes("90"), Ok(90), "90 minutes is allowed");
    assert_eq!(parse_minutes("   "), Err(DurationError::Empty), "blank input is Empty");
    assert_eq!(parse_minutes("ten"), Err(DurationError::NotANumber(String::from("ten"))), "text is NotANumber with the trimmed input");
    assert_eq!(parse_minutes("-5"), Err(DurationError::NotANumber(String::from("-5"))), "negative numbers are NotANumber");
    assert_eq!(parse_minutes("120"), Err(DurationError::TooLong(120)), "over 90 minutes is TooLong");
    assert_eq!(total_minutes(&["30", "45", " 15 "]), Ok(90), "total_minutes adds valid durations");
    assert_eq!(total_minutes(&[]), Ok(0), "no durations total zero");
    assert_eq!(total_minutes(&["30", "abc", ""]), Err(DurationError::NotANumber(String::from("abc"))), "total_minutes stops at the first error");
    assert_eq!(DurationError::Empty.to_string(), "duration is empty", "Display for Empty");
    assert_eq!(DurationError::NotANumber(String::from("abc")).to_string(), "'abc' is not a number of minutes", "Display for NotANumber");
    assert_eq!(DurationError::TooLong(120).to_string(), "120 minutes is longer than the 90-minute limit", "Display for TooLong");
    requires_std_error(&DurationError::Empty);
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-iterators-closures':
      return {
        files: { 'main.rs': `${code}

fn attempt(learner: &str, score: u32) -> Attempt {
    Attempt { learner: String::from(learner), score }
}

fn main() {
    let attempts = vec![attempt("ada", 72), attempt("lin", 45), attempt("ada", 91), attempt("sam", 60), attempt("lin", 60)];
    assert_eq!(passing_learners(&attempts, 60), vec!["ada", "ada", "sam", "lin"], "passing_learners keeps order and includes scores equal to the pass mark");
    assert_eq!(passing_learners(&attempts, 100), Vec::<&str>::new(), "nobody passes an impossible mark");
    assert_eq!(average_score(&attempts), Some(65.6), "average_score is the mean as f64");
    assert_eq!(average_score(&[attempt("ada", 2), attempt("lin", 3)]), Some(2.5), "average_score does not use integer division");
    assert_eq!(average_score(&[]), None, "average_score of nothing is None");
    let best = best_by_learner(&attempts);
    assert_eq!(best.get("ada"), Some(&91), "best_by_learner keeps the highest score");
    assert_eq!(best.get("lin"), Some(&60), "best_by_learner keeps the highest score even when it comes last");
    assert_eq!(best.len(), 3, "best_by_learner has one entry per learner");
    assert_eq!(best.keys().map(|name| name.as_str()).collect::<Vec<_>>(), vec!["ada", "lin", "sam"], "best_by_learner is a BTreeMap ordered by name");
    let grader = make_grader(70);
    assert!(grader(70) && grader(95), "the grader passes scores at or above its mark");
    assert!(!grader(69), "the grader fails scores below its mark");
    let strict = make_grader(90);
    let passed = attempts.iter().filter(|a| strict(a.score)).count();
    assert_eq!(passed, 1, "each grader remembers its own pass mark");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-smart-pointers':
      return {
        files: { 'main.rs': `${code}

fn main() {
    let path = Path::Step(String::from("ownership"), Box::new(Path::Step(String::from("borrowing"), Box::new(Path::End))));
    assert_eq!(path_len(&Path::End), 0, "an empty path has no steps");
    assert_eq!(path_len(&path), 2, "path_len counts every Step through the boxes");

    let roster = new_roster();
    let other_view = std::rc::Rc::clone(&roster);
    assert_eq!(std::rc::Rc::strong_count(&roster), 2, "cloning an Rc shares the roster instead of copying it");
    assert!(enroll(&roster, "ada"), "enrolling a new learner returns true");
    assert!(!enroll(&other_view, "ada"), "enrolling the same learner through another handle returns false");
    assert!(enroll(&other_view, "lin"), "another handle can enroll a new learner");
    assert_eq!(roster_size(&roster), 2, "every handle sees the same learners");
    assert_eq!(roster_size(&other_view), 2, "every handle sees the same learners");
    drop(other_view);
    assert_eq!(std::rc::Rc::strong_count(&roster), 1, "dropping a handle lowers the count");

    assert_eq!(count_in_threads(4, 1_000), 4_000, "all threads add to one shared counter");
    assert_eq!(count_in_threads(1, 7), 7, "one thread counts on its own");
    assert_eq!(count_in_threads(0, 10), 0, "no threads count nothing");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-concurrency-send-sync':
      return {
        files: { 'main.rs': `${code}

fn requires_send_and_sync<T: Send + Sync>() {}

fn main() {
    let numbers: Vec<u64> = (1..=1_000).collect();
    assert_eq!(parallel_sum(&numbers, 4), 500_500, "four workers sum 1..=1000");
    assert_eq!(parallel_sum(&numbers, 3), 500_500, "uneven chunks still include every number");
    assert_eq!(parallel_sum(&numbers, 1), 500_500, "one worker sums everything");
    assert_eq!(parallel_sum(&[5, 6, 7], 8), 18, "more workers than numbers still works");
    assert_eq!(parallel_sum(&[], 4), 0, "an empty slice sums to zero");

    assert_eq!(squares_in_order(vec![3, 1, 2]), vec![9, 1, 4], "results come back in job order, not finishing order");
    assert_eq!(squares_in_order((1..=20).rev().collect()), (1..=20u64).rev().map(|n| n * n).collect::<Vec<_>>(), "many jobs keep their order");
    assert_eq!(squares_in_order(Vec::new()), Vec::<u64>::new(), "no jobs, no results");

    requires_send_and_sync::<HitCounter>();
    let counter = HitCounter::new();
    std::thread::scope(|scope| {
        for _ in 0..8 {
            scope.spawn(|| {
                for _ in 0..1_000 {
                    counter.record();
                }
            });
        }
    });
    assert_eq!(counter.total(), 8_000, "every hit from every thread is counted");
    println!("PATHWAY_TEST_PASS");
}
` },
        command: ['/bin/sh', '-c', 'rustc --edition=2024 /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'rust-async-tokio':
      return {
        files: { 'main.rs': `${code}

static IN_FLIGHT: std::sync::atomic::AtomicUsize = std::sync::atomic::AtomicUsize::new(0);
static MAX_IN_FLIGHT: std::sync::atomic::AtomicUsize = std::sync::atomic::AtomicUsize::new(0);

/// Counts a call as finished when it completes *or* is cancelled. A timed-out future stops at its
/// .await and never reaches code after it, so the decrement lives in Drop.
struct PathwayInFlightGuard;

impl Drop for PathwayInFlightGuard {
    fn drop(&mut self) {
        IN_FLIGHT.fetch_sub(1, std::sync::atomic::Ordering::SeqCst);
    }
}

/// Provided by the grader: a slow "remote call" that takes 100 ms.
async fn slow_square(number: u64) -> u64 {
    let now = IN_FLIGHT.fetch_add(1, std::sync::atomic::Ordering::SeqCst) + 1;
    MAX_IN_FLIGHT.fetch_max(now, std::sync::atomic::Ordering::SeqCst);
    let _in_flight = PathwayInFlightGuard;
    tokio::time::sleep(std::time::Duration::from_millis(100)).await;
    number * number
}

#[tokio::main]
async fn main() {
    let started = std::time::Instant::now();
    let squares = squares_concurrently((1..=10).collect()).await;
    let elapsed = started.elapsed();
    assert_eq!(squares, (1..=10u64).map(|n| n * n).collect::<Vec<_>>(), "squares_concurrently returns squares in input order");
    assert!(elapsed < std::time::Duration::from_millis(600), "ten 100 ms calls should overlap, but took {elapsed:?}; await them concurrently, not one after another");
    assert_eq!(squares_concurrently(Vec::new()).await, Vec::<u64>::new(), "no numbers, no squares");

    assert_eq!(with_timeout(7, 1_000).await, Some(49), "a call that finishes in time returns Some");
    assert_eq!(with_timeout(7, 20).await, None, "a call slower than the limit returns None");
    tokio::time::sleep(std::time::Duration::from_millis(150)).await;
    assert_eq!(IN_FLIGHT.load(std::sync::atomic::Ordering::SeqCst), 0, "a timed-out call must not keep running in the background");

    MAX_IN_FLIGHT.store(0, std::sync::atomic::Ordering::SeqCst);
    let started = std::time::Instant::now();
    let bounded = bounded_squares((1..=9).collect(), 3).await;
    let elapsed = started.elapsed();
    let peak = MAX_IN_FLIGHT.load(std::sync::atomic::Ordering::SeqCst);
    assert_eq!(bounded, (1..=9u64).map(|n| n * n).collect::<Vec<_>>(), "bounded_squares returns squares in input order");
    assert!(peak <= 3, "bounded_squares ran {peak} calls at once; the limit is 3");
    assert!(peak >= 2, "bounded_squares should still run calls concurrently (peak was {peak})");
    assert!(elapsed < std::time::Duration::from_millis(900), "nine calls three at a time should take about 300 ms, but took {elapsed:?}");
    println!("PATHWAY_TEST_PASS");
}
` },
        // pathway-rustc links the image's prebuilt Tokio (see images/rust/Dockerfile).
        command: ['/bin/sh', '-c', 'pathway-rustc /workspace/main.rs -o /workspace/main && /workspace/main'],
        tests: 4,
        runtime: 'rust',
      }
    case 'foundations-making-decisions':
      return {
        files: {
          'Program.cs': `using System;\n\npublic static class Program\n{\n    public static void Main()\n    {\n${code}\n    }\n}\n`,
          'Sandbox.csproj': '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>',
        },
        command: ['/bin/sh', '-c', 'dotnet restore /workspace/Sandbox.csproj --ignore-failed-sources --nologo && dotnet run --project /workspace/Sandbox.csproj --no-restore --nologo'],
        tests: 2,
        requiredOutput: 'You can watch!',
      }
    case 'modern-csharp-records':
      return {
        files: {
          'Program.cs': `using System;\nusing System.Linq;\nusing System.Reflection;\n${code}\n\npublic static class Program\n{\n    public static void Main()\n    {\n        var type = typeof(Order);\n        var names = type.GetProperties().Select(property => property.Name).ToHashSet();\n        var printMembers = type.GetMethod("PrintMembers", BindingFlags.Instance | BindingFlags.NonPublic);\n        if (!names.IsSupersetOf(new[] { "Id", "Status" }) || printMembers is null)\n            throw new InvalidOperationException("Order must be a record with Id and Status properties.");\n        Console.WriteLine("PATHWAY_TEST_PASS");\n    }\n}\n`,
          'Sandbox.csproj': '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>',
        },
        command: ['/bin/sh', '-c', 'dotnet restore /workspace/Sandbox.csproj --ignore-failed-sources --nologo && dotnet run --project /workspace/Sandbox.csproj --no-restore --nologo'],
        tests: 2,
      }
    case 'web-api-sealed-services':
      return {
        files: {
          'Program.cs': `using System;\nusing System.Linq;\nusing System.Threading;\nusing System.Threading.Tasks;\n\npublic interface IOrderService { }\n\n${code}\n\npublic static class Program\n{\n    public static void Main()\n    {\n        var type = typeof(OrderService);\n        var hasCancellationAwareAsyncMethod = type.GetMethods().Any(method =>\n            typeof(Task).IsAssignableFrom(method.ReturnType) &&\n            method.GetParameters().Any(parameter => parameter.ParameterType == typeof(CancellationToken)));\n        if (!type.IsSealed || !typeof(IOrderService).IsAssignableFrom(type) || !hasCancellationAwareAsyncMethod)\n            throw new InvalidOperationException("OrderService must be sealed, implement IOrderService, and expose an async operation with CancellationToken.");\n        Console.WriteLine("PATHWAY_TEST_PASS");\n    }\n}\n`,
          'Sandbox.csproj': '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings></PropertyGroup></Project>',
        },
        command: ['/bin/sh', '-c', 'dotnet restore /workspace/Sandbox.csproj --ignore-failed-sources --nologo && dotnet run --project /workspace/Sandbox.csproj --no-restore --nologo'],
        tests: 3,
      }
    case 'python-fastapi-endpoint':
      return {
        files: {
          'submission.py': code,
          'test_submission.py': `import ast\n\nsource = open('/workspace/submission.py', encoding='utf-8').read()\ntree = ast.parse(source)\nfor node in ast.walk(tree):\n    if not isinstance(node, (ast.FunctionDef, ast.AsyncFunctionDef)):\n        continue\n    has_health_decorator = any(\n        isinstance(decorator, ast.Call)\n        and isinstance(decorator.func, ast.Attribute)\n        and decorator.func.attr == 'get'\n        and decorator.args\n        and isinstance(decorator.args[0], ast.Constant)\n        and decorator.args[0].value == '/health'\n        for decorator in node.decorator_list\n    )\n    if has_health_decorator and any(isinstance(item, ast.Return) for item in ast.walk(node)):\n        print('PATHWAY_TEST_PASS')\n        break\nelse:\n    raise AssertionError('Add a @app.get(\"/health\") function that returns a value.')\n`,
        },
        command: ['python3', '/workspace/test_submission.py'],
        tests: 2,
      }

    case 'linux-paths-navigation':
      return linuxFixture(code, "mkdir -p /workspace/project/src; printf 'a\\n' > /workspace/project/README.md; printf 'b\\n' > /workspace/project/config.yml", "grep -Fxq '/workspace/project/src' /workspace/learner.out; grep -Fq 'README.md' /workspace/learner.out; grep -Fq 'config.yml' /workspace/learner.out")
    case 'linux-files-directories':
      return linuxFixture(code, "mkdir -p /workspace/project; cd /workspace/project; printf 'report\\n' > report.txt; printf 'draft\\n' > draft.txt; printf 'tmp\\n' > scratch.tmp", "test -f /workspace/project/archive/2026/report.txt; test -f /workspace/project/release-notes.txt; test ! -e /workspace/project/draft.txt; test ! -e /workspace/project/scratch.tmp")
    case 'linux-read-text':
      return linuxFixture(code, "printf 'one\\ntwo\\nthree\\nfour\\nfive\\n' > /workspace/app.log", "grep -Fq 'one' /workspace/learner.out; grep -Fq 'two' /workspace/learner.out; grep -Fq 'four' /workspace/learner.out; grep -Fq 'five' /workspace/learner.out; grep -Eq '(^|[^0-9])5([^0-9]|$)' /workspace/learner.out")
    case 'linux-pipes-redirection':
      return linuxFixture(code, "printf 'ERROR api timeout\\nINFO ok\\nERROR api timeout\\nERROR db down\\n' > /workspace/app.log", "test -f /workspace/error-counts.txt; grep -Eq '2[[:space:]]+ERROR api timeout' /workspace/error-counts.txt; grep -Eq '1[[:space:]]+ERROR db down' /workspace/error-counts.txt; ! grep -Fq 'INFO ok' /workspace/error-counts.txt")
    case 'linux-grep-find':
      return linuxFixture(code, "mkdir -p /workspace/config/nested; printf 'enabled=true\\n' > /workspace/config/app.conf; printf 'enabled=false\\n' > /workspace/config/off.conf; printf 'enabled=true\\n' > /workspace/config/nested/extra.conf; printf 'enabled=true\\n' > /workspace/config/ignore.txt", "grep -Fq 'app.conf' /workspace/learner.out; grep -Fq 'extra.conf' /workspace/learner.out; ! grep -Fq 'off.conf' /workspace/learner.out; ! grep -Fq 'ignore.txt' /workspace/learner.out")
    case 'linux-expansion-quoting':
      return linuxFixture(code, "mkdir -p '/workspace/source files'; printf 'hello\\n' > '/workspace/source files/release notes.txt'; export SOURCE='/workspace/source files/release notes.txt'", "test -f '/workspace/backup/release notes.txt'; test \"$(find /workspace/backup -maxdepth 1 -type f | wc -l)\" = '1'")
    case 'linux-permissions':
      return linuxFixture(code, "printf '#!/bin/sh\\n' > /workspace/deploy.sh; chmod 600 /workspace/deploy.sh", "test \"$(stat -c '%a' /workspace/deploy.sh)\" = '750'")
    case 'linux-environment-path':
      return linuxFixture(code, "mkdir -p /workspace/home/pathway/bin; printf '#!/bin/sh\\nprintf \\\"hello\\\\n\\\"\\n' > /workspace/home/pathway/bin/hello-pathway; chmod +x /workspace/home/pathway/bin/hello-pathway", "grep -Fxq '/workspace/home/pathway/bin/hello-pathway' /workspace/learner.out")
    case 'linux-archives-compression':
      return linuxFixture(code, "mkdir -p /workspace/dist; printf 'app\\n' > /workspace/dist/app.txt; printf 'cfg\\n' > /workspace/dist/config.ini", "test -f /workspace/release.tar.gz; tar -tzf /workspace/release.tar.gz | grep -Fq 'dist/app.txt'; tar -tzf /workspace/release.tar.gz | grep -Fq 'dist/config.ini'; grep -Fq 'dist/app.txt' /workspace/learner.out")
    case 'bash-script-basics':
      return linuxFixture(code, '', "test -x /workspace/greet.sh; head -n 1 /workspace/greet.sh | grep -Fqx '#!/usr/bin/env bash'; grep -Fxq 'Hello, Pathway!' /workspace/learner.out")
    case 'bash-variables-arguments':
      return linuxFixture(code, '', "test -x /workspace/show-user.sh; set +e; /workspace/show-user.sh >/workspace/missing.out 2>/workspace/missing.err; missing_status=$?; set -e; test \"$missing_status\" = '2'; grep -Fxq 'Usage: show-user.sh USER' /workspace/missing.err; /workspace/show-user.sh Ada | grep -Fxq 'User: Ada'")
    case 'bash-status-conditionals':
      return linuxFixture(code, "printf 'data\\n' > /workspace/full.txt; : > /workspace/empty.txt", "test -x /workspace/validate.sh; /workspace/validate.sh /workspace/full.txt | grep -Fxq 'ready'; set +e; /workspace/validate.sh /workspace/empty.txt >/workspace/empty.out 2>/workspace/empty.err; s1=$?; /workspace/validate.sh /workspace/missing.txt >/workspace/miss.out 2>/workspace/miss.err; s2=$?; set -e; test \"$s1\" = '1'; test \"$s2\" = '1'; grep -Fxq 'invalid' /workspace/empty.err; grep -Fxq 'invalid' /workspace/miss.err")
    case 'bash-loops-functions':
      return linuxFixture(code, "printf 'a\\nb\\n' > '/workspace/alpha file.txt'; printf 'x\\n' > /workspace/beta.txt", "test -x /workspace/count-lines.sh; /workspace/count-lines.sh '/workspace/alpha file.txt' /workspace/beta.txt > /workspace/count.out; grep -Fq '/workspace/alpha file.txt: 2' /workspace/count.out; grep -Fq '/workspace/beta.txt: 1' /workspace/count.out")
    case 'bash-safe-scripting':
      return linuxFixture(code, '', "test -x /workspace/safe-temp.sh; grep -Fq 'Pathway' /workspace/learner.out; grep -Fq 'set -euo pipefail' /workspace/safe-temp.sh; grep -Fq 'mktemp' /workspace/safe-temp.sh; grep -Fq 'trap' /workspace/safe-temp.sh")
    case 'bash-text-processing':
      return linuxFixture(code, "printf '1,Ada,active\\n2,Grace,inactive\\n3,Linus,active\\n4,Ada,active\\n' > /workspace/users.csv", "printf 'Ada\\nLinus\\n' > /workspace/expected_names; cmp -s /workspace/expected_names /workspace/learner.out")
    case 'vim-search-substitute':
      return vimFixture(code, "printf 'TODO one\\nkeep\\nTODO two\\n' > /workspace/notes.txt", "! grep -Fq 'TODO' /workspace/notes.txt; test \"$(grep -c '^DONE' /workspace/notes.txt)\" = '2'")
    case 'linux-cli-capstone':
      return linuxFixture(code, "mkdir -p '/workspace/source bundle'; printf 'ERROR db\\nINFO ok\\nERROR db\\nERROR api\\n' > '/workspace/source bundle/app.log'; printf 'PORT=8080\\n' > '/workspace/source bundle/app.conf'; printf 'ignore\\n' > '/workspace/source bundle/ignore.txt'", "test -x /workspace/support-bundle.sh; /workspace/support-bundle.sh '/workspace/source bundle' /workspace/support.tar.gz; test -f /workspace/support.tar.gz; tar -tzf /workspace/support.tar.gz | grep -Fq 'app.log'; tar -tzf /workspace/support.tar.gz | grep -Fq 'app.conf'; tar -tzf /workspace/support.tar.gz | grep -Fq 'error-summary.txt'; mkdir -p /workspace/check; tar -xzf /workspace/support.tar.gz -C /workspace/check; summary=$(find /workspace/check -name error-summary.txt -print -quit); grep -Fq 'ERROR api' \"$summary\"; grep -Fq 'ERROR db' \"$summary\"")

    case 'sqlite-open-inspect':
      return sqliteFixture(code, '', "test \"$(sqlite3 \"$DB\" \"SELECT COUNT(*) FROM sqlite_schema WHERE type='table' AND name='products';\")\" = '1'; test \"$(sqlite3 \"$DB\" \"SELECT pk FROM pragma_table_info('products') WHERE name='id';\")\" = '1'; test \"$(sqlite3 \"$DB\" \"SELECT [notnull] FROM pragma_table_info('products') WHERE name='name';\")\" = '1'")
    case 'sqlite-strict-tables':
      return sqliteFixture(code, '', "grep -Eq 'CREATE TABLE users.*STRICT' <(sqlite3 \"$DB\" \"SELECT replace(sql, char(10), ' ') FROM sqlite_schema WHERE name='users';\") || { echo 'users must be STRICT'; exit 1; }; test \"$(sqlite3 \"$DB\" \"SELECT COUNT(*) FROM pragma_index_list('users') WHERE [unique]=1;\")\" -ge 1; test \"$(sqlite3 \"$DB\" \"SELECT dflt_value FROM pragma_table_info('users') WHERE name='active';\")\" = '1'")
    case 'sqlite-constraints':
      return sqliteFixture(code, '', "sqlite3 \"$DB\" \"INSERT INTO inventory(sku, location) VALUES('A','WH1');\"; test \"$(sqlite3 \"$DB\" \"SELECT quantity FROM inventory WHERE sku='A';\")\" = '0'; ! sqlite3 \"$DB\" \"INSERT INTO inventory(sku,quantity,location) VALUES('B',-1,'WH1');\" >/dev/null 2>&1; test \"$(sqlite3 \"$DB\" \"SELECT pk FROM pragma_table_info('inventory') WHERE name='sku';\")\" = '1'")
    case 'sqlite-crud':
      return sqliteFixture(code, "CREATE TABLE users(id INTEGER PRIMARY KEY, email TEXT NOT NULL UNIQUE, active INTEGER NOT NULL DEFAULT 1); INSERT INTO users(email) VALUES('temp@example.test');", "test \"$(sqlite3 \"$DB\" \"SELECT COUNT(*) FROM users WHERE email IN ('ada@example.test','grace@example.test');\")\" = '2'; test \"$(sqlite3 \"$DB\" \"SELECT active FROM users WHERE email='grace@example.test';\")\" = '0'; test \"$(sqlite3 \"$DB\" \"SELECT COUNT(*) FROM users WHERE email='temp@example.test';\")\" = '0'")
    case 'sqlite-select-filter-sort':
      return sqliteFixture(code, "CREATE TABLE users(email TEXT, active INTEGER, created_at TEXT); INSERT INTO users VALUES('old@example.test',1,'2026-01-01'),('new@example.test',1,'2026-09-01'),('off@example.test',0,'2026-10-01'),('b@example.test',1,'2026-08-01'),('c@example.test',1,'2026-07-01'),('d@example.test',1,'2026-06-01'),('e@example.test',1,'2026-05-01'),('f@example.test',1,'2026-04-01');", "test \"$(wc -l < /workspace/learner.out | tr -d ' ')\" = '5'; head -n 1 /workspace/learner.out | grep -Fq 'new@example.test|2026-09-01'; ! grep -Fq 'off@example.test' /workspace/learner.out")
    case 'sqlite-joins':
      return sqliteFixture(code, "CREATE TABLE customers(id INTEGER PRIMARY KEY,name TEXT); CREATE TABLE orders(id INTEGER PRIMARY KEY,customer_id INTEGER,total REAL); INSERT INTO customers VALUES(1,'Ada'),(2,'Grace'); INSERT INTO orders VALUES(10,1,12.5),(11,2,30.0);", "test \"$(wc -l < /workspace/learner.out | tr -d ' ')\" = '2'; grep -Fq '10|Ada|12.5' /workspace/learner.out; grep -Fq '11|Grace|30.0' /workspace/learner.out")
    case 'sqlite-aggregates-group-having':
      return sqliteFixture(code, "CREATE TABLE orders(customer_id INTEGER,total REAL); INSERT INTO orders VALUES(1,10),(1,20),(2,5),(3,7),(3,8),(3,9);", "grep -Fq '1|2|30.0' /workspace/learner.out; grep -Fq '3|3|24.0' /workspace/learner.out; ! grep -Eq '^2\\|' /workspace/learner.out")
    case 'sqlite-ctes-subqueries':
      return sqliteFixture(code, "CREATE TABLE users(role TEXT,active INTEGER); INSERT INTO users VALUES('admin',1),('admin',1),('admin',0),('editor',1),('viewer',1),('viewer',1);", "grep -Fq 'admin|2' /workspace/learner.out; grep -Fq 'viewer|2' /workspace/learner.out; ! grep -Fq 'editor|' /workspace/learner.out")
    case 'sqlite-transactions':
      return sqliteFixture(code, "CREATE TABLE accounts(id INTEGER PRIMARY KEY,balance INTEGER NOT NULL); INSERT INTO accounts VALUES(1,100),(2,50);", "test \"$(sqlite3 \"$DB\" \"SELECT balance FROM accounts WHERE id=1;\")\" = '75'; test \"$(sqlite3 \"$DB\" \"SELECT balance FROM accounts WHERE id=2;\")\" = '75'; grep -Eqi 'BEGIN|BEGIN TRANSACTION' /workspace/submission.sql")
    case 'sqlite-foreign-keys':
      return sqliteFixture(code, "CREATE TABLE customers(id INTEGER PRIMARY KEY); INSERT INTO customers VALUES(1);", "grep -Eqi 'PRAGMA[[:space:]]+foreign_keys[[:space:]]*=[[:space:]]*ON' /workspace/submission.sql; grep -Eqi 'REFERENCES[[:space:]]+customers' /workspace/submission.sql; grep -Eqi 'ON[[:space:]]+DELETE[[:space:]]+RESTRICT' /workspace/submission.sql; ! sqlite3 \"$DB\" \"PRAGMA foreign_keys=ON; INSERT INTO orders(customer_id) VALUES(999);\" >/dev/null 2>&1")
    case 'sqlite-indexes':
      return sqliteFixture(code, "CREATE TABLE orders(id INTEGER PRIMARY KEY,customer_id INTEGER,created_at TEXT);", "test \"$(sqlite3 \"$DB\" \"SELECT COUNT(*) FROM sqlite_schema WHERE type='index' AND name='idx_orders_customer_created';\")\" = '1'; test \"$(sqlite3 \"$DB\" \"SELECT group_concat(name, ',') FROM pragma_index_info('idx_orders_customer_created') ORDER BY seqno;\")\" = 'customer_id,created_at'; test \"$(sqlite3 \"$DB\" \"SELECT [unique] FROM pragma_index_list('orders') WHERE name='idx_orders_customer_created';\")\" = '0'")
    case 'sqlite-schema-migrations':
      return sqliteFixture(code, "CREATE TABLE users(id INTEGER PRIMARY KEY,email TEXT NOT NULL); INSERT INTO users(email) VALUES('ada@example.test');", "test \"$(sqlite3 \"$DB\" \"SELECT type FROM pragma_table_info('users') WHERE name='display_name';\")\" = 'TEXT'; test \"$(sqlite3 \"$DB\" \"SELECT COUNT(*) FROM users;\")\" = '1'; test \"$(sqlite3 \"$DB\" \"SELECT [notnull] FROM pragma_table_info('users') WHERE name='display_name';\")\" = '0'")
    case 'sqlite-views-triggers':
      return sqliteFixture(code, "CREATE TABLE users(id INTEGER PRIMARY KEY,email TEXT,active INTEGER); INSERT INTO users VALUES(1,'ada@example.test',1),(2,'off@example.test',0);", "test \"$(sqlite3 \"$DB\" \"SELECT COUNT(*) FROM sqlite_schema WHERE type='view' AND name='active_users';\")\" = '1'; test \"$(sqlite3 \"$DB\" \"SELECT group_concat(name, ',') FROM pragma_table_info('active_users');\")\" = 'id,email'; test \"$(sqlite3 \"$DB\" \"SELECT COUNT(*) FROM active_users;\")\" = '1'")
    case 'sqlite-capstone':
      return sqliteFixture(code, '', "test \"$(sqlite3 \"$DB\" \"SELECT COUNT(*) FROM sqlite_schema WHERE type='table' AND name IN ('projects','users','issues','comments');\")\" = '4'; test \"$(sqlite3 \"$DB\" \"SELECT COUNT(*) FROM sqlite_schema WHERE type='index' AND name='idx_issues_project_status_created';\")\" = '1'; test \"$(sqlite3 \"$DB\" \"SELECT COUNT(*) FROM sqlite_schema WHERE type='view' AND name='open_issue_summary';\")\" = '1'; test \"$(sqlite3 \"$DB\" \"SELECT COUNT(*) FROM issues WHERE status='open';\")\" -ge 1; test \"$(sqlite3 \"$DB\" \"SELECT COUNT(*) FROM comments;\")\" -ge 1; test \"$(sqlite3 \"$DB\" \"PRAGMA integrity_check;\")\" = 'ok'; grep -Eqi 'BEGIN|BEGIN TRANSACTION' /workspace/submission.sql")

    case 'react-lite-jsx-rendering':
      return reactFixture(code, "grep -Fq 'DashboardHeading' /workspace/submission.tsx && grep -Fq '<h1' /workspace/submission.tsx && grep -Fq 'title' /workspace/submission.tsx")
    case 'react-lite-components-props':
      return reactFixture(code, "grep -Fq 'StatCard' /workspace/submission.tsx && grep -Fq 'label' /workspace/submission.tsx && grep -Fq 'value' /workspace/submission.tsx")
    case 'react-lite-state':
      return reactFixture(code, "grep -Fq 'useState' /workspace/submission.tsx && grep -Fq 'onChange' /workspace/submission.tsx && grep -Fq 'value=' /workspace/submission.tsx")
    case 'react-lite-forms':
      return reactFixture(code, "grep -Fq 'useState' /workspace/submission.tsx && grep -Fq '<form' /workspace/submission.tsx && grep -Fq \"type='email'\" /workspace/submission.tsx")
    case 'react-lite-router-pages':
      return reactFixture(code, "grep -Fq 'createBrowserRouter' /workspace/submission.tsx && grep -Fq '/users' /workspace/submission.tsx")
    case 'react-lite-layout-nav':
      return reactFixture(code, "grep -Fq 'Link' /workspace/submission.tsx && grep -Fq 'Outlet' /workspace/submission.tsx && grep -Fq '/users' /workspace/submission.tsx")
    case 'react-lite-table-filter-sort':
      return reactFixture(code, "grep -Fq 'filterUsers' /workspace/submission.tsx && grep -Fq '.filter(' /workspace/submission.tsx && grep -Fq 'toLowerCase' /workspace/submission.tsx")
    case 'react-lite-dummy-api-read':
      return reactFixture(code, "grep -Fq 'usersApi' /workspace/submission.tsx && grep -Fq 'async list' /workspace/submission.tsx && grep -Fq 'seedUsers' /workspace/submission.tsx")
    case 'react-lite-router-loader':
      return reactFixture(code, "grep -Fq 'usersLoader' /workspace/submission.tsx && grep -Fq 'usersApi.list' /workspace/submission.tsx")
    case 'react-lite-dummy-api-write':
      return reactFixture(code, "grep -Fq 'async create' /workspace/submission.tsx && grep -Fq 'users.push' /workspace/submission.tsx")
    case 'react-lite-router-action-crud':
      return reactFixture(code, "grep -Fq 'ActionFunctionArgs' /workspace/submission.tsx && grep -Fq 'formData' /workspace/submission.tsx && grep -Fq 'usersApi.create' /workspace/submission.tsx")
    case 'react-lite-basic-testing':
      return reactFixture(code, "grep -Fq \"from 'vitest'\" /workspace/submission.tsx && grep -Fq 'filterUsers' /workspace/submission.tsx && grep -Fq 'toHaveLength' /workspace/submission.tsx")
    case 'react-lite-capstone':
      return reactFixture(code, "grep -Fq 'DashboardPage' /workspace/submission.tsx && grep -Fq 'StatCard' /workspace/submission.tsx && grep -Fq 'UserTable' /workspace/submission.tsx")
    case 'react-composition-root':
      return reactFixture(code, "grep -Fq 'RouterProvider' /workspace/submission.tsx && grep -Fq 'AppProviders' /workspace/submission.tsx")
    case 'react-props-composition':
      return reactFixture(code, "grep -Fq 'OrderStatusBadge' /workspace/submission.tsx && grep -Fq '<span' /workspace/submission.tsx")
    case 'react-use-state':
      return reactFixture(code, "grep -Fq 'useState' /workspace/submission.tsx && grep -Fq 'onClick' /workspace/submission.tsx")
    case 'react-use-reducer':
      return reactFixture(code, "grep -Fq 'useReducer' /workspace/submission.tsx && grep -Fq 'type Action' /workspace/submission.tsx")
    case 'react-effects-synchronization':
      return reactFixture(code, "grep -Fq 'useEffect' /workspace/submission.tsx && grep -Fq 'subscribeToOrder' /workspace/submission.tsx && grep -Fq '[orderId]' /workspace/submission.tsx")
    case 'react-context-provider-boundaries':
      return reactFixture(code, "grep -Fq 'createContext' /workspace/submission.tsx && grep -Fq 'useTenant' /workspace/submission.tsx && grep -Fq 'TenantProvider' /workspace/submission.tsx")
    case 'react-actions-optimistic-use':
      return reactFixture(code, "grep -Fq 'useActionState' /workspace/submission.tsx && grep -Fq 'useFormStatus' /workspace/submission.tsx && grep -Fq '<form' /workspace/submission.tsx")
    case 'react-router-data-mode':
      return reactFixture(code, "grep -Fq 'createBrowserRouter' /workspace/submission.tsx && grep -Fq 'orders' /workspace/submission.tsx")
    case 'react-router-loaders-params-search':
      return reactFixture(code, "grep -Fq 'params.orderId' /workspace/submission.tsx && grep -Fq 'getOrder' /workspace/submission.tsx")
    case 'react-router-actions-navigation':
      return reactFixture(code, "grep -Fq 'approveOrder' /workspace/submission.tsx && grep -Fq '<Form' /workspace/submission.tsx")
    case 'react-api-client-boundary':
      return reactFixture(code, "grep -Fq 'encodeURIComponent' /workspace/submission.tsx && grep -Fq 'response.ok' /workspace/submission.tsx && grep -Fq 'signal' /workspace/submission.tsx")
    case 'react-tailwind-design-system':
      return reactFixture(code, "grep -Fq 'ButtonHTMLAttributes' /workspace/submission.tsx && grep -Fq 'primary' /workspace/submission.tsx && grep -Fq 'secondary' /workspace/submission.tsx")
    case 'react-testing-vitest-rtl':
      return reactFixture(code, "grep -Fq 'render(' /workspace/submission.tsx && grep -Fq 'getByRole' /workspace/submission.tsx && grep -Fq 'expect' /workspace/submission.tsx")
    case 'react-enterprise-capstone':
      return reactFixture(code, "grep -Fq 'OrderApprovalPage' /workspace/submission.tsx && grep -Fq 'OrderSummary' /workspace/submission.tsx && grep -Fq 'ApproveOrderForm' /workspace/submission.tsx")
    case "git-init-status":
      return gitFixture(code, "printf '# Pathway Git Lab\\n' > README.md", "test -d .git || { echo 'Repository was not initialized.'; exit 1; }; git status --porcelain | grep -Fq '?? README.md' || { echo 'README.md should remain untracked.'; exit 1; }")
    case "git-stage-files":
      return gitFixture(code, "git init -q; printf '# Pathway Git Lab\\n' > README.md; printf 'scratch\\n' > notes.txt", "git diff --cached --name-only | grep -Fxq 'README.md' || { echo 'README.md is not staged.'; exit 1; }; ! git diff --cached --name-only | grep -Fxq 'notes.txt' || { echo 'notes.txt should not be staged.'; exit 1; }; git status --porcelain | grep -Fq '?? notes.txt' || { echo 'notes.txt should remain untracked.'; exit 1; }")
    case "git-first-commit":
      return gitFixture(code, "git init -q; printf '# Pathway Git Lab\\n' > README.md; git add README.md", "git rev-parse --verify HEAD >/dev/null 2>&1 || { echo 'No commit exists yet.'; exit 1; }; git ls-tree --name-only HEAD | grep -Fxq 'README.md' || { echo 'README.md is not in the commit.'; exit 1; }; test -z \"$(git status --porcelain)\" || { echo 'The working tree should be clean after the commit.'; exit 1; }")
    case "git-history-diff":
      return gitFixture(code, "git init -q; printf '# Pathway Git Lab\\n' > README.md; git add README.md; git commit -qm 'Initial README'; printf 'notes v1\\n' > notes.txt; git add notes.txt; git commit -qm 'Add notes'; printf 'draft change\\n' >> notes.txt", "grep -Fq 'Add notes' /workspace/learner.out || { echo 'Output should include the Add notes commit.'; exit 1; }; grep -Fq 'Initial README' /workspace/learner.out || { echo 'Output should include the Initial README commit.'; exit 1; }; grep -Fq 'notes.txt' /workspace/learner.out || { echo 'Output should identify notes.txt as modified.'; exit 1; }")
    case "git-switch-branch":
      return gitFixture(code, "git init -q; printf 'baseline\\n' > app.txt; git add app.txt; git commit -qm 'Initial app'", "test \"$(git branch --show-current)\" = 'feature/navigation' || { echo 'Current branch should be feature/navigation.'; exit 1; }; git merge-base --is-ancestor main feature/navigation || { echo 'feature/navigation should start from main.'; exit 1; }")
    case "git-feature-commit":
      return gitFixture(code, "git init -q; printf 'Pathway app\\n' > app.txt; git add app.txt; git commit -qm 'Initial app'; git switch -q -c feature/greeting; git rev-parse main > /workspace/base_main", "test \"$(git branch --show-current)\" = 'feature/greeting' || { echo 'Stay on feature/greeting.'; exit 1; }; test \"$(git rev-list --count main..HEAD)\" -ge 1 || { echo 'The feature branch needs a commit.'; exit 1; }; grep -Fxq 'Hello from the feature branch' app.txt || { echo 'app.txt is missing the requested greeting.'; exit 1; }; test \"$(git rev-parse main)\" = \"$(cat /workspace/base_main)\" || { echo 'main should remain unchanged.'; exit 1; }")
    case "git-merge-feature":
      return gitFixture(code, "git init -q; printf '# Project\\n' > README.md; git add README.md; git commit -qm 'Initial README'; git switch -q -c feature/readme; printf '\\nFeature documentation\\n' >> README.md; git add README.md; git commit -qm 'Document feature'; git switch -q main", "git merge-base --is-ancestor feature/readme main || { echo 'feature/readme is not integrated into main.'; exit 1; }; grep -Fq 'Feature documentation' README.md || { echo 'README.md does not contain the feature content.'; exit 1; }; test -z \"$(git status --porcelain)\" || { echo 'The working tree should be clean.'; exit 1; }")
    case "git-resolve-conflict":
      return gitFixture(code, "git init -q; printf 'theme=light\\nfont=sans\\n' > settings.txt; git add settings.txt; git commit -qm 'Add settings'; git switch -q -c feature/theme; printf 'theme=purple\\nfont=sans\\n' > settings.txt; git add settings.txt; git commit -qm 'Use purple theme'; git switch -q main; printf 'theme=light\\nfont=mono\\n' > settings.txt; git add settings.txt; git commit -qm 'Use mono font'", "test -z \"$(git ls-files -u)\" || { echo 'Unmerged paths remain.'; exit 1; }; printf 'theme=purple\\nfont=mono\\n' > /workspace/expected_settings; cmp -s settings.txt /workspace/expected_settings || { echo 'settings.txt does not contain the required combined result.'; exit 1; }; test \"$(git rev-list --parents -n 1 HEAD | wc -w)\" -eq 3 || { echo 'HEAD should be the completed merge commit.'; exit 1; }")
    case "git-restore-reset":
      return gitFixture(code, "git init -q; printf 'stable app\\n' > app.txt; printf 'stable notes\\n' > notes.txt; git add app.txt notes.txt; git commit -qm 'Baseline'; printf 'temporary app edit\\n' >> app.txt; printf 'keep this notes edit\\n' >> notes.txt; git add notes.txt", "git diff --quiet -- app.txt || { echo 'The unstaged app.txt change should be discarded.'; exit 1; }; git diff --cached --quiet -- notes.txt || { echo 'notes.txt should be unstaged.'; exit 1; }; ! git diff --quiet -- notes.txt || { echo 'The notes.txt working-tree edit should be preserved.'; exit 1; }")
    case "git-ignore-generated-files":
      return gitFixture(code, "git init -q; printf 'SECRET=not-real\\n' > .env; printf 'generated log\\n' > app.log; printf 'source\\n' > src.txt", "git check-ignore -q .env || { echo '.env should be ignored.'; exit 1; }; git check-ignore -q app.log || { echo 'app.log should be ignored by *.log.'; exit 1; }; git diff --cached --name-only | grep -Fxq '.gitignore' || { echo '.gitignore should be staged.'; exit 1; }; git diff --cached --name-only | grep -Fxq 'src.txt' || { echo 'src.txt should be staged.'; exit 1; }; ! git diff --cached --name-only | grep -Fxq '.env' || { echo '.env must not be staged.'; exit 1; }; ! git diff --cached --name-only | grep -Fxq 'app.log' || { echo 'app.log must not be staged.'; exit 1; }")
    case "git-fetch-remote":
      return gitFixture(code, "cd /workspace; git init -q --bare remote.git; git init -q seed; cd seed; printf 'v1\\n' > app.txt; git add app.txt; git commit -qm 'Initial remote'; git remote add origin /workspace/remote.git; git push -q -u origin main; cd /workspace; git clone -q /workspace/remote.git repo; cd repo; git rev-parse main > /workspace/local_before; cd /workspace/seed; printf 'v2\\n' >> app.txt; git add app.txt; git commit -qm 'Remote update'; git push -q origin main; git rev-parse HEAD > /workspace/remote_head; cd /workspace/repo", "test \"$(git rev-parse origin/main)\" = \"$(cat /workspace/remote_head)\" || { echo 'origin/main was not updated by fetch.'; exit 1; }; test \"$(git rev-parse main)\" = \"$(cat /workspace/local_before)\" || { echo 'fetch should not move local main.'; exit 1; }; test \"$(git rev-parse main)\" != \"$(git rev-parse origin/main)\" || { echo 'local main should remain behind origin/main.'; exit 1; }; test -z \"$(git status --porcelain)\" || { echo 'The working tree should remain unchanged.'; exit 1; }")
    case "git-rebase-feature":
      return gitFixture(code, "git init -q; printf 'base\\n' > app.txt; git add app.txt; git commit -qm 'Initial app'; git switch -q -c feature/search; printf 'search\\n' > search.txt; git add search.txt; git commit -qm 'Add search'; git switch -q main; printf 'main update\\n' > main.txt; git add main.txt; git commit -qm 'Update main'; git switch -q feature/search", "git merge-base --is-ancestor main feature/search || { echo 'feature/search is not rebased onto main.'; exit 1; }; grep -Fxq 'search' search.txt || { echo 'The feature change was not preserved.'; exit 1; }; test -z \"$(git rev-list --merges main..feature/search)\" || { echo 'The rebased feature history should be linear.'; exit 1; }")
    case "git-revert-push":
      return gitFixture(code, "cd /workspace; git init -q --bare remote.git; cd repo; git init -q; printf 'stable\\n' > app.txt; git add app.txt; git commit -qm 'Stable release'; git remote add origin /workspace/remote.git; git push -q -u origin main; printf 'debug=true\\n' > debug.txt; git add debug.txt; git commit -qm 'Bad debug change'; git push -q origin main; git rev-parse HEAD > /workspace/bad_sha", "test \"$(git rev-parse HEAD)\" != \"$(cat /workspace/bad_sha)\" || { echo 'A new revert commit should exist.'; exit 1; }; git merge-base --is-ancestor \"$(cat /workspace/bad_sha)\" HEAD || { echo 'The bad commit should remain in history rather than being rewritten away.'; exit 1; }; test ! -e debug.txt || { echo 'The revert should remove debug.txt.'; exit 1; }; test \"$(git rev-parse HEAD)\" = \"$(git --git-dir=/workspace/remote.git rev-parse refs/heads/main)\" || { echo 'origin/main does not match local main after push.'; exit 1; }")
    default:
      return null
  }
}

export function boundedOutput(stdout = '', stderr = '') {
  const output = `${stdout}${stderr}`.trim()
  return output.length > 4_000 ? `${output.slice(0, 4_000)}\n[output truncated]` : output
}

// Vercel Sandbox image (a repository in the broker project's Vercel Container Registry) for each
// fixture runtime. Fixtures without a runtime use the .NET + Python image, as before.
const SANDBOX_IMAGES = {
  rust: 'rust',
  react: 'node',
  git: 'tools',
  sqlite: 'tools',
  linux: 'tools',
  kubernetes: 'tools',
  hook: 'tools',
  python: 'tools',
}

/** Wrapper baked into every sandbox image (images/as-learner) that drops root to uid 10001. */
export const LEARNER_WRAPPER = '/usr/local/bin/as-learner'

/** Every command that touches learner code runs through the wrapper, never directly as root. */
export function asLearner(command) {
  if (!Array.isArray(command) || command.length === 0) throw new Error('A command is required.')
  return [LEARNER_WRAPPER, ...command]
}

/** The request path without its query string, so `/health?probe=1` still routes to `/health`. */
export function requestPath(url = '/') {
  try {
    return new URL(url, 'http://broker.invalid').pathname
  } catch {
    return url
  }
}

export function sandboxImageFor(runtime, tag = 'latest') {
  return `${SANDBOX_IMAGES[runtime] ?? 'dotnet'}:${tag}`
}

export function evaluationResult(fixture, exitCode, output) {
  const passed = exitCode === 0 && output.includes('PATHWAY_TEST_PASS') && (!fixture.requiredOutput || output.includes(fixture.requiredOutput))
  return { passed, passingTests: passed ? fixture.tests : 0, totalTests: fixture.tests, feedback: passed ? 'All isolated Modal sandbox tests passed.' : (output || 'The sandboxed test did not produce a passing result.'), nextLessonSlug: null }
}


const MAX_HOOK_SCRIPT_BYTES = 20_000
const MAX_HOOK_COMMAND_BYTES = 1_000

export function validateHookRequest(value) {
  if (!value || value.event !== 'PreToolUse') return 'Only PreToolUse hooks are supported.'
  if (typeof value.matcher !== 'string' || value.matcher.length === 0 || value.matcher.length > 120) return 'Matcher must be between 1 and 120 characters.'
  if (typeof value.script !== 'string' || value.script.length === 0 || Buffer.byteLength(value.script, 'utf8') > MAX_HOOK_SCRIPT_BYTES) return 'Hook script must be between 1 and 20,000 bytes.'
  if (typeof value.command !== 'string' || value.command.length === 0 || Buffer.byteLength(value.command, 'utf8') > MAX_HOOK_COMMAND_BYTES) return 'Simulated command must be between 1 and 1,000 bytes.'
  try { new RegExp(`^(?:${value.matcher})$`) } catch { return 'Matcher is not a valid regular expression.' }
  return null
}

export function hookMatcherMatches(matcher, toolName = 'Bash') {
  if (matcher === '*') return true
  return new RegExp(`^(?:${matcher})$`).test(toolName)
}

export function hookInputFor(command) {
  return {
    session_id: 'pathway-hook-lab',
    transcript_path: '/tmp/pathway-hook-lab/transcript.jsonl',
    cwd: '/workspace',
    permission_mode: 'default',
    hook_event_name: 'PreToolUse',
    tool_name: 'Bash',
    tool_input: {
      command,
      description: 'Pathway Hook Playground simulated Bash tool call',
    },
    tool_use_id: 'toolu_pathway_lab',
  }
}

export function interpretHookResult(inputJson, matcherMatched, exitCode, stdout = '', stderr = '') {
  const cleanStdout = stdout.trim()
  const cleanStderr = stderr.trim()
  if (!matcherMatched) {
    return {
      matcherMatched: false,
      executed: false,
      exitCode: null,
      outcome: 'skipped',
      summary: 'The hook did not run because the matcher did not match the Bash tool.',
      reason: null,
      stdout: '',
      stderr: '',
      inputJson,
    }
  }

  if (exitCode === 2) {
    return {
      matcherMatched: true,
      executed: true,
      exitCode,
      outcome: 'blocked',
      summary: 'Claude Code would block this PreToolUse tool call.',
      reason: cleanStderr || 'The hook exited with blocking status code 2.',
      stdout: cleanStdout,
      stderr: cleanStderr,
      inputJson,
    }
  }

  let parsed = null
  if (cleanStdout.startsWith('{') && cleanStdout.endsWith('}')) {
    try { parsed = JSON.parse(cleanStdout) } catch { parsed = null }
  }

  const hookOutput = parsed?.hookSpecificOutput
  if (hookOutput?.hookEventName === 'PreToolUse') {
    const decision = hookOutput.permissionDecision
    const reason = hookOutput.permissionDecisionReason ?? null
    const known = new Set(['allow', 'deny', 'ask', 'defer'])
    if (!known.has(decision)) {
      return {
        matcherMatched: true,
        executed: true,
        exitCode,
        outcome: 'error',
        summary: 'Claude Code would treat this as a non-blocking hook error and continue normal permission flow.',
        reason: 'permissionDecision must be allow, deny, ask, or defer.',
        stdout: cleanStdout,
        stderr: cleanStderr,
        inputJson,
      }
    }
    const summaries = {
      deny: 'Claude Code would deny the Bash tool call and show Claude the hook reason.',
      allow: 'Claude Code would allow the Bash tool call without prompting for permission.',
      ask: 'Claude Code would require the normal user permission prompt.',
      defer: 'Claude Code would defer the tool call for later handling.',
    }
    return {
      matcherMatched: true,
      executed: true,
      exitCode,
      outcome: decision === 'deny' ? 'denied' : decision === 'allow' ? 'allowed' : decision,
      summary: summaries[decision],
      reason,
      stdout: cleanStdout,
      stderr: cleanStderr,
      inputJson,
    }
  }

  if (exitCode === 0 && !cleanStdout) {
    return {
      matcherMatched: true,
      executed: true,
      exitCode,
      outcome: 'no_decision',
      summary: 'The hook succeeded silently. Claude Code would continue through its normal permission flow.',
      reason: null,
      stdout: '',
      stderr: cleanStderr,
      inputJson,
    }
  }

  return {
    matcherMatched: true,
    executed: true,
    exitCode,
    outcome: 'error',
    summary: exitCode === 0
      ? 'Claude Code would treat this output as a non-blocking hook error and continue normal permission flow.'
      : 'The hook failed with a non-blocking status. Claude Code would continue normal permission flow.',
    reason: exitCode === 0
      ? 'PreToolUse structured decisions must be a valid JSON object on stdout.'
      : (cleanStderr || `Hook exited with status ${exitCode}.`),
    stdout: cleanStdout,
    stderr: cleanStderr,
    inputJson,
  }
}
