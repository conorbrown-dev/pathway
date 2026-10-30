# expect: a boolean where an integer is required
TYPES = {"string": str, "integer": int, "number": (int, float), "boolean": bool}


def validate_arguments(schema, arguments):
    errors = []
    properties = schema.get("properties", {})
    for name in schema.get("required", []):
        if name not in arguments:
            errors.append(f"missing required argument {name!r}")
    for name, value in arguments.items():
        spec = properties.get(name)
        if spec is None:
            errors.append(f"unexpected argument {name!r}")
            continue
        expected = spec.get("type")
        if expected in TYPES and not isinstance(value, TYPES[expected]):
            errors.append(f"{name!r} must be a {expected}")
        if "enum" in spec and value not in spec["enum"]:
            errors.append(f"{name!r} must be one of {spec['enum']}")
    return errors


def _run_tool(tools, tool_call):
    tool = tools.get(tool_call.get("name"))
    if tool is None:
        return f"Error: unknown tool {tool_call.get('name')!r}"
    arguments = tool_call.get("arguments") or {}
    problems = validate_arguments(tool["schema"], arguments)
    if problems:
        return "Error: invalid arguments: " + "; ".join(problems)
    try:
        return str(tool["function"](**arguments))
    except Exception as error:  # noqa: BLE001 - the model should see the failure
        return f"Error: {error}"


def run_agent(model, tools, user_message, max_steps):
    messages = [{"role": "user", "content": user_message}]
    for _ in range(max_steps):
        reply = model(messages)
        if not reply.get("tool_calls"):
            messages.append({"role": "assistant", "content": reply.get("content")})
            return reply.get("content"), messages
        messages.append({"role": "assistant", "tool_calls": reply["tool_calls"]})
        for tool_call in reply["tool_calls"]:
            messages.append({"role": "tool", "tool_call_id": tool_call.get("id"), "content": _run_tool(tools, tool_call)})
    return None, messages
