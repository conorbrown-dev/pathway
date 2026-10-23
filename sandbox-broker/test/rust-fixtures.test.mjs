// Compiles and runs each Rust fixture against reference solutions in test/rust-solutions:
// `<slug>.pass.rs` and `<slug>.*.pass.rs` must pass, `<slug>.fail.rs` must fail. This keeps the
// lesson text, the fixture and the grading contract in step. CI sets PATHWAY_REQUIRE_RUSTC so a
// missing compiler fails loudly; elsewhere the test skips when rustc is not installed.
import test from 'node:test'
import assert from 'node:assert/strict'
import { spawnSync } from 'node:child_process'
import { mkdtempSync, readdirSync, readFileSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { evaluationResult, fixtureFor } from '../src/evaluator.mjs'

const solutionsDir = fileURLToPath(new URL('./rust-solutions/', import.meta.url))
const hasRustc = spawnSync('rustc', ['--version']).status === 0
const skip = !hasRustc && !process.env.PATHWAY_REQUIRE_RUSTC

test('rustc is available when required', { skip: !process.env.PATHWAY_REQUIRE_RUSTC }, () => assert.ok(hasRustc, 'rustc is not installed'))

function grade(slug, code) {
  const fixture = fixtureFor(slug, code)
  assert.ok(fixture, `missing fixture for ${slug}`)
  assert.equal(fixture.runtime, 'rust')
  const workspace = mkdtempSync(join(tmpdir(), `${slug}-`))
  for (const [path, contents] of Object.entries(fixture.files)) writeFileSync(join(workspace, path), contents)
  // The fixture's command targets /workspace inside the sandbox; point it at the temporary copy.
  const command = fixture.command.map(part => part.replaceAll('/workspace', workspace))
  const result = spawnSync(command[0], command.slice(1), { encoding: 'utf8', env: { ...process.env, RUST_BACKTRACE: '0' }, timeout: 60_000 })
  return { ...evaluationResult(fixture, result.status ?? 1, `${result.stdout}${result.stderr}`), output: `${result.stdout}${result.stderr}` }
}

const solutions = readdirSync(solutionsDir).filter(name => name.endsWith('.rs')).sort()

test('every Rust reference solution has a known outcome', () => {
  assert.ok(solutions.length > 0)
  for (const name of solutions) assert.match(name, /\.(pass|fail)\.rs$/, `${name} must end in .pass.rs or .fail.rs`)
})

for (const name of solutions) {
  const slug = name.split('.')[0]
  const shouldPass = name.endsWith('.pass.rs')
  test(`${slug}: ${name} ${shouldPass ? 'passes' : 'fails'}`, { skip }, () => {
    const result = grade(slug, readFileSync(join(solutionsDir, name), 'utf8'))
    assert.equal(result.passed, shouldPass, result.output.slice(0, 2_000))
  })
}
