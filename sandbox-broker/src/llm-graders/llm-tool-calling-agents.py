# tests: 4
SCHEMA = {
    "type": "object",
    "properties": {"path": {"type": "string"}, "max_lines": {"type": "integer"}, "mode": {"type": "string", "enum": ["text", "hex"]}, "ratio": {"type": "number"}, "follow": {"type": "boolean"}},
    "required": ["path"],
}


def main():
    ns = load()
    validate, run_agent = (need(ns, n) for n in ("validate_arguments", "run_agent"))

    check(call(validate, SCHEMA, {"path": "a.txt"}) == [], "A call with only the required argument should be valid.")
    check(call(validate, SCHEMA, {"path": "a", "max_lines": 5, "mode": "hex", "ratio": 0.5, "follow": False}) == [], "All arguments with the right types should be valid.")
    check(call(validate, SCHEMA, {"path": "a", "ratio": 2}) == [], "An integer is a valid number.")
    bad = [
        ({}, "a missing required argument"),
        ({"path": 3}, "a number where a string is required"),
        ({"path": "a", "max_lines": "5"}, "a string where an integer is required"),
        ({"path": "a", "max_lines": True}, "a boolean where an integer is required (True is an int in Python, but not in JSON Schema)"),
        ({"path": "a", "max_lines": 2.5}, "a fractional number where an integer is required"),
        ({"path": "a", "mode": "binary"}, "a value outside the enum"),
        ({"path": "a", "colour": "red"}, "an argument the schema doesn't define"),
        ({"path": "a", "follow": "yes"}, "a string where a boolean is required"),
    ]
    for arguments, label in bad:
        errors = call(validate, SCHEMA, arguments)
        check(isinstance(errors, list) and len(errors) >= 1, f"validate_arguments should report an error for {label}: {arguments}.")
    test("argument validation")

    def read_file(path, max_lines=None, mode=None, ratio=None, follow=None):
        if path == "missing":
            raise FileNotFoundError(f"No such file: {path}")
        return f"contents of {path}"

    tools = {
        "read_file": {"schema": SCHEMA, "function": read_file},
        "add": {"schema": {"type": "object", "properties": {"a": {"type": "number"}, "b": {"type": "number"}}, "required": ["a", "b"]}, "function": lambda a, b: a + b},
    }

    def scripted(replies):
        seen = []

        def model(messages):
            seen.append([dict(m) for m in messages])
            return replies[len(seen) - 1]
        return model, seen

    model, seen = scripted([
        {"tool_calls": [{"id": "c1", "name": "add", "arguments": {"a": 2, "b": 3}}]},
        {"content": "The answer is 5."},
    ])
    final, messages = call(run_agent, model, tools, "What is 2 + 3?", 5)
    check(final == "The answer is 5.", f"run_agent should return the model's final text; got {show(final)}.")
    check(messages[0] == {"role": "user", "content": "What is 2 + 3?"}, "The first message should be the user's message.")
    tool_messages = [m for m in messages if m.get("role") == "tool"]
    check(len(tool_messages) == 1 and tool_messages[0].get("tool_call_id") == "c1" and str(tool_messages[0].get("content")) == "5", f"After running add(2, 3), append a tool message with tool_call_id 'c1' and content '5'; got {show(tool_messages)}.")
    check(any(m.get("tool_call_id") == "c1" for m in seen[1]), "The model's second call should see the tool result.")
    test("a loop that calls tools and returns the answer")

    model, seen = scripted([
        {"tool_calls": [{"id": "a", "name": "read_file", "arguments": {"path": "missing"}}, {"id": "b", "name": "delete_everything", "arguments": {}}, {"id": "c", "name": "read_file", "arguments": {"path": 7}}]},
        {"content": "done"},
    ])
    try:
        final, messages = call(run_agent, model, tools, "go", 5)
    except GraderFailure:
        fail("run_agent crashed when a tool raised an exception. Catch it and return 'Error: ...' as the tool result.")
    results = {m.get("tool_call_id"): str(m.get("content")) for m in messages if m.get("role") == "tool"}
    check(set(results) == {"a", "b", "c"}, f"Every tool call needs a tool result message, even failures; got results for {sorted(results)}.")
    check(results["a"].startswith("Error:") and "No such file" in results["a"], "A tool that raises should produce a result starting with 'Error:' that includes the exception's message.")
    check(results["b"].startswith("Error:"), "An unknown tool should produce an 'Error:' result, not run anything.")
    check(results["c"].startswith("Error:"), "Invalid arguments should produce an 'Error:' result without calling the tool.")
    check(final == "done", "After errors, the loop should continue and return the final answer.")
    test("errors are reported back to the model")

    looping = lambda messages: {"tool_calls": [{"id": f"x{len(messages)}", "name": "add", "arguments": {"a": 1, "b": 1}}]}
    count = {"n": 0}

    def counted(messages):
        count["n"] += 1
        return looping(messages)
    final, messages = call(run_agent, counted, tools, "loop forever", 3)
    check(final is None, "When the step limit is reached without a final answer, return None as the final text.")
    check(count["n"] == 3, f"With max_steps 3, the model should be called exactly 3 times; it was called {count['n']} times.")
    test("the step limit stops runaway agents")


run(main)
