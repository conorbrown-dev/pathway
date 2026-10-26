import posixpath
import re
from urllib.parse import urlparse

DESTRUCTIVE = [
    r"\brm\s+-[a-zA-Z]*r[a-zA-Z]*f|\brm\s+-[a-zA-Z]*f[a-zA-Z]*r",
    r"\|\s*(ba)?sh\b",
    r"\bmkfs",
    r"\bdd\s+if=",
    r"\bchmod\s+-R\s+777",
    r">\s*/dev/sd",
]


def _inside(workspace, path):
    resolved = posixpath.normpath(posixpath.join(workspace, path))
    return resolved == workspace or resolved.startswith(workspace + "/")


def decide(call, workspace, allowed_domains, tainted):
    tool = call.get("tool")
    arguments = call.get("arguments", {})
    if tool in ("read", "write"):
        if not _inside(workspace, arguments.get("path", "")):
            return "deny"
        return "allow" if tool == "read" else "ask"
    if tool == "fetch":
        host = urlparse(arguments.get("url", "")).hostname
        if host not in allowed_domains:
            return "deny"
        return "ask" if tainted else "allow"
    if tool == "send":
        return "deny" if tainted else "ask"
    if tool == "shell":
        command = arguments.get("command", "")
        if any(re.search(pattern, command) for pattern in DESTRUCTIVE):
            return "deny"
        return "ask"
    return "deny"


SECRETS = re.compile(r"sk-[A-Za-z0-9_-]{20,}|ghp_[A-Za-z0-9]{36}|AKIA[A-Z0-9]{16}")


def redact_secrets(text):
    return SECRETS.sub("[REDACTED]", text)
