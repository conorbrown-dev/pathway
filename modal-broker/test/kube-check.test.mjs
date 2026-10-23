// Runs the sandbox checker locally against known-good and known-bad answers.
// CI installs PyYAML for the broker job; elsewhere these tests skip when it is missing.
import test from 'node:test'
import assert from 'node:assert/strict'
import { spawnSync } from 'node:child_process'
import { mkdtempSync, writeFileSync } from 'node:fs'
import { tmpdir } from 'node:os'
import { join } from 'node:path'
import { fileURLToPath } from 'node:url'

const checker = fileURLToPath(new URL('../src/kube_check.py', import.meta.url))
const hasYaml = spawnSync('python3', ['-c', 'import yaml']).status === 0
const dir = mkdtempSync(join(tmpdir(), 'kube-check-'))

// CI sets PATHWAY_REQUIRE_PYYAML so a missing parser fails loudly instead of skipping.
test('PyYAML is available when required', { skip: !process.env.PATHWAY_REQUIRE_PYYAML }, () => assert.ok(hasYaml, 'python3 cannot import yaml'))

function run(slug, text) {
  const file = join(dir, `${slug}-${Math.random().toString(36).slice(2)}.txt`)
  writeFileSync(file, text)
  const result = spawnSync('python3', ['-I', checker, slug, file], { encoding: 'utf8' })
  return { passed: result.status === 0 && result.stdout.includes('PATHWAY_TEST_PASS'), output: result.stdout + result.stderr }
}

const cases = [
  ['k8s-yaml-api-objects', true, 'apiVersion: v1\nkind: Pod\nmetadata:\n  name: web\nspec:\n  containers:\n    - name: web\n      image: nginx:1.29\n'],
  ['k8s-yaml-api-objects', false, 'apiVersion: v1\nkind: Pod\nmetadata:\n  name: web\nspec:\n  containers:\n    name: web\n    image: nginx:1.29\n'],
  ['k8s-yaml-api-objects', false, '# kind: Pod name: web nginx:1.29\n'],
  ['k8s-labels-selectors', true, 'metadata:\n  name: orders\n  labels:\n    app: orders\n    tier: api\n'],
  ['k8s-labels-selectors', false, 'metadata:\n  name: orders\nlabels:\n  app: orders\n  tier: api\n'],
  ['k8s-deployments-replicasets', true, 'apiVersion: apps/v1\nkind: Deployment\nmetadata:\n  name: orders\nspec:\n  replicas: 3\n  selector:\n    matchLabels:\n      app: orders\n  template:\n    metadata:\n      labels:\n        app: orders\n    spec:\n      containers:\n        - name: orders\n          image: ghcr.io/example/orders:2.0\n'],
  ['k8s-deployments-replicasets', false, 'apiVersion: apps/v1\nkind: Deployment\nmetadata:\n  name: orders\nspec:\n  replicas: 3\n  selector:\n    matchLabels:\n      app: orders\n  template:\n    metadata:\n      labels:\n        app: order\n    spec:\n      containers:\n        - name: orders\n          image: ghcr.io/example/orders:2.0\n'],
  ['k8s-services', true, 'apiVersion: v1\nkind: Service\nmetadata:\n  name: orders\nspec:\n  selector:\n    app: orders\n  ports:\n    - port: 80\n      targetPort: 8080\n'],
  ['k8s-services', false, 'apiVersion: v1\nkind: Service\nmetadata:\n  name: orders\nspec:\n  selector:\n    app: orders\n  ports:\n    - port: 8080\n      targetPort: 80\n'],
  ['k8s-resources-qos', true, 'resources:\n  requests:\n    cpu: 250m\n    memory: 256Mi\n  limits:\n    memory: 512Mi\n'],
  ['k8s-resources-qos', false, 'resources:\n  requests:\n    cpu: 250m\n    memory: 256M\n  limits:\n    memory: 512Mi\n'],
  ['k8s-kubectl-contexts', true, 'kubectl config current-context\nkubectl get pods -n payments\n'],
  ['k8s-kubectl-contexts', false, 'kubectl config current-context\n# kubectl get pods -n payments\nkubectl get pods\n'],
  ['k8s-debugging', true, 'kubectl -n payments get pods/orders-abc -owide\nkubectl -n payments describe pod/orders-abc\nkubectl -n payments logs pod/orders-abc -p\n'],
  ['k8s-debugging', false, 'kubectl get pod orders-abc -n payments -o wide\nkubectl describe pod orders-abc -n payments\nkubectl logs orders-abc -n payments --previous\nkubectl delete pod orders-abc -n payments\n'],
]

for (const [slug, expected, text] of cases)
  test(`${slug} ${expected ? 'accepts a correct answer' : 'rejects a common mistake'}`, { skip: !hasYaml && 'PyYAML not installed' }, () => {
    const { passed, output } = run(slug, text)
    assert.equal(passed, expected, output)
  })
