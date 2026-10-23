import test from 'node:test'
import assert from 'node:assert/strict'
import { boundedOutput, evaluationResult, fixtureFor, sandboxImageFor, hookInputFor, hookMatcherMatches, interpretHookResult, validateHookRequest, validateRequest } from '../src/evaluator.mjs'

test('accepts bounded valid code submissions', () => assert.equal(validateRequest({ lessonSlug: 'python-functions', code: 'def greet(name): return name' }), null))

test('builds a Rust fixture with a pinned isolated runtime', () => { const fixture = fixtureFor('rust-hello-functions', 'fn greet(name: &str) -> String { format!("Hello, {name}") }'); assert.equal(fixture.runtime, 'rust'); assert.equal(fixture.tests, 2); assert.equal(evaluationResult(fixture, 0, 'PATHWAY_TEST_PASS').passed, true) })
test('rejects malformed and oversized submissions', () => { assert.match(validateRequest({}), /required/); assert.match(validateRequest({ lessonSlug: 'x', code: 'a'.repeat(50_001) }), /50,000/) })
test('builds fixtures and returns passing isolated test result', () => { const fixture = fixtureFor('python-functions', 'def greet(name): return name'); assert.equal(fixture.tests, 2); assert.equal(evaluationResult(fixture, 0, 'PATHWAY_TEST_PASS').passed, true) })
test('has isolated fixtures for every shipped code exercise', () => {
  for (const lessonSlug of ['foundations-making-decisions', 'modern-csharp-records', 'web-api-sealed-services', 'python-functions', 'python-fastapi-endpoint'])
    assert.ok(fixtureFor(lessonSlug, 'pass'))
})

test('has isolated TypeScript fixtures for every React coding exercise', () => {
  const lessons = [
    'react-composition-root',
    'react-props-composition',
    'react-use-state',
    'react-use-reducer',
    'react-effects-synchronization',
    'react-context-provider-boundaries',
    'react-actions-optimistic-use',
    'react-router-data-mode',
    'react-router-loaders-params-search',
    'react-router-actions-navigation',
    'react-api-client-boundary',
    'react-tailwind-design-system',
    'react-testing-vitest-rtl',
    'react-enterprise-capstone',
  ]
  for (const lessonSlug of lessons) {
    const fixture = fixtureFor(lessonSlug, 'export const value = 1')
    assert.ok(fixture, `missing fixture for ${lessonSlug}`)
    assert.equal(fixture.runtime, 'react')
    assert.equal(fixture.tests, 2)
    assert.match(fixture.command.join(' '), /tsc/)
    assert.ok(fixture.files['react-stubs.d.ts'])
  }
})

test('has isolated TypeScript fixtures for every React Lite coding exercise', () => {
  const lessons = [
    'react-lite-jsx-rendering',
    'react-lite-components-props',
    'react-lite-state',
    'react-lite-forms',
    'react-lite-router-pages',
    'react-lite-layout-nav',
    'react-lite-table-filter-sort',
    'react-lite-dummy-api-read',
    'react-lite-router-loader',
    'react-lite-dummy-api-write',
    'react-lite-router-action-crud',
    'react-lite-basic-testing',
    'react-lite-capstone',
  ]
  for (const lessonSlug of lessons) {
    const fixture = fixtureFor(lessonSlug, 'export const value = 1')
    assert.ok(fixture, `missing fixture for ${lessonSlug}`)
    assert.equal(fixture.runtime, 'react')
    assert.equal(fixture.tests, 2)
    assert.match(fixture.command.join(' '), /tsc/)
  }
})

test('has isolated Linux Bash Vim fixtures for every coding exercise', () => {
  const lessons = [
    'linux-paths-navigation', 'linux-files-directories', 'linux-read-text',
    'linux-pipes-redirection', 'linux-grep-find', 'linux-expansion-quoting',
    'linux-permissions', 'linux-environment-path', 'linux-archives-compression',
    'bash-script-basics', 'bash-variables-arguments', 'bash-status-conditionals',
    'bash-loops-functions', 'bash-safe-scripting', 'bash-text-processing',
    'vim-search-substitute', 'linux-cli-capstone',
  ]
  for (const lessonSlug of lessons) {
    const fixture = fixtureFor(lessonSlug, 'printf test')
    assert.ok(fixture, `missing fixture for ${lessonSlug}`)
    assert.equal(fixture.runtime, 'linux')
    assert.equal(fixture.tests, 3)
    assert.match(fixture.files['exercise.sh'], /PATHWAY_TEST_PASS/)
  }
})

