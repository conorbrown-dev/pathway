# tests: 4
# The answer is a JSON config, not Python, so this grader reads the file instead of running it.
import json
import re
from urllib.parse import urlparse

OPENAI_COMPATIBLE = ("@opencode/ai/providers/openai-compatible", "@ai-sdk/openai-compatible")


def strip_comments(text):
    out = []
    for line in text.splitlines():
        if line.strip().startswith("//"):
            continue
        out.append(line)
    return "\n".join(out)


def main():
    with open(SUBMISSION, encoding="utf-8") as handle:
        raw = handle.read()
    try:
        config = json.loads(re.sub(r",(\s*[}\]])", r"\1", strip_comments(raw)))
    except json.JSONDecodeError as error:
        fail(f"Your config isn't valid JSON: {error.msg} on line {error.lineno}. Comment lines starting with // are allowed; other text isn't.")
    check(isinstance(config, dict), "The config should be a JSON object.")

    primary = config.get("model")
    check(isinstance(primary, str) and re.match(r"^(anthropic|openai)/\S+$", primary or "") is not None, f"Set a top-level \"model\" to an anthropic/… or openai/… model so a frontier model is the primary agent; got {show(primary)}.")
    test("the frontier model is primary")

    v2 = "providers" in config or "agents" in config
    providers = config.get("providers" if v2 else "provider")
    check(isinstance(providers, dict) and providers, "Add a custom provider under \"providers\" (opencode 2.x) or \"provider\" (1.x).")
    local_models = {}
    for provider_id, provider in providers.items():
        if not isinstance(provider, dict):
            continue
        package = provider.get("package" if v2 else "npm")
        settings = provider.get("settings" if v2 else "options") or {}
        base_url = settings.get("baseURL", "") if isinstance(settings, dict) else ""
        parsed = urlparse(base_url) if isinstance(base_url, str) else None
        if package in OPENAI_COMPATIBLE and parsed and parsed.hostname in ("127.0.0.1", "localhost") and parsed.path.rstrip("/").endswith("/v1"):
            for model_id, model in (provider.get("models") or {}).items():
                limit = (model or {}).get("limit") or {}
                if isinstance(limit.get("context"), (int, float)) and isinstance(limit.get("output"), (int, float)) and not isinstance(limit.get("context"), bool):
                    local_models[f"{provider_id}/{model_id}"] = model
    key_names = "package / settings.baseURL" if v2 else "npm / options.baseURL"
    check(local_models, f"Add a provider with the OpenAI-compatible package ({key_names}) whose baseURL is http://127.0.0.1:<port>/v1 or http://localhost:<port>/v1, and at least one model with numeric limit.context and limit.output.")
    test("a local OpenAI-compatible provider with limits")

    agents = config.get("agents" if v2 else "agent")
    check(isinstance(agents, dict) and agents, "Add your subagent under \"agents\" (2.x) or \"agent\" (1.x).")
    specialists = {name: a for name, a in agents.items() if isinstance(a, dict) and a.get("mode") == "subagent" and a.get("model") in local_models}
    check(specialists, f"Add an agent with \"mode\": \"subagent\" and \"model\" set to one of your local models: {', '.join(sorted(local_models))}.")
    name, agent = next(iter(specialists.items()))
    description = agent.get("description")
    check(isinstance(description, str) and len(description.strip()) >= 40, f"Give the {name} subagent a description of at least 40 characters that says what it does and when to use it; the primary agent reads it to decide when to delegate.")
    test("a well-described subagent on your model")

    if v2:
        rules = agent.get("permissions")
        check(isinstance(rules, list) and rules and isinstance(rules[0], dict), f"Give {name} a \"permissions\" list of rules.")
        first = rules[0]
        check(first.get("action") == "*" and first.get("effect") == "deny", "Start the permissions list with { \"action\": \"*\", \"resource\": \"*\", \"effect\": \"deny\" }, then allow only what the job needs.")
        for rule in rules[1:]:
            check(not (rule.get("action") in ("edit", "shell", "webfetch", "*") and rule.get("effect") in ("allow", "ask")), f"The subagent shouldn't be allowed to {rule.get('action')}; its job doesn't need it.")
    else:
        permission = agent.get("permission")
        check(isinstance(permission, dict), f"Give {name} a \"permission\" object.")
        for action in ("edit", "bash", "webfetch"):
            check(permission.get(action) == "deny", f"Set \"{action}\": \"deny\" in {name}'s permission object; its job doesn't need it.")
    test("least-privilege permissions")


run(main)
