import http from 'node:http'
import crypto from 'node:crypto'
import { Readable } from 'node:stream'
import { Sandbox } from '@vercel/sandbox'
import { boundedOutput, evaluationResult, fixtureFor, hookInputFor, hookMatcherMatches, interpretHookResult, requestPath, sandboxImageFor, validateHookRequest, validateRequest } from './evaluator.mjs'

const port = Number(process.env.PORT ?? 8080)
// Fly's private network is IPv6-only, so deployments there set HOST=::. Default to IPv4 so the
// broker still starts on hosts without IPv6.
const host = process.env.HOST ?? '0.0.0.0'
const sharedSecret = process.env.RUNNER_SHARED_SECRET
// Learner code runs in Vercel Sandbox microVMs created in this team/project, from images in the
// project's Vercel Container Registry (see images/ and .github/workflows/sandbox-images.yml).
const teamId = process.env.SANDBOX_TEAM_ID
const projectId = process.env.SANDBOX_PROJECT_ID
const imageTag = process.env.SANDBOX_IMAGE_TAG ?? 'latest'
// Off Vercel (local development, the Fly fallback) authenticate with an access token instead of
// the per-request OIDC token Vercel provides.
const accessToken = process.env.VERCEL_TOKEN
const configured = Boolean(sharedSecret && teamId && projectId)

const SANDBOX_TIMEOUT_MS = 60_000
const EXERCISE_TIMEOUT_MS = 12_000
const HOOK_TIMEOUT_MS = 5_000

function reply(response, status, value) { response.writeHead(status, { 'content-type': 'application/json', 'cache-control': 'no-store' }); response.end(JSON.stringify(value)) }
function isAuthorized(request) {
  const supplied = request.headers['x-pathway-runner-key']
  if (!sharedSecret || typeof supplied !== 'string') return false
  const left = Buffer.from(sharedSecret)
  const right = Buffer.from(supplied)
  return left.length === right.length && crypto.timingSafeEqual(left, right)
}
async function readBody(request) {
  const parts = []; let size = 0
  for await (const part of request) { size += part.length; if (size > 60_000) throw new Error('Request too large.'); parts.push(part) }
  return JSON.parse(Buffer.concat(parts).toString('utf8'))
}

/** Vercel injects a short-lived, project-scoped OIDC token into every request to a function. */
function sandboxToken(request) {
  const oidc = request.headers['x-vercel-oidc-token']
  return accessToken || (typeof oidc === 'string' ? oidc : undefined)
}

async function createSandbox(token, runtime) {
  return Sandbox.create({
    teamId,
    projectId,
    token,
    image: sandboxImageFor(runtime, imageTag),
    networkPolicy: 'deny-all',
    resources: { vcpus: 1 },
    timeout: SANDBOX_TIMEOUT_MS,
  })
}

async function run(sandbox, command, timeoutMs, stdin) {
  const [cmd, ...args] = command
  const signal = AbortSignal.timeout(timeoutMs)
  try {
    const result = await sandbox.runCommand({ cmd, args, cwd: '/workspace', signal, ...(stdin === undefined ? {} : { stdin: Readable.from([stdin]) }) })
    const [stdout, stderr] = await Promise.all([result.stdout(), result.stderr()])
    return { exitCode: result.exitCode, stdout, stderr }
  } catch (error) {
    if (signal.aborted) return { exitCode: 124, stdout: '', stderr: `Time limit exceeded (${timeoutMs / 1000} seconds).` }
    throw error
  }
}

async function writeWorkspace(sandbox, files) {
  await sandbox.writeFiles(Object.entries(files).map(([path, content]) => ({ path: `/workspace/${path}`, content: Buffer.from(content, 'utf8') })))
}

async function evaluate(payload, token) {
  const fixture = fixtureFor(payload.lessonSlug, payload.code)
  if (!fixture) return { passed: false, passingTests: 0, totalTests: 0, feedback: 'This exercise has no isolated test fixture yet.', nextLessonSlug: null }
  const sandbox = await createSandbox(token, fixture.runtime)
  try {
    await writeWorkspace(sandbox, fixture.files)
    const { exitCode, stdout, stderr } = await run(sandbox, fixture.command, EXERCISE_TIMEOUT_MS)
    return evaluationResult(fixture, exitCode, boundedOutput(stdout, stderr))
  } finally {
    await sandbox.stop().catch(() => undefined)
  }
}

async function evaluateHook(payload, token) {
  const input = hookInputFor(payload.command)
  const inputJson = JSON.stringify(input)
  if (!hookMatcherMatches(payload.matcher, 'Bash')) return interpretHookResult(inputJson, false, null, '', '')

  const sandbox = await createSandbox(token, 'hook')
  try {
    await writeWorkspace(sandbox, { 'hook.sh': payload.script })
    const { exitCode, stdout, stderr } = await run(sandbox, ['/bin/bash', '/workspace/hook.sh'], HOOK_TIMEOUT_MS, inputJson)
    return interpretHookResult(inputJson, true, exitCode, stdout.slice(0, 8_000), stderr.slice(0, 8_000))
  } finally {
    await sandbox.stop().catch(() => undefined)
  }
}

http.createServer(async (request, response) => {
  const path = requestPath(request.url)
  if (request.method === 'GET' && path === '/health')
    return reply(response, configured ? 200 : 503, { status: configured ? 'ok' : 'misconfigured', provider: 'vercel-sandbox' })

  if (request.method !== 'POST' || !['/evaluate', '/evaluate-hook'].includes(path))
    return reply(response, 404, { message: 'Not found.' })

  if (!isAuthorized(request)) return reply(response, 401, { message: 'Unauthorized.' })
  if (!configured) return reply(response, 503, { message: 'The isolated evaluator is not configured.' })
  const token = sandboxToken(request)
  if (!token) return reply(response, 503, { message: 'The isolated evaluator has no Vercel credentials.' })

  try {
    const payload = await readBody(request)
    if (path === '/evaluate-hook') {
      const issue = validateHookRequest(payload)
      if (issue) return reply(response, 400, { message: issue })
      return reply(response, 200, await evaluateHook(payload, token))
    }

    const issue = validateRequest(payload)
    if (issue) return reply(response, 400, { message: issue })
    return reply(response, 200, await evaluate(payload, token))
  } catch (error) {
    console.error('Sandbox evaluation failed.', error)
    return reply(response, 503, { message: 'The isolated evaluator is temporarily unavailable.' })
  }
}).listen(port, host, () => console.log(`Sandbox evaluator broker listening on ${host}:${port}`))
