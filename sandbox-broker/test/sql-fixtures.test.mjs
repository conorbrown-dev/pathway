// Runs each SQL Server course grader (src/sql-graders) against reference solutions in
// test/sql-solutions: `<slug>.pass.sql` and `<slug>.*.pass.sql` must pass, `<slug>.*.fail.sql` must
// fail. Every grader needs at least one of each, so a grader that accepts everything (or nothing)
// can't slip through. scripts/fixtures-in-image.mjs repeats this inside the real tools image.
import test from 'node:test'
import assert from 'node:assert/strict'
import { spawnSync } from 'node:child_process'
import { mkdtempSync, readdirSync, readFileSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'
import { evaluationResult, fixtureFor, sqlGradedSlugs } from '../src/evaluator.mjs'

const solutionsDir = fileURLToPath(new URL('./sql-solutions/', import.meta.url))
const hasPython = spawnSync('python3', ['--version']).status === 0
const solutions = readdirSync(solutionsDir).filter(name => name.endsWith('.sql')).sort()

function grade(slug, code) {
  const fixture = fixtureFor(slug, code)
  assert.ok(fixture, `missing fixture for ${slug}`)
  assert.equal(fixture.runtime, 'python')
  const workspace = mkdtempSync(join(tmpdir(), `${slug}-`))
  for (const [path, contents] of Object.entries(fixture.files)) writeFileSync(join(workspace, path), contents)
  const command = fixture.command.map(part => part.replaceAll('/workspace', workspace))
  const result = spawnSync(command[0], command.slice(1), { encoding: 'utf8', timeout: 60_000 })
  const output = `${result.stdout}${result.stderr}`
  return { ...evaluationResult(fixture, result.status ?? 1, output), output }
}

test('every SQL reference solution has a known outcome', () => {
  assert.ok(solutions.length > 0)
  for (const name of solutions) assert.match(name, /\.(pass|fail)\.sql$/, `${name} must end in .pass.sql or .fail.sql`)
})

test('every SQL grader has passing and failing reference solutions', () => {
  for (const slug of sqlGradedSlugs()) {
    assert.ok(solutions.some(name => name.split('.')[0] === slug && name.endsWith('.pass.sql')), `${slug} has no .pass.sql solution`)
    assert.ok(solutions.some(name => name.split('.')[0] === slug && name.endsWith('.fail.sql')), `${slug} has no .fail.sql solution`)
  }
})

test('the grader cannot be fooled by printing the pass marker', { skip: !hasPython }, () => {
  const slug = sqlGradedSlugs()[0]
  const result = grade(slug, "PRINT 'PATHWAY_TEST_PASS';\nSELECT 'PATHWAY_TEST_PASS';\n")
  assert.equal(result.passed, false, result.output)
})

for (const name of solutions) {
  const slug = name.split('.')[0]
  const shouldPass = name.endsWith('.pass.sql')
  test(`${slug}: ${name} ${shouldPass ? 'passes' : 'fails'}`, { skip: !hasPython }, () => {
    const code = readFileSync(join(solutionsDir, name), 'utf8')
    const result = grade(slug, code)
    assert.equal(result.passed, shouldPass, result.output.slice(0, 2_000))
    // A failing solution names the feedback it should produce, so it fails for the intended reason.
    if (!shouldPass) {
      const expected = /^(?:--) expect: (.+)$/m.exec(code)?.[1]
      assert.ok(expected, `${name} needs a "-- expect: <feedback text>" line`)
      assert.ok(result.output.includes(expected.trim()), `${name} failed, but not with "${expected}":\n${result.output.slice(0, 1_000)}`)
    }
  })
}
