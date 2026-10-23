"""Checks Kubernetes exercise submissions inside the isolated evaluator sandbox.

No cluster is available in the sandbox, so manifests are parsed with a real YAML parser and
checked semantically (object kind and API group, selectors that actually match template
labels, port mapping, resource quantities) and kubectl sequences are tokenized and checked
for intent. Prints one line per check and PATHWAY_TEST_PASS only when every check passes.

Usage: python3 kube_check.py <lesson-slug> <submission-file>
"""
import re
import shlex
import sys

import yaml


class Report:
    def __init__(self):
        self.failures = 0

    def check(self, ok, message, hint=""):
        print(("PASS  " if ok else "FAIL  ") + message + ("" if ok or not hint else f" — {hint}"))
        if not ok:
            self.failures += 1
        return ok


# ---------------------------------------------------------------- YAML helpers

def load_documents(text, report):
    try:
        docs = [doc for doc in yaml.safe_load_all(text) if doc is not None]
    except yaml.YAMLError as error:
        mark = getattr(error, "problem_mark", None)
        where = f" (line {mark.line + 1}, column {mark.column + 1})" if mark else ""
        report.check(False, f"YAML parses{where}", "check indentation: nested keys need consistent spaces, never tabs")
        return None
    if not docs or not all(isinstance(doc, dict) for doc in docs):
        report.check(False, "YAML contains a mapping", "start with key: value pairs such as apiVersion: v1")
        return None
    return docs


def find_kind(docs, kind):
    return next((doc for doc in docs if str(doc.get("kind", "")).lower() == kind.lower()), None)


def mapping(value):
    return value if isinstance(value, dict) else {}


def sequence(value):
    return value if isinstance(value, list) else []


def labels_str(value):
    return {str(key): str(val) for key, val in mapping(value).items()}


# ---------------------------------------------------------------- quantities

_BINARY = {"Ki": 2**10, "Mi": 2**20, "Gi": 2**30, "Ti": 2**40}
_DECIMAL = {"k": 10**3, "M": 10**6, "G": 10**9, "T": 10**12}


def memory_bytes(value):
    match = re.fullmatch(r"\s*([0-9]+(?:\.[0-9]+)?)\s*(Ki|Mi|Gi|Ti|k|M|G|T)?\s*", str(value))
    if not match:
        return None
    number, suffix = float(match.group(1)), match.group(2)
    return number * (_BINARY.get(suffix) or _DECIMAL.get(suffix) or 1)


def cpu_millicores(value):
    match = re.fullmatch(r"\s*([0-9]+(?:\.[0-9]+)?)\s*(m)?\s*", str(value))
    if not match:
        return None
    number = float(match.group(1))
    return number if match.group(2) else number * 1000


# ---------------------------------------------------------------- kubectl helpers

def kubectl_commands(text):
    commands = []
    for raw in text.splitlines():
        line = raw.strip()
        if line.startswith("$"):
            line = line[1:].strip()
        if not line or line.startswith("#"):
            continue
        try:
            tokens = shlex.split(line, comments=True)
        except ValueError:
            continue
        if tokens and tokens[0] == "kubectl":
            commands.append(tokens[1:])
    return commands


def namespace_of(args):
    for index, token in enumerate(args):
        if token in ("-n", "--namespace") and index + 1 < len(args):
            return args[index + 1]
        if token.startswith("--namespace="):
            return token.split("=", 1)[1]
        if token.startswith("-n") and len(token) > 2:
            return token[2:]
    return None


def output_format(args):
    for index, token in enumerate(args):
        if token in ("-o", "--output") and index + 1 < len(args):
            return args[index + 1]
        if token.startswith("--output="):
            return token.split("=", 1)[1]
        if token.startswith("-o") and len(token) > 2:
            return token[2:]
    return None