test('has isolated SQLite fixtures for every SQL coding exercise', () => {
  const lessons = [
    'sqlite-open-inspect',
    'sqlite-strict-tables',
    'sqlite-constraints',
    'sqlite-crud',
    'sqlite-select-filter-sort',
    'sqlite-joins',
    'sqlite-aggregates-group-having',
    'sqlite-ctes-subqueries',
    'sqlite-transactions',
    'sqlite-foreign-keys',
    'sqlite-indexes',
    'sqlite-schema-migrations',
    'sqlite-views-triggers',
    'sqlite-capstone',
  ]
  for (const lessonSlug of lessons) {
    const fixture = fixtureFor(lessonSlug, 'SELECT 1;')
    assert.ok(fixture, `missing fixture for ${lessonSlug}`)
    assert.equal(fixture.runtime, 'sqlite')
    assert.equal(fixture.tests, 3)
    assert.match(fixture.files['exercise.sh'], /sqlite3 -batch -bail/)
    assert.match(fixture.files['exercise.sh'], /PATHWAY_TEST_PASS/)
  }
})

test('has isolated Git fixtures for the full CLI course', () => {
  const lessons = [
    'git-init-status', 'git-stage-files', 'git-first-commit', 'git-history-diff',
    'git-switch-branch', 'git-feature-commit', 'git-merge-feature', 'git-resolve-conflict',
    'git-restore-reset', 'git-ignore-generated-files', 'git-fetch-remote',
    'git-rebase-feature', 'git-revert-push',
  ]
  for (const lessonSlug of lessons) {
    const fixture = fixtureFor(lessonSlug, 'git status')
    assert.ok(fixture, `missing fixture for ${lessonSlug}`)
    assert.equal(fixture.runtime, 'git')
    assert.equal(fixture.tests, 3)
    assert.match(fixture.files['exercise.sh'], /PATHWAY_TEST_PASS/)
  }
})
test('bounds sandbox output', () => assert.match(boundedOutput('a'.repeat(4_001)), /output truncated/))


test('validates and matches Claude PreToolUse hook requests', () => {
  const payload = { event: 'PreToolUse', matcher: 'Bash', script: 'exit 0', command: 'npm test' }
  assert.equal(validateHookRequest(payload), null)
  assert.equal(hookMatcherMatches('Bash', 'Bash'), true)
  assert.equal(hookMatcherMatches('Edit|Write', 'Bash'), false)
})

test('interprets a structured deny decision from a Claude hook', () => {
  const inputJson = JSON.stringify(hookInputFor('rm -rf /tmp/build'))
  const stdout = JSON.stringify({
    hookSpecificOutput: {
      hookEventName: 'PreToolUse',
      permissionDecision: 'deny',
      permissionDecisionReason: 'Destructive command blocked by hook',
    },
  })
  const result = interpretHookResult(inputJson, true, 0, stdout, '')
  assert.equal(result.outcome, 'denied')
  assert.equal(result.reason, 'Destructive command blocked by hook')
  assert.equal(result.executed, true)
})

test('interprets silent exit zero as no hook decision', () => {
  const inputJson = JSON.stringify(hookInputFor('npm test'))
  const result = interpretHookResult(inputJson, true, 0, '', '')
  assert.equal(result.outcome, 'no_decision')
})

test('has isolated Kubernetes fixtures for every Kubernetes coding exercise', () => {
  const lessons = ['k8s-yaml-api-objects', 'k8s-kubectl-contexts', 'k8s-labels-selectors', 'k8s-deployments-replicasets', 'k8s-services', 'k8s-resources-qos', 'k8s-debugging']
  for (const lessonSlug of lessons) {
    const fixture = fixtureFor(lessonSlug, 'kind: Pod')
    assert.ok(fixture, `missing fixture for ${lessonSlug}`)
    assert.equal(fixture.runtime, 'kubernetes')
    assert.ok(fixture.tests > 0)
    assert.match(fixture.files['kube_check.py'], /PATHWAY_TEST_PASS/)
    assert.deepEqual(fixture.command.slice(-2), [lessonSlug, '/workspace/submission.txt'])
  }
})

test('maps every fixture runtime to a Vercel Sandbox image', () => {
  const images = new Set(['dotnet', 'rust', 'node', 'tools'])
  const slugs = ['python-functions', 'modern-csharp-records', 'rust-ownership', 'react-use-state', 'git-init-status', 'sqlite-crud', 'linux-permissions', 'vim-search-substitute', 'k8s-services']
  for (const slug of slugs) {
    const fixture = fixtureFor(slug, 'x')
    assert.ok(fixture, `missing fixture for ${slug}`)
    const [repository, tag] = sandboxImageFor(fixture.runtime).split(':')
    assert.ok(images.has(repository), `${slug} -> ${repository}`)
    assert.equal(tag, 'latest')
    assert.ok(Object.keys(fixture.files).every(path => !path.startsWith('/')), `${slug} writes outside /workspace`)
  }
  assert.equal(sandboxImageFor('hook', 'v2'), 'tools:v2')
  assert.equal(sandboxImageFor(undefined), 'dotnet:latest')
})

test('every image a runtime can map to has a Dockerfile', async () => {
  const { existsSync } = await import('node:fs')
  for (const repository of ['dotnet', 'rust', 'node', 'tools'])
    assert.ok(existsSync(new URL(`../images/${repository}/Dockerfile`, import.meta.url)), `images/${repository}/Dockerfile`)
})
