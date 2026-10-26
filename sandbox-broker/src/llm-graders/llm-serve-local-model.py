# tests: 4
import json


def sse(*deltas, finish=None):
    lines = [": keep-alive", ""]
    for delta in deltas:
        lines.append("data: " + json.dumps({"choices": [{"index": 0, "delta": delta, "finish_reason": None}]}))
        lines.append("")
    lines.append("data: " + json.dumps({"choices": [{"index": 0, "delta": {}, "finish_reason": finish}]}))
    lines.append("")
    lines.append("data: [DONE]")
    lines.append("")
    lines.append("data: " + json.dumps({"choices": [{"index": 0, "delta": {"content": "AFTER DONE"}, "finish_reason": None}]}))
    return "\n".join(lines)


def main():
    ns = load()
    build, parse_sse, assemble = (need(ns, n) for n in ("build_request", "parse_sse", "assemble"))

    messages = [{"role": "user", "content": "hi"}]
    request = call(build, "my-model", messages)
    check(request == {"model": "my-model", "messages": messages, "temperature": 0.0, "stream": True}, f"build_request without tools should be exactly model, messages, temperature and stream; got {show(request, 300)}.")
    tool = {"name": "read_file", "description": "Read a file", "parameters": {"type": "object", "properties": {"path": {"type": "string"}}, "required": ["path"]}}
    request = call(build, "my-model", messages, tools=[tool], temperature=0.2, stream=False)
    check(request.get("tools") == [{"type": "function", "function": tool}] and request["temperature"] == 0.2 and request["stream"] is False, "With tools, wrap each as {'type': 'function', 'function': tool} and pass the other settings through.")
    test("building requests")

    text = sse({"role": "assistant", "content": ""}, {"content": "Hel"}, {"content": "lo"}, finish="stop")
    chunks = call(parse_sse, text)
    check(isinstance(chunks, list) and len(chunks) == 4, f"parse_sse should return the 4 data chunks before [DONE] and skip comments and blank lines; got {len(chunks) if isinstance(chunks, list) else show(chunks)}.")
    check(all(isinstance(c, dict) for c in chunks), "Each chunk should be the parsed JSON object.")
    test("parsing the event stream")

    reply = call(assemble, chunks)
    check(reply.get("content") == "Hello" and reply.get("tool_calls") == [] and reply.get("finish_reason") == "stop", f"A text stream should assemble to content 'Hello', no tool calls, finish_reason 'stop'; got {show(reply, 300)}.")
    stream = sse(
        {"role": "assistant", "content": ""},
        {"tool_calls": [{"index": 0, "id": "call_1", "type": "function", "function": {"name": "read_file", "arguments": ""}}]},
        {"tool_calls": [{"index": 0, "function": {"arguments": "{\"path\": "}}]},
        {"tool_calls": [{"index": 1, "id": "call_2", "type": "function", "function": {"name": "list_dir", "arguments": "{\"dir\""}}]},
        {"tool_calls": [{"index": 0, "function": {"arguments": "\"a.txt\"}"}}]},
        {"tool_calls": [{"index": 1, "function": {"arguments": ": \"src\"}"}}]},
        finish="tool_calls",
    )
    chunks = call(parse_sse, stream)
    try:
        reply = assemble(chunks)
    except ValueError as error:
        fail(f"Interleaved fragments should join per index into two tool calls, but assembling them raised ValueError ({error}). Append each arguments fragment to its own index's list rather than replacing it.")
    except Exception as error:  # noqa: BLE001
        fail(f"assemble raised {type(error).__name__}: {error}")
    check(reply.get("content") == "", "No content deltas here, so content should be ''.")
    check(reply.get("tool_calls") == [
        {"id": "call_1", "name": "read_file", "arguments": {"path": "a.txt"}},
        {"id": "call_2", "name": "list_dir", "arguments": {"dir": "src"}},
    ], f"Interleaved fragments should join per index into two tool calls with parsed arguments; got {show(reply.get('tool_calls'), 300)}.")
    check(reply.get("finish_reason") == "tool_calls", "finish_reason should be the last non-null one: 'tool_calls'.")
    test("joining content and tool-call fragments")

    broken = sse({"tool_calls": [{"index": 0, "id": "call_9", "function": {"name": "read_file", "arguments": "{\"path\": "}}]}, finish="tool_calls")
    chunks = call(parse_sse, broken)
    check(raises(assemble, ValueError, chunks), "Unparseable tool arguments should raise ValueError.")
    try:
        assemble(chunks)
    except ValueError as error:
        check("call_9" in str(error), "The ValueError should name the tool call's id (call_9) so the problem can be traced.")
    except Exception as error:  # noqa: BLE001
        fail(f"assemble raised {type(error).__name__} instead of ValueError for broken arguments.")
    test("reporting broken tool arguments")


run(main)