def positional(args):
    """Arguments that are not flags or flag values."""
    values, skip = [], False
    for token in args:
        if skip:
            skip = False
            continue
        if token in ("-n", "--namespace", "-o", "--output", "-c", "--container", "-l", "--selector"):
            skip = True
            continue
        if token.startswith("-"):
            continue
        values.append(token)
    return values


def targets_pod(args, verb, name):
    words = positional(args)
    if not words or words[0] != verb:
        return False
    rest = words[1:]
    pod_words = ("pod", "pods", "po")
    # kubectl accepts TYPE/NAME with any alias of the pod resource.
    if any(f"{alias}/{name}" in rest for alias in pod_words):
        return True
    if verb == "logs":
        return name in rest
    return len(rest) >= 2 and rest[0] in pod_words and name in rest[1:]


# ---------------------------------------------------------------- lessons

def pod_manifest(text, report):
    docs = load_documents(text, report)
    if docs is None:
        return
    pod = find_kind(docs, "Pod")
    if not report.check(pod is not None, "manifest declares kind: Pod"):
        return
    report.check(pod.get("apiVersion") == "v1", "Pod uses apiVersion: v1", "Pods live in the core API group, written as v1")
    report.check(mapping(pod.get("metadata")).get("name") == "web", "Pod is named web", "set metadata.name")
    containers = sequence(mapping(pod.get("spec")).get("containers"))
    web = next((c for c in containers if mapping(c).get("name") == "web"), None)
    report.check(web is not None, "a container is named web", "spec.containers is a list; each item needs a name")
    report.check(web is not None and web.get("image") == "nginx:1.29", "container image is nginx:1.29", "pin the exact tag")


def labelled_metadata(text, report):
    docs = load_documents(text, report)
    if docs is None:
        return
    doc = docs[0]
    metadata = mapping(doc.get("metadata")) if "metadata" in doc else doc
    labels = labels_str(metadata.get("labels"))
    report.check(metadata.get("name") == "orders", "metadata.name is orders")
    report.check(labels.get("app") == "orders", "label app=orders exists", "labels is a nested mapping under metadata")
    report.check(labels.get("tier") == "api", "label tier=api exists")


def deployment(text, report):
    docs = load_documents(text, report)
    if docs is None:
        return
    deploy = find_kind(docs, "Deployment")
    if not report.check(deploy is not None, "manifest declares kind: Deployment"):
        return
    report.check(deploy.get("apiVersion") == "apps/v1", "Deployment uses apiVersion: apps/v1", "Deployments belong to the apps API group")
    report.check(mapping(deploy.get("metadata")).get("name") == "orders", "Deployment named orders")
    spec = mapping(deploy.get("spec"))
    report.check(spec.get("replicas") == 3, "replicas is 3", "use the number 3, not a string")
    match_labels = labels_str(mapping(spec.get("selector")).get("matchLabels"))
    template_labels = labels_str(mapping(mapping(spec.get("template")).get("metadata")).get("labels"))
    report.check(
        match_labels.get("app") == "orders" and bool(match_labels) and match_labels.items() <= template_labels.items(),
        "selector.matchLabels selects app=orders and matches the Pod template labels",
        "the API server rejects a Deployment whose selector does not match its template",
    )
    containers = sequence(mapping(mapping(spec.get("template")).get("spec")).get("containers"))
    images = [mapping(c).get("image") for c in containers]
    report.check("ghcr.io/example/orders:2.0" in images, "image is the versioned ghcr.io/example/orders:2.0", "avoid :latest so rollouts are reproducible")


