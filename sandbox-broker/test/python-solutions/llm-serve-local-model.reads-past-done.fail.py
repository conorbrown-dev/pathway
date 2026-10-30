# expect: parse_sse should return the 4 data chunks before [DONE]
import json


def build_request(model, messages, tools=None, temperature=0.0, stream=True):
    request = {"model": model, "messages": messages, "temperature": temperature, "stream": stream}
    if tools:
        request["tools"] = [{"type": "function", "function": tool} for tool in tools]
    return request


def parse_sse(text):
    chunks = []
    for line in text.splitlines():
        if not line.startswith("data: "):
            continue
        payload = line[len("data: "):]
        if payload.strip() == "[DONE]":
            continue
        chunks.append(json.loads(payload))
    return chunks


def assemble(chunks):
    content, calls, finish = [], {}, None
    for chunk in chunks:
        choice = chunk["choices"][0]
        delta = choice.get("delta", {}) or {}
        if delta.get("content"):
            content.append(delta["content"])
        for fragment in delta.get("tool_calls") or []:
            slot = calls.setdefault(fragment["index"], {"id": None, "name": None, "arguments": []})
            if fragment.get("id"):
                slot["id"] = fragment["id"]
            function = fragment.get("function") or {}
            if function.get("name"):
                slot["name"] = function["name"]
            if function.get("arguments"):
                slot["arguments"].append(function["arguments"])
        if choice.get("finish_reason"):
            finish = choice["finish_reason"]
    tool_calls = []
    for index in sorted(calls):
        slot = calls[index]
        text = "".join(slot["arguments"]) or "{}"
        try:
            arguments = json.loads(text)
        except json.JSONDecodeError:
            raise ValueError(f"tool call {slot['id']} has invalid JSON arguments: {text!r}") from None
        tool_calls.append({"id": slot["id"], "name": slot["name"], "arguments": arguments})
    return {"content": "".join(content), "tool_calls": tool_calls, "finish_reason": finish}
