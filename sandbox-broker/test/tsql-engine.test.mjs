// Runs the T-SQL engine's unit tests (test/tsql_engine_test.py) under python3 -I.
import test from 'node:test'
import assert from 'node:assert/strict'
import { spawnSync } from 'node:child_process'
import { fileURLToPath } from 'node:url'

const hasPython = spawnSync('python3', ['--version']).status === 0

test('the T-SQL engine unit tests pass', { skip: !hasPython }, () => {
  const file = fileURLToPath(new URL('./tsql_engine_test.py', import.meta.url))
  const result = spawnSync('python3', ['-I', file], { encoding: 'utf8', timeout: 120_000 })
  assert.equal(result.status, 0, `${result.stdout}${result.stderr}`.slice(0, 3_000))
})