def service(text, report):
    docs = load_documents(text, report)
    if docs is None:
        return
    svc = find_kind(docs, "Service")
    if not report.check(svc is not None, "manifest declares kind: Service"):
        return
    spec = mapping(svc.get("spec"))
    report.check(mapping(svc.get("metadata")).get("name") == "orders", "Service named orders")
    report.check(spec.get("type", "ClusterIP") == "ClusterIP", "Service type is ClusterIP (the default)")
    report.check(labels_str(spec.get("selector")).get("app") == "orders", "selects Pods labelled app=orders", "Services use spec.selector directly, not matchLabels")
    ports = [mapping(p) for p in sequence(spec.get("ports"))]
    report.check(any(str(p.get("port")) == "80" for p in ports), "serves port 80")
    report.check(any(str(p.get("port")) == "80" and str(p.get("targetPort")) == "8080" for p in ports), "forwards port 80 to targetPort 8080", "targetPort is the container's port")


def find_resources(node):
    if isinstance(node, dict):
        if "requests" in node or "limits" in node:
            return node
        for key, value in node.items():
            found = find_resources(value)
            if found is not None:
                return found
    if isinstance(node, list):
        for item in node:
            found = find_resources(item)
            if found is not None:
                return found
    return None


def resources(text, report):
    docs = load_documents(text, report)
    if docs is None:
        return
    block = find_resources(docs[0])
    if not report.check(block is not None, "a resources block with requests and limits exists"):
        return
    requests, limits = mapping(block.get("requests")), mapping(block.get("limits"))
    report.check(cpu_millicores(requests.get("cpu")) == 250, "CPU request is 250m", "250m means a quarter of one CPU")
    request_memory, limit_memory = memory_bytes(requests.get("memory")), memory_bytes(limits.get("memory"))
    report.check(request_memory == 256 * 2**20, "memory request is 256Mi", "Mi is mebibytes; M would be megabytes")
    report.check(limit_memory == 512 * 2**20, "memory limit is 512Mi")
    if request_memory and limit_memory:
        report.check(limit_memory >= request_memory, "memory limit is not below the request")


def contexts(text, report):
    commands = kubectl_commands(text)
    report.check(any(positional(c)[:2] == ["config", "current-context"] for c in commands), "prints the current context", "kubectl config current-context")
    report.check(
        any(positional(c)[:2] in (["get", "pods"], ["get", "pod"], ["get", "po"]) and namespace_of(c) == "payments" for c in commands),
        "lists Pods in Namespace payments",
        "add -n payments to kubectl get pods",
    )


def debugging(text, report):
    commands = kubectl_commands(text)
    mutating = [c for c in commands if positional(c)[:1] and positional(c)[0] in ("delete", "apply", "edit", "patch", "replace", "scale")]
    report.check(not mutating, "the sequence only reads cluster state", "inspect before changing anything")
    get_wide = [c for c in commands if targets_pod(c, "get", "orders-abc") and output_format(c) == "wide"]
    describe = [c for c in commands if targets_pod(c, "describe", "orders-abc")]
    previous = [c for c in commands if targets_pod(c, "logs", "orders-abc") and ("--previous" in c or "-p" in c or "--previous=true" in c)]
    report.check(bool(get_wide), "gets Pod orders-abc with -o wide")
    report.check(bool(describe), "describes Pod orders-abc", "kubectl describe pod orders-abc")
    report.check(bool(previous), "reads the previous container's logs", "kubectl logs orders-abc --previous")
    relevant = get_wide + describe + previous
    report.check(bool(relevant) and all(namespace_of(c) == "payments" for c in relevant), "every command targets Namespace payments", "use -n payments on each command")


LESSONS = {
    "k8s-yaml-api-objects": pod_manifest,
    "k8s-labels-selectors": labelled_metadata,
    "k8s-deployments-replicasets": deployment,
    "k8s-services": service,
    "k8s-resources-qos": resources,
    "k8s-kubectl-contexts": contexts,
    "k8s-debugging": debugging,
}


def main():
    slug, path = sys.argv[1], sys.argv[2]
    with open(path, encoding="utf-8") as handle:
        text = handle.read()
    report = Report()
    LESSONS[slug](text, report)
    if report.failures:
        sys.exit(1)
    print("PATHWAY_TEST_PASS")


if __name__ == "__main__":
    main()
