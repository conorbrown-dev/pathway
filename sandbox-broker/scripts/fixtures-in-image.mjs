// Runs every Rust reference solution through its real fixture inside a built sandbox image, the
// way production grades it: as the learner user (via as-learner), with no network.
//
//   node scripts/fixtures-in-image.mjs <docker-image>
//
// `<slug>.pass.rs` / `<slug>.*.pass.rs` must pass and `<slug>.*.fail.rs` must fail. Exits non-zero
// on any mismatch, and reports each run's duration against the sandbox's per-command limit.
import { spawnSync } from 'node:child_process'
import { chmodSync, mkdtempSync, readdirSync, readFileSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { evaluationResult, fixtureFor, LEARNER_WRAPPER } from '../src/evaluator.mjs'

const image = process.argv[2]
if (!image) {
  console.error('usage: node scripts/fixtures-in-image.mjs <docker-image>')
  process.exit(2)
}

const LIMIT_MS = 12_000 // EXERCISE_TIMEOUT_MS in src/server.mjs
const solutionsDir = fileURLToPath(new URL('../test/rust-solutions/', import.meta.url))
const solutions = readdirSync(solutionsDir).filter(name => name.endsWith('.rs')).sort()
let failures = 0
const timings = []

for (const name of solutions) {
  const slug = name.split('.')[0]
  const shouldPass = name.endsWith('.pass.rs')
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
}

console.log(`${solutions.length - failures}/${solutions.length} fixture checks passed`)
if (process.env.GITHUB_ACTIONS) console.log(`::notice title=Rust fixtures in image::${solutions.length - failures}/${solutions.length} passed. ${timings.join(', ')}`)
process.exit(failures === 0 ? 0 : 1)
