// Runs every reference solution through its real fixture inside a built sandbox image, the way
// production grades it: as the learner user (via as-learner), with no network.
//
//   node scripts/fixtures-in-image.mjs <docker-image> [rust|python]
//
// rust (the default) uses test/rust-solutions in the rust image; python uses test/python-solutions
// (the AI & LLM course graders) in the tools image. `<slug>.pass.*` / `<slug>.*.pass.*` must pass
// and `<slug>.*.fail.*` must fail. Exits non-zero on any mismatch, and reports each run's duration
// against the sandbox's per-command limit.
import { spawnSync } from 'node:child_process'
import { chmodSync, mkdtempSync, readdirSync, readFileSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { evaluationResult, fixtureFor, LEARNER_WRAPPER } from '../src/evaluator.mjs'

const image = process.argv[2]
const kind = process.argv[3] ?? 'rust'
const SUITES = { rust: { dir: 'rust-solutions', ext: '.rs' }, python: { dir: 'python-solutions', ext: '.py' } }
if (!image || !SUITES[kind]) {
  console.error('usage: node scripts/fixtures-in-image.mjs <docker-image> [rust|python]')
  process.exit(2)
}
const { dir, ext } = SUITES[kind]

const LIMIT_MS = 12_000 // EXERCISE_TIMEOUT_MS in src/server.mjs
const solutionsDir = fileURLToPath(new URL(`../test/${dir}/`, import.meta.url))
const solutions = readdirSync(solutionsDir).filter(name => name.endsWith(ext)).sort()
let failures = 0
const timings = []
const compileFailures = []

for (const name of solutions) {
  const slug = name.split('.')[0]
  const shouldPass = name.endsWith(`.pass${ext}`)
  const fixture = fixtureFor(slug, readFileSync(join(solutionsDir, name), 'utf8'))
  if (!fixture) {
    console.log(`FAIL ${name}: no fixture for ${slug}`)
    failures++
    continue
  }
  const workspace = mkdtempSync(join(tmpdir(), `${slug}-`))
  chmodSync(workspace, 0o777)
  for (const [path, contents] of Object.entries(fixture.files)) writeFileSync(join(workspace, path), contents, { mode: 0o644 })

  const started = Date.now()
  const run = spawnSync('docker', ['run', '--rm', '--network', 'none', '-e', 'RUST_BACKTRACE=0', '-v', `${workspace}:/workspace`, image, LEARNER_WRAPPER, ...fixture.command], { encoding: 'utf8', timeout: 120_000 })
  const elapsed = Date.now() - started
  const output = `${run.stdout ?? ''}${run.stderr ?? ''}`
  const result = evaluationResult(fixture, run.status ?? 1, output)
  const ok = result.passed === shouldPass
  const slow = shouldPass && elapsed > LIMIT_MS ? ` (over the ${LIMIT_MS} ms sandbox limit)` : ''
  console.log(`${ok ? 'ok  ' : 'FAIL'} ${name}: ${shouldPass ? 'passes' : 'fails'} as expected in ${elapsed} ms${slow}`)
  if (!ok || slow) {
    failures++
    console.log(output.slice(0, 2_000))
    if (process.env.GITHUB_ACTIONS) console.log(`::error title=${name}::${ok ? 'too slow' : 'unexpected result'} (${elapsed} ms): ${output.replace(/\s+/g, ' ').slice(0, 300)}`)
  }
  timings.push(`${name} ${elapsed} ms`)
  // A *.fail.rs that doesn't compile proves nothing about the grader's checks; list them so a
  // reviewer can confirm each one is meant to be a compile error (e.g. a borrow or no_std failure).
  if (!shouldPass && /^error(\[E\d{4}\])?: /m.test(output)) compileFailures.push(name)
}

console.log(`${solutions.length - failures}/${solutions.length} fixture checks passed`)
if (kind === 'rust') {
  console.log(`fail.rs files that fail to compile: ${compileFailures.join(', ') || 'none'}`)
  if (process.env.GITHUB_ACTIONS) console.log(`::notice title=Expected failures that are compile errors::${compileFailures.join(', ') || 'none'}`)
}
if (process.env.GITHUB_ACTIONS) console.log(`::notice title=${kind} fixtures in image::${solutions.length - failures}/${solutions.length} passed. ${timings.join(', ')}`)
process.exit(failures === 0 ? 0 : 1)
